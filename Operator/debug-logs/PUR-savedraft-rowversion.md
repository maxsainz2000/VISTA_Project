---
test-id: PUR-SaveDraft-RowVersion
checklist: (none — user chose "just fix it")
branch: debug/PUR-savedraft-rowversion
started: 2026-05-29T00:00
status: in-progress
---

# Debug Session — Purchase Orders "Save Draft" crash

## Problem Statement
Clicking **Save Draft** in the Purchase Orders tab throws:

```
Microsoft.EntityFrameworkCore.DbUpdateException
  Message: Could not save changes. Please configure your entity type accordingly.
  Source:  MySql.EntityFrameworkCore
Inner Exception 1:
  MySqlException: Unknown column 'RowVersion' in 'field list'
```

Stack: PurchaseOrderListViewModel.SaveDraftAsync → PurchaseOrderService.CreateDraftAsync
(line 56, `_db.SaveChangesAsync()`) → BaseDbContext.SaveChangesAsync line 112.

## Starting State
- **Commit:** `f8a83ee`
- **Build status:** clean (assumed — app was running)
- **Relevant files:**
  - `MerchSys.Purchasing/Data/Configurations/PurchaseOrderLineConfiguration.vb`
  - `MerchSys.SharedKernel/Entities/AuditableEntity.vb` (root cause, off-limits)
  - `MerchSys.Infrastructure/Data/Migrations/Central/0001_initial_schema.sql` (schema, off-limits)

## Root Cause (confirmed before any edit)
`AuditableEntity` inherits `ConcurrencyAwareEntity`, which declares a public
`RowVersion As DateTime` property. Therefore **every** AuditableEntity-derived
entity — including `PurchaseOrderLine` — gets a `RowVersion` property that EF
Core maps to a `RowVersion` column **by convention**.

- `Pur_PurchaseOrders` table HAS a `RowVersion` column (verified via SHOW COLUMNS)
  and `PurchaseOrderConfiguration` calls `IsRowVersion()`, so the parent insert works.
- `Pur_PurchaseOrderLines` table has **NO** `RowVersion` column (verified via
  SHOW COLUMNS) — it is an append-only child table, intentionally omitted per the
  Migrations README. But `PurchaseOrderLineConfiguration` neither maps nor ignores
  the inherited `RowVersion` property, so EF emits SQL referencing the non-existent
  column → "Unknown column 'RowVersion' in 'field list'".

Live DB verification:
- `Pur_PurchaseOrders`: RowVersion timestamp(6) present.
- `Pur_PurchaseOrderLines`: no RowVersion column.

## Allowed Files
- `MerchSys.Purchasing/Data/Configurations/PurchaseOrderLineConfiguration.vb` — ignore the unmapped inherited RowVersion property for this append-only child entity.

### Off-limits (do NOT touch)
- `SharedKernel/Entities/AuditableEntity.vb` — changing its base class (true root cause) has cross-module blast radius; out of scope for this single-test fix.
- `MerchSys.Infrastructure/.../0001_initial_schema.sql` — append-only migration; adding a RowVersion column to the lines table contradicts the append-only-child design.
- Other modules' configs/services.

---

## Attempt Log

### Attempt 1
- **Hypothesis:** `PurchaseOrderLine` inherits a `RowVersion` property (via AuditableEntity → ConcurrencyAwareEntity) that EF maps to a column the `Pur_PurchaseOrderLines` table does not have. Telling EF to `Ignore` that property in `PurchaseOrderLineConfiguration` will stop EF from referencing the missing column, matching the schema's append-only-child intent.
- **Changed:** `PurchaseOrderLineConfiguration.vb` — added `builder.Ignore(Function(l) l.RowVersion)`.
- **Build result:** clean — 0 Warning(s), 0 Error(s).
- **Runtime result:** Not run by agent — WPF UI (login + Save Draft click) cannot be driven headlessly here. Root cause confirmed by live `SHOW COLUMNS` (lines table lacks RowVersion) + entity hierarchy; fix removes the bad mapping. Operator to re-run Save Draft to confirm.
- **Verdict:** ✅ fix applied, build clean (runtime confirmation deferred to operator)
- **Action:** committed on debug branch

