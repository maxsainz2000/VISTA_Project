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

## Resolution
- **Status:** resolved (pending operator runtime confirmation)
- **Root cause:** `AuditableEntity` inherits `ConcurrencyAwareEntity`, giving every auditable entity a `RowVersion` property. EF maps it by convention. `Pur_PurchaseOrderLines` (append-only child) has no RowVersion column and the config did not ignore the property, so the insert referenced a non-existent column.
- **Fix description:** `PurchaseOrderLineConfiguration` now calls `builder.Ignore(Function(l) l.RowVersion)`.
- **Final commit:** (see git log on this branch)
- **Agent wiki entry needed?** yes — append-only child entities inheriting ConcurrencyAwareEntity must Ignore RowVersion when their table omits the column. NOTE: same latent bug class affects every other AuditableEntity-derived entity whose table has no RowVersion column (e.g. Pur_GoodsReceiptLines, Pos_SalesTransactionLines, Pos_CreditPayments, etc.) — flagged for separate review.
