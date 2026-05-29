---
type: error-fix
module: Infrastructure
agent: claude-code
date: 2026-05-29
tags: [ef-core, mariadb, vb-net, vat, schema-migration, post-pivot-regression, runtime-error]
error-code: MySqlException-1054
severity: runtime-error
---

## Problem

Clicking **Confirm Receipt** in Goods Receiving threw:

```
Microsoft.EntityFrameworkCore.DbUpdateException
  Message=Could not save changes. Please configure your entity type accordingly.
  Source=MySql.EntityFrameworkCore
Inner Exception 1:
  MySqlException: Unknown column 'InputVat' in 'field list'
```

Origin chain: `GoodsReceivingService.ReceiveGoodsAsync` publishes `GoodsReceivedEvent`
→ `GoodsReceivedAccountingHandler.Handle` inserts an `ExpenseRecord`
→ `BaseDbContext.SaveChangesAsync` → INSERT into `Acc_ExpenseRecords` fails.

## Root Cause

The `Acc_ExpenseRecords` and `Acc_RevenueRecords` tables were created in the initial
central schema (`0001_initial_schema.sql`) **without** the six BIR three-bucket VAT columns.
Those columns (`VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`,
`InputVat`, `VatTreatment`) were added to the `ExpenseRecord` / `RevenueRecord` entities
by ACC-10 via the `LedgerVatExtensions.vb` **partial classes** — and originally backed by a
**SQLite-era EF migration** (`20260510100000_AddVatLedgerColumns`). When the project pivoted
to centralized MariaDB (2026-05-28), that ALTER was never ported to a central migration, so
the live MariaDB tables lagged the EF model.

The non-obvious part: **the failing handler (`GoodsReceivedAccountingHandler`) never sets a
VAT value.** It doesn't have to. EF Core maps every property on the entity (including those
on partial-class extensions) by convention, so it lists *all* mapped columns — `InputVat`
included — in every INSERT. A column the model knows about but the table lacks fails the
INSERT regardless of whether the code assigns it.

## Fix

Added an idempotent additive migration. `0001_initial_schema.sql` must **not** be edited —
`MariaDbSchemaInitializer` SHA-256-pins applied scripts and aborts on drift.

```sql
-- Migrations/Central/AddAccVatLedgerColumns.sql  (auto-embedded by the *.sql glob in MerchSys.App.vbproj)
ALTER TABLE `Acc_ExpenseRecords`
    ADD COLUMN IF NOT EXISTS `VatableAmount`   DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatExemptAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `ZeroRatedAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `OutputVat`       DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `InputVat`        DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatTreatment`    INT            NOT NULL DEFAULT 0;
-- (same for Acc_RevenueRecords)
```

`VatTreatment` is a VB.NET enum (`Vatable=0, Exempt=1, ZeroRated=2`) mapped to `INT` by
convention — no `HasConversion`, no column rename. Verified applied against the host DB:
all six columns present on both tables.

## Prevention

- **Post-pivot schema audit:** every SQLite-era EF migration that altered a table
  (`*_AddVatLedgerColumns`, etc.) must have a hand-written counterpart under
  `Migrations/Central/*.sql`. When you find an entity property with no matching central
  column, that is a missing migration, not a code bug.
- When you see **`Unknown column 'X' in 'field list'`** on an INSERT/UPDATE: the EF model has
  a mapped property the physical table lacks. Check partial-class extensions
  (`*Extensions.vb`) — they add mapped columns that are easy to miss when reading the base
  entity file alone.
- Never edit an already-applied numbered migration to add the column — it trips
  `MariaDbSchemaInitializer`'s drift guard. Always add a new `ADD COLUMN IF NOT EXISTS` script.

## Related

- `[[mariadb-pure-client-server-architecture]]` — the pivot that orphaned the SQLite migration
- `[[efcore-inherited-rowversion-unmapped-column]]` — same class: EF model vs. central schema mismatch
- Schema-alignment precedent: `AlignReceiptSyncColumns.sql` (INFRA-14), `AddInvSaleCogs.sql` (INFRA-22)
