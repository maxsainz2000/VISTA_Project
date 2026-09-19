---
test-id: PUR-Test-1 (PUR-14 + PUR-15 — VAT Event Pipeline)
checklist: PUR-verification-checklist.md
branch: debug/PUR-vat-ledger-columns
started: 2026-05-29T00:00
status: in-progress
---

# Debug Session — PUR Test 1 (Goods Receiving — Confirm Receipt)

## Problem Statement

Clicking **Confirm Receipt** in the Goods Receiving tab throws:

```
Microsoft.EntityFrameworkCore.DbUpdateException
  Message=Could not save changes. Please configure your entity type accordingly.
  Source=MySql.EntityFrameworkCore
Inner Exception 1:
  MySqlException: Unknown column 'InputVat' in 'field list'
```

Thrown from `GoodsReceivedAccountingHandler.Handle` → `BaseDbContext.SaveChangesAsync`,
which itself runs inside `GoodsReceivingService.ReceiveGoodsAsync` (the MediatR publish of
`GoodsReceivedEvent`).

## Starting State
- **Commit:** `6c3f81f`
- **Build status:** clean (per project state — no source changes yet)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Entities/Extensions/LedgerVatExtensions.vb` (partial classes add the VAT props)
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` (the failing INSERT)
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb` (populates the VAT cols)
  - `WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/0001_initial_schema.sql` (created the tables WITHOUT VAT cols)

## Root Cause

The `Acc_ExpenseRecords` and `Acc_RevenueRecords` tables were created in
`0001_initial_schema.sql` **before** the BIR VAT ledger feature (ACC-10) added the
six three-bucket VAT columns to the `ExpenseRecord` / `RevenueRecord` entities via the
`LedgerVatExtensions.vb` partial classes:

- `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount` — DECIMAL
- `OutputVat`, `InputVat` — DECIMAL
- `VatTreatment` — enum → INT

EF Core maps all six by convention. Every INSERT EF emits for these entities lists the full
column set — including `InputVat` — so even the *legacy* `GoodsReceivedAccountingHandler`
(which never sets a VAT value) fails because the physical table is missing the columns.

No migration was ever authored to `ALTER TABLE ... ADD COLUMN` for these. The migration
runner uses SHA-256 drift detection, so `0001_initial_schema.sql` must **not** be edited;
the fix must be a new additive migration (same pattern as INFRA-14
`AlignReceiptSyncColumns.sql` and INFRA-22 `AddInvSaleCogs.sql`).

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/AddAccVatLedgerColumns.sql` (NEW) — additive, idempotent schema-alignment migration. Auto-embedded by the existing `*.sql` glob in `MerchSys.App.vbproj` (line 37).

### Off-limits (do NOT touch)
- `0001_initial_schema.sql` — applied + hash-pinned; editing it triggers fatal drift detection.
- `SharedKernel/*` — shared contracts, not the bug source.
- Accounting entities/handlers — they are already correct; the DB schema is what lags.

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Physical `Acc_ExpenseRecords` / `Acc_RevenueRecords` tables lack the six VAT columns the EF model expects. Adding them via a new idempotent migration resolves the `Unknown column 'InputVat'` failure.
- **Changed:** Added `Migrations/Central/AddAccVatLedgerColumns.sql` — `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` for both ledger tables.
- **Build result:** clean — `Build succeeded. 0 Warning(s), 0 Error(s)`.
- **Runtime result:** Verified directly against host MariaDB. Before: `Acc_ExpenseRecords` had no VAT columns (`SELECT ... information_schema.COLUMNS` returned only the original 11). After applying `AddAccVatLedgerColumns.sql`: both `Acc_ExpenseRecords` and `Acc_RevenueRecords` now carry all six columns (`VatableAmount`/`VatExemptAmount`/`ZeroRatedAmount`/`OutputVat`/`InputVat` = decimal(18,4), `VatTreatment` = int). The `Unknown column 'InputVat'` INSERT failure can no longer occur.
- **Verdict:** ✅ fixed
- **Action:** committed on `debug/PUR-vat-ledger-columns`

---

## Resolution

- **Status:** resolved
- **Root cause:** `Acc_ExpenseRecords` / `Acc_RevenueRecords` were created in `0001_initial_schema.sql` before ACC-10 added the six BIR three-bucket VAT columns to the entities (via `LedgerVatExtensions.vb` partial classes). EF Core maps all six by convention and emits them in every INSERT, so even the legacy `GoodsReceivedAccountingHandler` (which never sets a VAT value) failed with `Unknown column 'InputVat'`. No `ALTER TABLE` migration was ever authored.
- **Fix description:** Added idempotent additive migration `Migrations/Central/AddAccVatLedgerColumns.sql` (`ADD COLUMN IF NOT EXISTS` for all six columns on both ledger tables). Auto-embedded by the existing `*.sql` glob in `MerchSys.App.vbproj`; applied + hash-recorded by `MariaDbSchemaInitializer` on next startup. Verified applied against the host DB. `0001_initial_schema.sql` was left untouched to preserve SHA-256 drift integrity.
- **Final commit:** see debug branch
- **Agent wiki entry needed?** yes — `errors/efcore-vat-ledger-columns-missing-central-schema.md`
