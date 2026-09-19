---
module: MerchSys.Accounting
plan-id: ACC-21
title: "Per-Batch FIFO COGS Accuracy in Sale Revenue Handler"
depends-on: [ACC-22, INV-03, INFRA-21]
estimated-files: 8
priority: high
---

# Per-Batch FIFO COGS Accuracy in Sale Revenue Handler

## Context

When a single sale quantity spans multiple FIFO batches at different unit costs, the recorded COGS is wrong. The handler computes `FifoUnitCost × Quantity` where `FifoUnitCost` is the unit cost of only the FIFO-oldest non-expired batch (`MerchSys.Inventory/Handlers/GetProductCostQueryHandler.vb:28-35`). The other batches consumed by the same sale are ignored. The Inventory module's `StockService.DeductStockFIFOAsync` (`MerchSys.Inventory/Services/StockService.vb:74-143`) **does** compute correct per-batch COGS in `FIFODeductionResult.COGS`, but those values are discarded after the FIFO deduction completes; Accounting re-queries cost from scratch.

Hands-on reproduction (2026-05-27, see `Plans/Future/deferred-features-backlog.md` item 18b):

- 3 batches of ProductId=3: ₱1,200 × 10, ₱1,100 × 10, ₱1,000 × 10 → total 30 units, total cost ₱33,000.
- One sale, 30 units at ₱1,100 retail → revenue ₱33,000. Correct break-even.
- Recorded `RevenueRecord.COGS = 36,000` (= ₱1,200 × 30 — oldest batch's unit cost applied to the entire 30-unit quantity), `GrossProfit = -3,000`. **Phantom ₱3,000 loss.**

This is the exact failure mode that `system_plan.md` Risk Register P5 (rated High/High) flags as a critical accounting risk, and it directly contradicts the design commitments cited in `system_plan.md §6.2`, `§9`, `§10`/`§11`, `Inventory-Module_AcademicPaper.md §2`, `Accounting-Module_AcademicPaper.md §1.5` and `§2`. The Inventory module does the right thing per-batch; the Accounting module throws that away.

This plan fixes it by giving Accounting access to the per-batch FIFO breakdown that Inventory already computes.

Promoted from `Plans/Future/deferred-features-backlog.md` item 18b (2026-05-27).

## Design choice: persist Inventory's FIFO result, query it from Accounting

Three options were considered. Recap of why we land on persistence:

1. **Attach breakdown to the event payload.** Would require POS (the publisher) to know the per-batch breakdown before the Inventory handler runs the deduction. POS does not perform the FIFO; Inventory does. Pushing FIFO upstream into POS violates the module boundary and breaks Inventory's authority over its own state.
2. **Merge Inventory's FIFO deduction and Accounting's revenue write into one handler.** Couples two modules into one transaction and breaks the "no cross-module service calls" rule in CLAUDE.md.
3. **Inventory persists `FIFODeductionResult` to a new `Inv_SaleCogs` ledger keyed by `(TransactionId, ProductId, BatchId)`. Accounting queries via a new MediatR query.** Each module stays in its own boundary. Idempotent. Survives MediatR handler ordering. Auditable (you can reconstruct exactly which batches a historical sale drew from). **This plan picks option 3.**

The new `Inv_SaleCogs` table is also a long-term audit primitive: it answers "what did this sale actually cost?" forever, even if a later admin tool modifies stock batches or shrinkage corrupts the live `Inv_StockBatches` rows.

## Prerequisites

- **ACC-22** (Revenue Record Consolidation) — `SaleRevenueHandler` is the sole writer of `Acc_RevenueRecords`. ACC-21 only has to modify one place.
- **INV-03** (Stock Management) — `StockService.DeductStockFIFOAsync`, `FIFODeductionResult`
- **INFRA-21** (Accounting Handler Sync Migration) — `ISyncableRepository.SaveChangesWithJournalAsync` already used by the handler

## Wiki References

- `concepts/fifo-costing.md` — per-batch costing is the design contract, not an approximation
- `concepts/modular-monolith.md` — cross-module communication via MediatR events and queries only
- `analysis/cross-module-data-flow.md` — existing query pattern (`GetProductCostQuery` → `GetProductCostResult`) to mirror
- `analysis/problem-feature-matrix.md` — Risk P5 (Price Volatility & Costing Error)

## Deliverables

```
MerchSys.Inventory/Entities/
└── SaleCogsRecord.vb                                  ' NEW — TransactionId, ProductId, BatchId, QuantityDeducted, UnitCost, Cogs, DeductedAt

MerchSys.Inventory/Data/Configurations/
└── SaleCogsRecordConfiguration.vb                     ' NEW — table Inv_SaleCogs, composite index (TransactionId, ProductId)

MerchSys.Inventory/Data/
└── InventoryDbContext.vb                              ' MOD — add SaleCogsRecords DbSet

MerchSys.Inventory/Migrations/
└── 2026XXXX_AddInvSaleCogs.vb                         ' NEW — manual SQL migration

MerchSys.Inventory/Handlers/
└── SaleCompletedHandler.vb                            ' MOD — after FIFO deduction, persist FIFODeductionResult rows to Inv_SaleCogs

MerchSys.SharedKernel/Queries/
└── GetSaleCogsBreakdownQuery.vb                       ' NEW — query + result types

MerchSys.Inventory/Handlers/
└── GetSaleCogsBreakdownQueryHandler.vb                ' NEW — reads Inv_SaleCogs for (TransactionId, ProductId)

MerchSys.Accounting/Handlers/
└── SaleRevenueHandler.vb                              ' MOD — replace GetProductCostQuery with GetSaleCogsBreakdownQuery; sum per-batch COGS
```

Total: 6 new files, 2 modified. `GetProductCostQuery` / `GetProductCostQueryHandler` / `GetProductCostResult` are **not deleted** — they remain valid for any caller that genuinely wants "what would the next sale's cost be?" (the Stock Dashboard's FIFO Cost column from INV-15 is such a caller). They simply stop being the COGS source for actual sales.

## Specification

### `SaleCogsRecord` entity

```vb
Public Class SaleCogsRecord
    Inherits Entity                          ' NOT AuditableEntity — this row IS the audit record

    ''' <summary>POS transaction this deduction belonged to. Cross-module reference (no FK).</summary>
    Public Property TransactionId As Integer

    ''' <summary>Product whose batches were drawn from.</summary>
    Public Property ProductId As Integer

    ''' <summary>FK to the specific StockBatch consumed. May refer to a fully-depleted batch.</summary>
    Public Property BatchId As Integer

    ''' <summary>Units taken from this batch for this sale.</summary>
    Public Property QuantityDeducted As Integer

    ''' <summary>Unit cost on the batch at the time of deduction. Frozen here so later batch edits cannot alter history.</summary>
    Public Property UnitCost As Decimal

    ''' <summary>QuantityDeducted * UnitCost. Stored, not computed, for trivial Accounting SUM.</summary>
    Public Property Cogs As Decimal

    ''' <summary>UTC timestamp the deduction was committed.</summary>
    Public Property DeductedAt As DateTime
End Class
```

- Plain `Entity` (`Id` only). Append-only — no `ModifiedBy`/`ModifiedAt`. No `IsDeleted`.
- `BatchId` is an EF-side FK within Inventory (same module), with `DeleteBehavior.Restrict` — a batch with cost history cannot be hard-deleted.
- `(TransactionId, ProductId)` is the natural lookup key. `(BatchId)` is a secondary index for "show me every sale that drew from this batch."

### `SaleCogsRecordConfiguration`

- Table: `Inv_SaleCogs`.
- Index 1: `(TransactionId, ProductId)` — primary read path for Accounting.
- Index 2: `(BatchId)` — secondary read path for batch-level audit.
- `UnitCost` and `Cogs`: `Decimal(18, 4)` to preserve enough precision for fractional kg / L unit costs that may arise later.

### Migration (manual SQL, EF CLI ban per CLAUDE.md)

```sql
CREATE TABLE IF NOT EXISTS Inv_SaleCogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TransactionId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    BatchId INTEGER NOT NULL,
    QuantityDeducted INTEGER NOT NULL,
    UnitCost NUMERIC NOT NULL,
    Cogs NUMERIC NOT NULL,
    DeductedAt TEXT NOT NULL,
    FOREIGN KEY (BatchId) REFERENCES Inv_StockBatches(Id)
);
CREATE INDEX IF NOT EXISTS IX_Inv_SaleCogs_Tx_Product
    ON Inv_SaleCogs (TransactionId, ProductId);
CREATE INDEX IF NOT EXISTS IX_Inv_SaleCogs_Batch
    ON Inv_SaleCogs (BatchId);
```

Applied by `DatabaseInitializer` at `Application_Startup`, same pattern as ACC-15, INV-14, etc.

### `SaleCompletedHandler` (Inventory) — persist breakdown

Today's handler (`MerchSys.Inventory/Handlers/SaleCompletedHandler.vb:33-44`) loops over items, calls `_stockService.DeductStockFIFOAsync`, sums COGS for a log line, then discards the breakdown. Change: after each deduction, write the breakdown to `Inv_SaleCogs` in the same scope.

```vb
For Each item In notification.Items
    Try
        Dim deductions = Await _stockService.DeductStockFIFOAsync(item.ProductId, item.Quantity)
        Await PersistCogsBreakdownAsync(notification.TransactionId, item.ProductId, deductions, cancellationToken)
        Dim totalCogs As Decimal = deductions.Sum(Function(d) d.COGS)
        _logger.LogInformation("FIFO deduction for Product {ProductId} ({ProductName}): Qty={Qty}, COGS={COGS}.",
            item.ProductId, item.ProductName, item.Quantity, totalCogs)
    Catch ex As InsufficientStockException
        ' (unchanged)
    End Try
Next
```

`PersistCogsBreakdownAsync` reads the deductions, constructs `SaleCogsRecord` rows, adds them to `_db.SaleCogsRecords`, and calls `_repository.SaveChangesWithJournalAsync(cancellationToken)` once at the end of the loop. Idempotency: pre-check `_db.SaleCogsRecords.Any(Function(r) r.TransactionId = transactionId AndAlso r.ProductId = productId)` before inserting and skip if true — handles re-publish of `SaleCompletedEvent` without duplicating COGS history.

The handler now needs `ISyncableRepository(Of InventoryDbContext)` and `InventoryDbContext` injected. `StockService` already injects both, so DI is already wired; just add to this handler's constructor.

### `GetSaleCogsBreakdownQuery` (SharedKernel)

```vb
Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the actual per-batch COGS breakdown
    ''' for a completed sale's line item. Mirrors the Inv_SaleCogs ledger written by
    ''' the Inventory module's SaleCompletedHandler.
    ''' Returns an empty list if the deduction has not been committed yet
    ''' (e.g., Accounting handler raced ahead of Inventory's SaveChanges) — callers must
    ''' treat that as a transient condition and fall back gracefully.
    ''' </summary>
    Public Class GetSaleCogsBreakdownQuery
        Implements IRequest(Of GetSaleCogsBreakdownResult)
        Public Property TransactionId As Integer
        Public Property ProductId As Integer
    End Class

    Public Class GetSaleCogsBreakdownResult
        ''' <summary>Total COGS for this (Transaction, Product) — SUM of per-batch COGS.</summary>
        Public Property TotalCogs As Decimal
        ''' <summary>Per-batch detail; empty when no Inv_SaleCogs rows match.</summary>
        Public Property Lines As List(Of SaleCogsLine)
    End Class

    Public Class SaleCogsLine
        Public Property BatchId As Integer
        Public Property QuantityDeducted As Integer
        Public Property UnitCost As Decimal
        Public Property Cogs As Decimal
    End Class

End Namespace
```

### `GetSaleCogsBreakdownQueryHandler` (Inventory)

Straightforward `_db.SaleCogsRecords.Where(...).ToListAsync()` projection. **VB.NET trap warning:** `ToListAsync()` on a full-entity query in EF Core 10 VB.NET silently returns empty (see CLAUDE.md trap table). Project to `SaleCogsLine` inside the LINQ tree:

```vb
Dim lines = Await _db.SaleCogsRecords.
    Where(Function(r) r.TransactionId = request.TransactionId AndAlso r.ProductId = request.ProductId).
    Select(Function(r) New SaleCogsLine With {
        .BatchId = r.BatchId,
        .QuantityDeducted = r.QuantityDeducted,
        .UnitCost = r.UnitCost,
        .Cogs = r.Cogs
    }).
    ToListAsync(cancellationToken)
Return New GetSaleCogsBreakdownResult With {
    .TotalCogs = lines.Sum(Function(l) l.Cogs),
    .Lines = lines
}
```

### `SaleRevenueHandler` (Accounting) — switch COGS source

After ACC-22 the legacy handler is gone and the sole writer carries the line:

```vb
' ACC-21: replace with per-batch FIFO breakdown.
Dim costResult = Await _mediator.Send(New GetProductCostQuery() With {.ProductId = item.ProductId}, cancellationToken)
Dim cogs = costResult.FifoUnitCost * item.Quantity
```

Replace with:

```vb
Dim breakdown = Await _mediator.Send(
    New GetSaleCogsBreakdownQuery() With {
        .TransactionId = notification.TransactionId,
        .ProductId = item.ProductId
    },
    cancellationToken)

Dim cogs As Decimal
If breakdown.Lines.Count > 0 Then
    cogs = breakdown.TotalCogs
Else
    ' Inventory's SaveChanges has not landed yet — fall back to FIFO-oldest unit cost
    ' so the row is at least non-zero. ACC-22's idempotency guard means the next
    ' re-publish (if any) will not overwrite this row, so we log a warning and proceed.
    Dim costResult = Await _mediator.Send(New GetProductCostQuery() With {.ProductId = item.ProductId}, cancellationToken)
    cogs = costResult.FifoUnitCost * item.Quantity
    _logger.LogWarning(
        "COGS breakdown not yet persisted for Tx {Tx} / Product {Product}; fell back to FIFO-oldest unit cost. " &
        "Investigate handler ordering if this recurs.",
        notification.TransactionId, item.ProductId)
End If
```

The fallback path is **defensive**, not the normal flow. In practice MediatR's `Publish` for `SaleCompletedEvent` (Inventory deduction + COGS persist) is awaited before `Publish` for `SaleCompletedWithVatEvent` (Accounting revenue) — both are awaited in sequence inside `VatAwareReceiptService.PaymentResultAsync`. The fallback exists for two cases: (1) a future code path that publishes only the VAT event, (2) a defensive seam if Publish ordering ever changes.

The handler's `ExpenseRecord` write (added by ACC-22) uses the same `cogs` value — no further change needed.

### Out-of-band reconciliation (optional, document only)

If the warning above ever fires in production, a Manager-visible reconciliation can be added: a one-shot job that scans `Acc_RevenueRecords WHERE COGS = (FifoOldestUnitCost * QuantitySold)` and re-computes from `Inv_SaleCogs`. Out of scope for this plan — log it in the Progress summary if you observe the fallback firing during dev-menu testing.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. `Inv_SaleCogs` table created at first launch after deployment.
3. Reproduction case: with three batches at ₱1,200 / ₱1,100 / ₱1,000 (10 units each) and one 30-unit sale at ₱1,100 retail:
   - `Acc_RevenueRecords.COGS = 33000` exactly.
   - `Acc_RevenueRecords.GrossProfit = 0` exactly.
   - `Inv_SaleCogs` contains three rows for `(TransactionId=N, ProductId=3)` with COGS ₱12,000 / ₱11,000 / ₱10,000 respectively.
4. Partial-batch span: with one batch at ₱1,200 (10 units) and a 4-unit sale, `Acc_RevenueRecords.COGS = 4800`; `Inv_SaleCogs` contains one row with `QuantityDeducted=4`.
5. Single-batch sale unchanged: behaviour matches today for any sale that fits inside one batch.
6. Re-publish of `SaleCompletedEvent` for the same transaction does not create duplicate `Inv_SaleCogs` rows.
7. The fallback warning log line fires zero times during normal dev-menu sales; document any occurrence with the transaction trace in the Progress summary.
8. `GetProductCostQuery` and `GetProductCostQueryHandler` still exist and still pass through `StockDashboardService` callers (INV-15) unchanged.

## Out of Scope (Defer)

- **Backfill of historical sales.** Sales completed before this plan ships have no `Inv_SaleCogs` rows. `Acc_RevenueRecords.COGS` for those rows remains whatever the legacy handler recorded — wrong in the multi-batch case, correct in the single-batch case. A backfill from `Inv_StockBatches` is not possible because we no longer know which batches a historical sale drew from. Future plan only if a Manager explicitly requests it.
- **MariaDB central-DB triggers** for `Inv_SaleCogs` immutability. Operational data, not BIR-tamper-protected. Pattern after ACC-15 if requirements escalate.
- **Removing `SaleCompletedEvent`.** Same reason as ACC-22's out-of-scope note.
- **Cross-batch COGS attribution for returns/refunds.** The POS return path is not in scope today; when it ships it must also write to `Inv_SaleCogs` (negative quantities) to keep the ledger consistent. Flag that requirement in the return plan when it's written.
- **DB-level immutability triggers on `Inv_SaleCogs`.** Out of scope; the row is application-write-once, but no SQLite trigger enforcement until/unless requirements escalate.

## Notes for Implementers

- **Handler ordering between modules:** `Publish(SaleCompletedEvent)` (Inventory) is awaited before `Publish(SaleCompletedWithVatEvent)` (Accounting) inside `VatAwareReceiptService.PaymentResultAsync`. This means by the time Accounting reads `Inv_SaleCogs`, the rows are committed. **Verify** at implementation by adding a temporary `Debug.Assert(breakdown.Lines.Count > 0)` during dev testing; remove before commit.
- **Idempotency vs the fallback path:** the idempotency check inside the Inventory handler prevents duplicate `Inv_SaleCogs` rows, but if Inventory itself fails partway (e.g., exception after some but not all items deducted), `Inv_SaleCogs` will be partial. ACC-22's `RevenueRecord` idempotency keys (`SourceTransactionId, ProductId`) plus a re-publish will re-execute Inventory and complete the breakdown. Document this resumption story in the Progress summary.
- **`Decimal` precision:** SQLite stores `NUMERIC` as IEEE-754 `REAL` unless typed. EF Core 10 maps `Decimal(18,4)` correctly via the configured converter — verify by reading back ₱1,234.5678 round-trip in a dev test.
- **VB.NET trap — `entry` in DbContext loop:** does not apply here; this code does not iterate `ChangeTracker.Entries()`.
- **VB.NET trap — `ToListAsync` empty result:** addressed in spec by projecting to `SaleCogsLine` inside the LINQ tree. Do not regress to `.ToListAsync()` on full entities.
- **VB.NET trap — parameter shadows property:** when `SaleCompletedHandler` adds `PersistCogsBreakdownAsync(transactionId, productId, deductions, ...)`, the parameter `deductions` does not collide with anything in the class today — but verify if the class is later extended.
- **VB.NET trap — `Await` in `Catch`/`Finally` (BC36943):** the existing `Try`/`Catch` around `DeductStockFIFOAsync` re-throws; do not add an `Await` inside the `Catch` block. Persist the breakdown only on the success path, which is where the spec places it.
- **MediatR registration:** the new `GetSaleCogsBreakdownQueryHandler` is picked up by assembly scan. No DI edits required.
- **Codebase wiki sync:** new entity (`SaleCogsRecord`), new table (`Inv_SaleCogs`), new query, new handler. Flag all of these in the Progress summary's Discrepancies section for the next Antigravity sync.

## Output Requirements

### Implementation Summary
Create at `Progress/VISTA_Modules/Accounting/ACC-21-summary.md` using `Progress/_template.md`. Include:

- A SQLite query showing the three `Inv_SaleCogs` rows produced by the reproduction case (3 batches × 10 units × ₱1,000/₱1,100/₱1,200).
- The resulting `Acc_RevenueRecords` row showing `COGS = 33000` and `GrossProfit = 0`.
- A note on whether the fallback warning ever fired during dev testing (expected: no).
- A `## Codebase Wiki Discrepancy` section listing the new entity, table, query, and handler so the next wiki sync covers them.
- A `git diff` excerpt showing the replaced `cogs` computation in `SaleRevenueHandler.vb`, with the `' ACC-21: replace with per-batch FIFO breakdown.` marker comment removed (because the work it describes is now done).
- A short note explaining why `GetProductCostQuery` was retained rather than deleted (it still serves the Stock Dashboard's FIFO Cost column from INV-15 and remains a meaningful "next-sale cost" query).

### Documentation
- XML doc on `SaleCogsRecord` describing it as the append-only, per-batch sale-cost ledger and noting that it is the authoritative COGS source for any post-hoc audit.
- XML doc on `GetSaleCogsBreakdownQuery` describing the fallback contract (empty list when not yet committed).
- XML doc on the modified `SaleRevenueHandler` COGS block describing the breakdown-first / FIFO-oldest fallback strategy and citing this plan ID.
- One-line code comment on the Inventory handler's `PersistCogsBreakdownAsync` call explaining the idempotency check.
