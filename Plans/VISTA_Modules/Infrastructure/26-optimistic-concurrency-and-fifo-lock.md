---
module: MerchSys.Infrastructure
plan-id: INFRA-26
title: "Optimistic Concurrency Tokens + FOR UPDATE on FIFO Decrement"
depends-on: [INFRA-25]
estimated-files: 12
priority: critical
amendment-ref: AMD-2026-05-28-01
---

# INFRA-26: Optimistic Concurrency Tokens + FOR UPDATE on FIFO Decrement

## Context

The pivot to four concurrent clients (per `system_plan_amendment_2026-05-28.md`) requires explicit concurrency control. Two mechanisms work together:

1. **Optimistic concurrency tokens** on mutable rows: detect when another client modified a row between read and save. EF Core raises `DbUpdateConcurrencyException`; the UI prompts user to refresh.
2. **Pessimistic `SELECT ... FOR UPDATE`** inside the FIFO inventory decrement transaction: serialize concurrent sales of the same product so two cashiers cannot both decrement the same `Inv_StockBatches.QuantityRemaining` from 5 to 4.

The `RowVersion TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)` columns already exist on mutable tables (INFRA-24). This plan wires them into the entity classes and EF configurations, and rewrites the FIFO decrement path.

## Prerequisites

- **INFRA-25** — Module DbContexts on MariaDB; SQLite gone.

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` §2.3
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (Concurrency Control)
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` (Concurrency Control — Two Mechanisms)

## Deliverables

### Optimistic concurrency tokens

```
MerchSys.SharedKernel/Entities/ConcurrencyAwareEntity.vb       ' NEW — abstract base with RowVersion property
MerchSys.SharedKernel/Entities/AuditableEntity.vb              ' MOD — inherit from ConcurrencyAwareEntity
```

The following mutable entities inherit (already do via `AuditableEntity`) and gain a `RowVersion` property:
- `Inv_StockBatches` (`QuantityRemaining` mutates)
- `Inv_Products` (RetailPrice mutates — INV-14)
- `Pur_AccountsPayable` (`OutstandingBalance` mutates)
- `Pur_PurchaseOrders` (`Status` mutates through lifecycle)
- `Pos_CreditAccounts` (`OutstandingBalance` mutates)
- `Pos_SalesTransactions` (mutable until OR issued)
- `Pur_Vendors` (mutable)
- `Inv_ProductCategories` (mutable)

Append-only entities **do not** inherit `ConcurrencyAwareEntity`:
- `Inv_StockMovements`
- `Inv_SaleCogs`
- `Pos_ReceiptIntegrity`
- `Acc_RevenueRecords`
- `Acc_ExpenseRecords`
- `Acc_TamperAuditLog`
- `Pur_PriceChangeAlerts`

```
MerchSys.<Module>/Entities/<Entity>Configuration.vb           ' MOD — IsRowVersion on the RowVersion property
```

```vb
builder.Property(Function(e) e.RowVersion).
    IsRowVersion().
    HasColumnType("TIMESTAMP(6)").
    ValueGeneratedOnAddOrUpdate()
```

### FIFO decrement rewrite

The current FIFO decrement code path (likely in `MerchSys.Inventory/Handlers/SaleCompletedHandler.vb`, `ShrinkageHandler.vb`, and the returns path) reads batches, decrements, saves — without explicit row locking. Rewrite each path to use a single transaction with `SELECT ... FOR UPDATE`:

```vb
Public Async Function DecrementFifoAsync(productId As Integer, quantity As Integer) As Task(Of List(Of FifoDeduction))
    Using tx = Await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
        ' Lock the oldest non-depleted, non-expired batches for this product.
        Dim batches = Await _db.Database.SqlQueryRaw(Of StockBatchLocked)(
            "SELECT Id, QuantityRemaining, UnitCost, ExpiryDate, ReceiptDate " &
            "FROM Inv_StockBatches " &
            "WHERE ProductId = {0} AND QuantityRemaining > 0 AND IsDeleted = 0 " &
            "  AND (ExpiryDate IS NULL OR ExpiryDate > UTC_TIMESTAMP()) " &
            "ORDER BY ReceiptDate ASC, Id ASC " &
            "FOR UPDATE",
            productId).ToListAsync()

        Dim deductions As New List(Of FifoDeduction)()
        Dim remaining = quantity
        For Each b In batches
            If remaining <= 0 Then Exit For
            Dim take = Math.Min(b.QuantityRemaining, remaining)
            ' Update via tracked entity so RowVersion check applies
            Dim tracked = Await _db.StockBatches.FindAsync(b.Id)
            tracked.QuantityRemaining -= take
            deductions.Add(New FifoDeduction With {.BatchId = b.Id, .Quantity = take, .UnitCost = b.UnitCost})
            remaining -= take
        Next

        If remaining > 0 Then
            Throw New InsufficientStockException(productId, requested:=quantity, available:=quantity - remaining)
        End If

        Await _db.SaveChangesAsync()
        Await tx.CommitAsync()
        Return deductions
    End Using
End Function
```

