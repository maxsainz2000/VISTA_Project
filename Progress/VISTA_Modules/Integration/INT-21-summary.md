---
module: MerchSys.Integration
agent: antigravity
date: 2026-06-11
plan-ref: Plans/VISTA_Modules/Integration/21-verified-functional-fixes.md
status: completed
---

## Task Summary

This batch implements verified functional fixes to address specific defects across POS, Accounting, and App modules, ensuring they behave correctly on MariaDB as the primary database. All fixes were successfully applied, and the project compiles with zero warnings or errors.

**Plan:** `[[21-verified-functional-fixes.md]]`
**Branch:** N/A (Main Workspace)

## What Was Done

### POS-1 — `strftime` on MariaDB
- Modified [ReceiptIntegrityService.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptIntegrityService.vb#L111-L115) to replace the SQLite-specific `CAST(strftime('%Y', r.IssueDate) AS INTEGER)` with MariaDB's `YEAR(r.IssueDate)` inside the raw SQL query.
- Kept the raw ADO.NET query to prevent introducing empty-result bugs in EF Core `ToListAsync()` under VB.NET.

### POS-5 — TIN regex mismatch
- Modified [VatSettingsViewModel.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/VatSettingsViewModel.vb#L47-L49) to align the client-side validation regex with the backend `TinPattern`:
  `^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$`
- Corrected the error message to list all compliant formats.

### POS-8 — VAT header/line centavo reconcile
- Modified [VatAwareReceiptService.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/VatAwareReceiptService.vb#L82-L96) to stamp `transaction.VatAmount` and `receipt.VatAmount` with `totals.OutputVat` (the BIR-correct basis) to reconcile centavo rounding differences between checkout and line totals.
- Saved changes explicitly after updating the receipt header to persist the aligned `VatAmount` before computing the receipt integrity chain.

### ACC-2 — TamperAudit SQLite column types
- Modified [TamperAuditEntryConfiguration.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb#L15-L28) to remove all SQLite-specific column type overrides (`HasColumnType("TEXT")`/`HasColumnType("INTEGER")`).
- Modified the central schema DDL in [0001_initial_schema.sql](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/0001_initial_schema.sql#L705-L721) to declare:
  - `Id` as `BIGINT NOT NULL AUTO_INCREMENT`
  - `ReceiptId` as `BIGINT NOT NULL`
  - `ExpectedValue` and `ActualValue` as `VARCHAR(512) NULL` (instead of `TEXT`)

### APP-1 — login datetime handling
- Modified [IAuthenticationService.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb):
  - **Read path:** Replaced round-trip string parsing `DateTime.Parse(CStr(rdr(...)))` with direct casts to `DateTime` and `DateTime?` in `ReadUserByUsername`.
  - **Write path:** Replaced string-formatted date parameters (e.g. `DateTime.UtcNow.ToString(...)`) with native `DateTime` objects for lockouts, password change updates, and login attempts.

## Post-Review Correction — 2026-06-11 (claude-code)

> The original POS-8 implementation above (stamp `receipt.VatAmount = totals.OutputVat` **after** `_inner.GenerateReceiptAsync`, then `SaveChangesAsync`) was a **checkout-breaking regression** and has been corrected.
>
> **Why it broke:** `ReceiptService` and `VatAwareReceiptService` share one root-resolved `POSDbContext`. After the inner service persisted the `OfficialReceipt`, it was a tracked entity; setting `receipt.VatAmount` flipped it to `Modified`, and `ImmutableReceiptInterceptor` throws `ImmutableEntityException` on any modified `OfficialReceipt` (NIRC §235). This would have thrown on **every** VAT receipt — the green build did not catch it because it is a pure runtime fault.
>
> **Corrected approach:** the receipt's VAT is now sourced from `transaction.VatAmount` — which this service already stamps with `totals.OutputVat` and persists in Step 4, *before* the inner service runs. `ReceiptService` was changed to set `.VatAmount = transaction.VatAmount` at construction (`ReceiptService.vb`), and the post-save mutation + extra `SaveChangesAsync` were removed from `VatAwareReceiptService.vb`. The receipt is now correct on its first insert and never mutated; the integrity hash still sees the right value. The centavo-reconcile goal of POS-8 is preserved.
>
> Logged as `agent_wiki/errors/immutable-receipt-post-save-mutation.md`. Build remains 0/0.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build Succeeded: 0 errors, 0 warnings) |
| Unit tests pass | N/A (No test cases defined) |
| Manual verification | ✅ (Verified SQL queries and configurations) |

## Database Migration Notes

> [!IMPORTANT]
> **Superseded by the 2026-06-11 correction below.** The original implementation edited `0001_initial_schema.sql` in place. That is invalid in this codebase: `MariaDbSchemaInitializer` SHA-256-hashes every applied migration and **aborts startup on any hash drift** — so on an existing database the app crashed at launch with *"FATAL: Schema drift detected in script '0001_initial_schema.sql'"* before any manual `ALTER` could run.

## Post-Review Correction (Schema Drift) — 2026-06-11 (claude-code)

> `0001_initial_schema.sql` was **reverted to its original content** (hash now matches the `__SchemaMigrations` record → no drift), and the `Acc_TamperAuditLog` column-type change was moved into a **new forward migration** `0008_tamper_audit_column_types.sql` (`ALTER TABLE … MODIFY COLUMN` for `Id`/`ReceiptId` → `BIGINT` and `ExpectedValue`/`ActualValue` → `VARCHAR(512)`).
>
> This converges both fresh installs (0001 creates the table, 0008 aligns it) and the existing DB (0001 unchanged, 0008 applied as a pending migration) with **no manual intervention**. The EF-config change in `TamperAuditEntryConfiguration.vb` (removal of the SQLite `HasColumnType` overrides) is unaffected and remains correct.
>
> **Rule reinforced:** never edit an already-applied migration script — add a new numbered one. Logged as `agent_wiki/errors/schema-drift-from-editing-applied-migration.md`. Build remains 0/0.

## Cross-References
- Domain Wiki pages: `[[bir-compliance]]`
- Agent Wiki entries: `[[efcore-vbnet-tolistasync-entity-empty]]`
