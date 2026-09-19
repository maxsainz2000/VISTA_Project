---
module: MerchSys.Accounting
plan-id: ACC-22
title: "Revenue Record Consolidation — Eliminate Duplicate Writer Race"
depends-on: [ACC-01, ACC-11, INFRA-21, POS-14]
estimated-files: 4
priority: high
---

# Revenue Record Consolidation — Eliminate Duplicate Writer Race

## Context

`SaleCompletedEvent` and `SaleCompletedWithVatEvent` are published for the **same** POS transaction (the latter by `VatAwareReceiptService.PaymentResultAsync`, the former by `PaymentService.PaymentResultAsync` — see `MerchSys.POS/Services/VatAwareReceiptService.vb:17-19, 124-143` and `MerchSys.POS/Services/PaymentService.vb:74-90`). Two accounting handlers consume them and **both write to `Acc_RevenueRecords`**:

- `SaleCompletedAccountingHandler` (`MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb`) — legacy; always creates a row.
- `SaleCompletedWithVatHandler` (`MerchSys.Accounting/Handlers/SaleCompletedWithVatHandler.vb`) — newer; tries to find the row by `(SourceTransactionId, ProductId)` and update its VAT columns. If not found, falls through to the `Else` branch and **creates a second row**.

Hands-on testing on 2026-05-27 reproduced the duplicate (see `Plans/Future/deferred-features-backlog.md` item 18c):

```
Acc_RevenueRecords:
  Id=1  SourceTransactionId=1  ProductId=3  COGS=36000  GrossProfit=-3000   ← from legacy handler
  Id=2  SourceTransactionId=1  ProductId=3  COGS=0      GrossProfit=33000   ← from VAT handler's Else branch
Acc_ExpenseRecords:
  Id=4  Category=COGS  Amount=0   ← matched the duplicate row
```

The VAT handler's `FirstOrDefaultAsync` did not see the row written by the legacy handler. The two handlers run in separate MediatR `Publish` calls and have separate scopes / `DbContext` instances at runtime; "the same `DbContext` instance" in the backlog post-mortem is not the right explanation, but the symptom is real: a duplicate row exists. Whether the cause is scope-per-publish, change-tracker timing, or `Publish` ordering inside `VatAwareReceiptService`, the **structural fix is the same: stop having two writers.**

This plan removes the race by making `SaleCompletedWithVatHandler` the sole writer of `Acc_RevenueRecords` for every sale. The legacy `SaleCompletedAccountingHandler` is deleted. `SaleCompletedWithVatEvent` is published for every transaction today (VAT-registered or not — `VatAwareReceiptService` runs unconditionally as a `MediatR` decorator-style wrapper around the receipt pipeline), so removing the legacy handler does not leave any sale unrecorded.

Promoted from `Plans/Future/deferred-features-backlog.md` item 18c (2026-05-27).

**Ordering note:** ACC-22 ships **before** ACC-21. ACC-21 (per-batch COGS) modifies whichever handler is the sole writer; doing ACC-22 first means ACC-21 only needs to touch one file.

## Prerequisites

- **ACC-01** (Accounting Domain Models) — `RevenueRecord`, `ExpenseRecord` entities
- **ACC-11** (VAT Reporting Service) — `SaleCompletedWithVatHandler` shipped here
- **INFRA-21** (Accounting Handler Sync Migration) — both handlers already use `ISyncableRepository.SaveChangesWithJournalAsync`
- **POS-14** (VAT Configuration & Calculation) — `SaleCompletedWithVatEvent` published unconditionally

## Wiki References

- `concepts/modular-monolith.md` — handler-per-event ownership; MediatR `Publish` semantics
- `concepts/bir-compliance.md` — RevenueRecord is the authoritative sales ledger; duplicates corrupt audit
- `analysis/cross-module-data-flow.md` — `SaleCompletedEvent` and `SaleCompletedWithVatEvent` dual-publish during the POS-14 window

## Deliverables

```
MerchSys.Accounting/Handlers/
├── SaleCompletedAccountingHandler.vb        ' DELETE — legacy duplicate writer
└── SaleCompletedWithVatHandler.vb           ' MOD — rename to SaleRevenueHandler; becomes sole writer; absorbs ExpenseRecord COGS write

MerchSys.Accounting/Handlers/
└── SaleRevenueHandler.vb                    ' NEW (renamed from above) — single authoritative handler

LLM_Wiki/codebase_wiki/modules/accounting/handlers.md  ' Discrepancy log in Progress summary; do NOT edit directly per CLAUDE.md
```

Net: one handler deleted, one handler renamed and slightly expanded, one DI registration verified (auto-scan picks up the rename automatically).

## Specification