Keys:
- `FOR UPDATE` holds row locks until the transaction commits. Other clients calling the same query on the same product wait.
- The subsequent `FindAsync` reads through EF's identity map; `SaveChangesAsync` includes the `RowVersion` check on the UPDATE — defense in depth.
- `IsolationLevel.ReadCommitted` is MariaDB's default; pass explicitly for clarity.

### Concurrency exception handling at the UI boundary

Add a base ViewModel helper:

```vb
Public Async Function ExecuteWithConcurrencyRetryAsync(Of T)(work As Func(Of Task(Of T)),
                                                              onRefresh As Func(Of Task)) As Task(Of T)
    Try
        Return Await work()
    Catch ex As DbUpdateConcurrencyException
        _notifications.Show("Data changed elsewhere — refreshing", NotificationLevel.Warning)
        Await onRefresh()
        Throw   ' Let caller decide whether to retry or abort
    End Try
End Function
```

Apply to all mutating commands (`SavePurchaseOrderCommand`, `IssueOrCommand`, `RecordShrinkageCommand`, etc.).

## Specification

### Migration script for `RowVersion` columns

The schema bootstrap (INFRA-24) already includes `RowVersion` on mutable tables in `0001_initial_schema.sql`. If INFRA-24 has shipped to any environment **without** this column on a mutable table, author a `0003_add_rowversion.sql` script to add it:

```sql
ALTER TABLE Inv_StockBatches
    ADD COLUMN RowVersion TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6);
```

(Verify INFRA-24's `0001_initial_schema.sql` against the entity list above before deciding whether `0003_*` is needed.)

### Provider quirk: `IsRowVersion()` against MariaDB

Some MySQL/MariaDB EF providers map `IsRowVersion()` to `TIMESTAMP` automatically; others require explicit `HasColumnType("TIMESTAMP(6)")`. INFRA-23's spike must have surfaced this. Follow whatever the provider docs (or the spike output) say.

If the chosen provider does not honor `IsRowVersion()` semantics out of the box, fall back to a `BIGINT` version column updated via `BEFORE UPDATE` trigger:

```sql
CREATE TRIGGER tr_<table>_rowversion BEFORE UPDATE ON <table>
FOR EACH ROW SET NEW.RowVersion = OLD.RowVersion + 1;
```

Document the fallback in the implementation summary if used.

## Acceptance Criteria

1. Every mutable entity has a `RowVersion` property mapped via `IsRowVersion()`.
2. Updating a row from two `DbContext` instances in sequence — Context A reads, Context B reads, Context A saves, Context B saves — produces `DbUpdateConcurrencyException` on B's save. (Verify with a small spike test or smoke procedure.)
3. FIFO decrement path uses `SELECT ... FOR UPDATE` inside `BeginTransactionAsync`. Spot-check: kill the transaction mid-decrement → `Inv_StockBatches.QuantityRemaining` is unchanged.
4. All mutating ViewModel commands route through `ExecuteWithConcurrencyRetryAsync`.
5. UI surfaces "Data changed elsewhere — refreshing" toast on `DbUpdateConcurrencyException`.
6. Build: 0 errors / 0 warnings.

## Out of Scope (Defer)

- Tearing out remaining sync code (`SyncOrchestrator`, etc.) → **INFRA-27**.
- Connection-status indicator → **INFRA-28**.
- Retry-with-fresh-read on concurrency exception (current spec re-throws after refresh; an automatic-retry layer may be valuable later).
- Distributed lock for cross-product bulk operations (e.g., bulk price update). Out of scope until a workflow demands it.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-26-summary.md` per `Progress/_template.md`. Include:

- List of entities that gained `RowVersion`.
- List of entities deliberately not given `RowVersion` (append-only) with justification per entity.
- A demonstration of the concurrency exception path (logs from a two-DbContext spike test).
- A demonstration of `SELECT ... FOR UPDATE` serialization (two concurrent decrement calls — second waits for first).
- Note on whether the chosen provider honored `IsRowVersion()` natively or required the `BIGINT` + trigger fallback.

### Documentation

- XML doc on `ConcurrencyAwareEntity.RowVersion` documenting the optimistic-concurrency contract.
- XML doc on `DecrementFifoAsync` documenting the transactional `FOR UPDATE` contract and the `InsufficientStockException` semantics.
- Inline comment at every `BeginTransactionAsync` site explaining why the transaction is required (concurrency serialization, not durability).
- Comment in `ExecuteWithConcurrencyRetryAsync` documenting that callers may choose to retry-with-fresh-read or to abort.