---

---

## Follow-up issue (same session): Delete Draft does not remove PO from list

### Problem
Operator confirmed Save Draft works. Then deleting a draft showed "Draft deleted."
but PO-2026-0001 (Draft, ₱15,000) remained in the list after refresh.

### Root cause (confirmed)
`DeleteDraftAsync` calls `_db.PurchaseOrders.Remove(po)`. `BaseDbContext.SaveChangesAsync`
intercepts `EntityState.Deleted` on `ISoftDeletable` entities and converts the hard delete
into a soft delete (`IsDeleted = True`, `DeletedAt`, `DeletedBy`). Verified in live DB:
`Pur_PurchaseOrders` Id=2 now has `IsDeleted=1, DeletedAt=2026-05-29 02:33:39`.

The delete itself works. The list query is the problem: `GetAllAsync` bypasses EF with a
raw `MySqlConnection` query, so the global EF soft-delete query filter (registered in
`BaseDbContext.ApplySoftDeleteFilters`) does NOT apply. The raw SQL had no `IsDeleted`
predicate, so soft-deleted rows kept appearing.

### Fix
Added `IsDeleted = 0` to both branches of the `GetAllAsync` raw query in
`PurchaseOrderService.vb`:
- with status: `WHERE IsDeleted = 0 AND Status = @status`
- without status: `WHERE IsDeleted = 0`

Build: 0 errors, 0 warnings. Runtime confirmation deferred to operator.

> **Latent:** any other raw-SQL read in the Purchasing module (or other modules) that
> targets a soft-deletable table must include the same `IsDeleted = 0` predicate — the EF
> query filter only protects EF-materialized queries, not raw ADO.NET reads.

---

## Follow-up issue #2 (same session): Save Draft duplicate OrderNumber after a delete

### Problem
After deleting PO-2026-0001 (soft delete) and creating a new draft, Save Draft threw:
```
MySqlException: Duplicate entry 'PO-2026-0001' for key 'IX_Pur_PurchaseOrders_OrderNumber'
```

### Root cause (confirmed)
`CreateDraftAsync` builds the order number from `_db.PurchaseOrders.Select(OrderNumber).ToListAsync()`,
which goes through EF and therefore applies the global soft-delete query filter. The soft-deleted
PO-2026-0001 is excluded, so `SequentialNumberGenerator` computes max=0 and regenerates
`PO-2026-0001`. But `IX_Pur_PurchaseOrders_OrderNumber` is a UNIQUE index spanning ALL rows
(soft-deleted included), so the insert collides. This is the classic soft-delete vs. unique-constraint
conflict, and it is a direct consequence of fixing the list query to hide soft-deleted rows.

### Fix
Add `.IgnoreQueryFilters()` to the existing-numbers lookup so the generator accounts for
soft-deleted order numbers too (next number becomes PO-2026-0002). One line in
`PurchaseOrderService.CreateDraftAsync`. Build: 0 errors, 0 warnings.

> **Note:** MariaDB has no filtered/partial unique index, so the constraint cannot be scoped to
> `IsDeleted=0` without a schema change (off-limits, append-only). Ignoring the query filter at the
> generation site is the correct, minimal fix.

---

## Resolution
- **Status:** resolved (pending operator runtime confirmation)
- **Root cause:** `AuditableEntity` inherits `ConcurrencyAwareEntity`, giving every auditable entity a `RowVersion` property. EF maps it by convention. `Pur_PurchaseOrderLines` (append-only child) has no RowVersion column and the config did not ignore the property, so the insert referenced a non-existent column. (Plus follow-up soft-delete/raw-SQL filter issue documented above.)
- **Fix description:** `PurchaseOrderLineConfiguration` now calls `builder.Ignore(Function(l) l.RowVersion)`.
- **Final commit:** (see git log on this branch)
- **Agent wiki entry needed?** yes — append-only child entities inheriting ConcurrencyAwareEntity must Ignore RowVersion when their table omits the column. NOTE: same latent bug class affects every other AuditableEntity-derived entity whose table has no RowVersion column (e.g. Pur_GoodsReceiptLines, Pos_SalesTransactionLines, Pos_CreditPayments, etc.) — flagged for separate review.