### Step 1 — Delete `SaleCompletedAccountingHandler`

Remove `MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` entirely. MediatR's assembly scan registration picks handlers up automatically, so no DI un-registration is needed. No other code in the solution constructs this type directly — verified at plan-write time via `Grep "SaleCompletedAccountingHandler"` returning only the file itself, the codebase-wiki snapshot, two prior progress summaries, and prior plan files (none in source).

### Step 2 — Rename `SaleCompletedWithVatHandler` → `SaleRevenueHandler`

The "WithVat" name was historically meaningful when there were two handlers. Once it's the sole writer the name is misleading: it also writes COGS and gross profit, not just VAT columns. Rename:
- File: `SaleCompletedWithVatHandler.vb` → `SaleRevenueHandler.vb`
- Class: `SaleCompletedWithVatHandler` → `SaleRevenueHandler`
- Logger generic: `ILogger(Of SaleCompletedWithVatHandler)` → `ILogger(Of SaleRevenueHandler)`

The implemented interface remains `INotificationHandler(Of SaleCompletedWithVatEvent)` — the **event** name does not change, only the handler.

### Step 3 — Absorb the COGS `ExpenseRecord` write

The legacy handler also created an `ExpenseRecord` per line item (category `"COGS"`, `Amount = cogs`). The new sole handler must do the same in its `Else` branch (when creating a fresh row) **and** preserve the pre-existing behaviour in its `If existing IsNot Nothing` branch:

- **If existing row is found** (re-publish / migration window): the legacy handler already created the matching `ExpenseRecord` in its run. Do not create a duplicate. Leave the `ExpenseRecord` untouched.
- **If no existing row** (the new normal path): create both the `RevenueRecord` and the matching `ExpenseRecord` in the same handler invocation, then call `SaveChangesWithJournalAsync` once.

```vb
' Inside the Else branch, after _db.RevenueRecords.Add(revenue)
Dim cogsExpense As New ExpenseRecord With {
    .RecordDate = notification.TransactionDate,
    .Category = "COGS",
    .Description = $"COGS for {item.ProductName} (Tx #{notification.TransactionId})",
    .Amount = cogs,
    .SourceModule = "POS",
    .SourceReferenceId = notification.TransactionId
}
_db.ExpenseRecords.Add(cogsExpense)
```

Single `SaveChangesWithJournalAsync` call at the end of the `For Each` loop — same as today.

### Step 4 — Idempotency guard on re-publish

The handler already queries `RevenueRecords.FirstOrDefaultAsync((SourceTransactionId, ProductId))`. Keep it; it is now load-bearing for re-publish safety because removing the legacy handler doesn't change the fact that re-publishing `SaleCompletedWithVatEvent` is still possible (e.g., during a POS replay). Add a matching idempotency guard for the new `ExpenseRecord` write:

```vb
Dim existingExpense = Await _db.ExpenseRecords.
    FirstOrDefaultAsync(
        Function(e) e.Category = "COGS" AndAlso
                    e.SourceModule = "POS" AndAlso
                    e.SourceReferenceId = notification.TransactionId AndAlso
                    e.Description.Contains($"(Tx #{notification.TransactionId})") AndAlso
                    e.Description.Contains(item.ProductName),
        cancellationToken)
If existingExpense Is Nothing Then
    _db.ExpenseRecords.Add(cogsExpense)
End If
```

This is defensive but cheap — the read happens once per item and the index on `Category + SourceReferenceId` (already present per ACC-02) keeps it sub-millisecond.

### Step 5 — Verify event publish ordering in POS is still valid

`VatAwareReceiptService` publishes `SaleCompletedWithVatEvent`. `PaymentService` publishes `SaleCompletedEvent`. After this plan ships, the **legacy event no longer has any Accounting consumer** but it is still consumed by `MerchSys.Inventory/Handlers/SaleCompletedHandler` (FIFO deduction). Do **not** stop publishing it — Inventory needs it. The blast radius of this plan is strictly inside `MerchSys.Accounting`.

If a future plan migrates the Inventory handler to consume `SaleCompletedWithVatEvent` as well, `SaleCompletedEvent` becomes deletable. That is out of scope here.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. `MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` no longer exists.
3. `MerchSys.Accounting/Handlers/SaleRevenueHandler.vb` exists; the class implements `INotificationHandler(Of SaleCompletedWithVatEvent)` only.
4. After completing one sale (any payment method, any VAT mode), `Acc_RevenueRecords` contains **exactly one** row per `(SourceTransactionId, ProductId)` — no duplicates.
5. `Acc_ExpenseRecords` contains **exactly one** COGS row per `(SourceTransactionId, ProductId)` — no duplicates.
6. Re-publishing `SaleCompletedWithVatEvent` for the same transaction (e.g., dev-menu replay or POS recovery) produces no new rows in either table — both writes are idempotent.
7. `SaleCompletedEvent` is still published by `PaymentService` (Inventory's `SaleCompletedHandler` depends on it).
8. The reproduction case from the deferred-backlog item 18c (single 30-unit sale at ₱1,100, three pre-existing batches at ₱1,000/₱1,100/₱1,200) produces one `RevenueRecord` row only. (COGS correctness is ACC-21's responsibility — this plan only deduplicates.)

## Out of Scope (Defer)

- **Per-batch COGS** — ACC-21. After ACC-22 the COGS line still uses `costResult.FifoUnitCost * item.Quantity`, which is wrong for multi-batch spans. ACC-21 fixes that.
- **Deleting `SaleCompletedEvent`** — still consumed by Inventory FIFO. A future cross-module plan can consolidate to a single event.
- **Backfilling existing duplicate rows.** Pre-existing duplicate rows in `Acc_RevenueRecords` (created during testing before this plan ships) are not auto-cleaned. The Progress summary should document the manual SQL needed to identify them: `SELECT SourceTransactionId, ProductId, COUNT(*) FROM Acc_RevenueRecords GROUP BY SourceTransactionId, ProductId HAVING COUNT(*) > 1;`. Deletion is a deliberate human step, not a migration.
- **Renaming `SaleCompletedWithVatEvent`** to match the new `SaleRevenueHandler` name. The event is published by POS and carrying its current name is part of the cross-module contract. Leave it.

## Notes for Implementers

- **MediatR auto-registration:** the project uses `services.AddMediatR(...)` with assembly scanning, so deleting the legacy handler removes its registration automatically. No edits needed in `MerchSys.App/Startup/*Registration*.vb`.
- **VB.NET trap — file rename + class rename:** when you rename the file and class, also update the namespace import for any test or dev-menu file that references it. At plan-write time `Grep "SaleCompletedWithVatHandler"` returns only its own file in source; if a new caller appears between now and implementation, search again and update.
- **VB.NET trap — `Await` in `Catch`/`Finally` (BC36943):** does not apply here, but the existing handler uses `Using _writeContext.Enter(...)` and you must not insert `Await` into a `Catch` if you add error handling.
- **VB.NET trap — string interpolation with `$"..."`:** keep using `$"(Tx #{notification.TransactionId})"` exactly as the legacy handler did so the idempotency `Description.Contains` check matches existing rows. A space or punctuation drift will create a duplicate.
- **`ISyncableRepository` posture:** unchanged from INFRA-21. The new handler keeps `_repository.SaveChangesWithJournalAsync(cancellationToken)` and does not regress to `_db.SaveChangesAsync()`.
- **Codebase wiki sync:** `LLM_Wiki/codebase_wiki/modules/accounting/handlers.md` references the legacy handler. Per CLAUDE.md the agent must not edit the codebase wiki; flag the rename + deletion in the Progress summary's "Discrepancies" section so the next Antigravity sync picks it up.
- The 2026-05-27 reproduction is reproducible in the dev menu — record the before / after row counts in the Progress summary.

## Output Requirements

### Implementation Summary
Create at `Progress/VISTA_Modules/Accounting/ACC-22-summary.md` using `Progress/_template.md`. Include:

- The deleted file's path and a single-line confirmation that no other source file references `SaleCompletedAccountingHandler`.
- The renamed file path and a `git diff --stat` excerpt showing the rename.
- A SQLite query result `SELECT SourceTransactionId, ProductId, COUNT(*) FROM Acc_RevenueRecords GROUP BY SourceTransactionId, ProductId HAVING COUNT(*) > 1;` from before and after a fresh test sale, confirming zero duplicates post-plan.
- A `## Codebase Wiki Discrepancy` section listing the handler rename so the next wiki sync can update `modules/accounting/handlers.md`.
- A `## ACC-21 Handoff` section pointing the next agent at the exact lines in `SaleRevenueHandler.vb` that compute `cogs` — the place where ACC-21 will replace the `GetProductCostQuery` lookup with the per-batch breakdown.

### Documentation
- XML doc on the renamed `SaleRevenueHandler` describing it as the sole writer of `Acc_RevenueRecords` for sales, and noting the idempotency keys: `(SourceTransactionId, ProductId)` for `RevenueRecord`, and the same plus `Category = "COGS"` for the matched `ExpenseRecord`.
- A one-line code comment at the COGS computation site reading exactly: `' ACC-21: replace with per-batch FIFO breakdown.` This is a deliberate forward reference so the ACC-21 implementer can grep for it.
