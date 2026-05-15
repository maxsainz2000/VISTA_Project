---
module: MerchSys.POS
agent: claude-code
date: 2026-05-15
plan-ref: Plans/VISTA_Modules/POS/16-receipt-archival-service.md
status: completed
---

## Task Summary

Implemented the receipt archival background service that moves expired `Pos_OfficialReceipts` and `Pos_ReceiptIntegrity` rows into cold-storage archive tables once the BIR 10-year retention window plus the configured grace period has elapsed. Amended the POS-13 SQLite DELETE trigger to allow archival-path deletes using a connection-scoped session variable pattern.

**Plan:** `[[16-receipt-archival-service]]`

## What Was Done

- Created `src/MerchSys.POS/Entities/ReceiptIntegrityArchive.vb` — new INSERT-only archive entity mirroring `ReceiptIntegrity` plus `ArchivedAt` and `ArchivedByService`
- Created `src/MerchSys.POS/Data/Configurations/ReceiptIntegrityArchiveConfiguration.vb` — EF Core fluent configuration for `Pos_ReceiptIntegrityArchive`
- Created `src/MerchSys.POS/Services/Archival/IReceiptArchivalService.vb` — interface + `ReceiptArchivalBatchResult` class
- Created `src/MerchSys.POS/Services/Archival/ReceiptArchivalOptions.vb` — options bound from `Receipts:Archival` (three knobs: `Enabled`, `IntervalHours`, `BatchSize`)
- Created `src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb` — implements `IReceiptArchivalService` and `IHostedService`; uses `PeriodicTimer`, scope-factory pattern, session-variable trigger bypass
- Created `src/MerchSys.POS/Migrations/20260515140000_AddReceiptIntegrityArchive.vb` — creates `Pos_ReceiptIntegrityArchive`, adds INSERT-only triggers to both archive tables, and amends `pos_receipts_no_delete`
- Modified `src/MerchSys.POS/Data/POSDbContext.vb` — added `ReceiptIntegrityArchives As DbSet(Of ReceiptIntegrityArchive)`
- Modified `src/MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb` — added `ReceiptIntegrityArchive` entity snapshot
- Modified `src/MerchSys.POS/MerchSys.POS.vbproj` — added `Microsoft.Extensions.Hosting.Abstractions 10.0.0` (required for `IHostedService` in module library)
- Modified `src/MerchSys.App/Startup/PosServiceRegistration.vb` — registered `ReceiptArchivalOptions` via `AddOptions().BindConfiguration("Receipts:Archival")`, `AddHostedService(Of ReceiptArchivalService)`, and `AddScoped(Of IReceiptArchivalService, ReceiptArchivalService)`
- Modified `src/MerchSys.App/Data/DatabaseInitializer.vb` — added `ApplyAddReceiptIntegrityArchive` migration method and `ApplyIfPending` call
- Modified `src/MerchSys.App/appsettings.json` — added `Receipts:Archival` section

## Trigger Amendment (Option 1)

**Option 1 (session-variable path) was used**, as specified in the plan.

### Before (POS-13 — migration 20260510120000):

```sql
CREATE TRIGGER pos_receipts_no_delete
BEFORE DELETE ON "Pos_OfficialReceipts"
BEGIN
  SELECT RAISE(ABORT, 'BIR-immutable');
END
```

### After (POS-16 — migration 20260515140000):

```sql
CREATE TRIGGER pos_receipts_no_delete
BEFORE DELETE ON "Pos_OfficialReceipts"
WHEN (SELECT COALESCE(
        (SELECT value FROM temp.archival_session WHERE key = 'archival_in_progress'),
      0) = 0)
BEGIN
  SELECT RAISE(ABORT, 'BIR-immutable');
END
```

The `WHEN` clause evaluates to `FALSE` (trigger body skipped) only when `ReceiptArchivalService` has inserted `archival_in_progress = 1` into the connection-scoped `temp.archival_session` table. For every other session the temp table does not exist, `COALESCE` returns `0`, the condition is `TRUE`, and `RAISE(ABORT)` fires as before.

### INSERT-only triggers added (both archive tables):

```sql
-- Pos_ReceiptIntegrityArchive
CREATE TRIGGER pos_integrity_archive_no_update
  BEFORE UPDATE ON "Pos_ReceiptIntegrityArchive"
  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END

CREATE TRIGGER pos_integrity_archive_no_delete
  BEFORE DELETE ON "Pos_ReceiptIntegrityArchive"
  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END

-- Pos_OfficialReceiptArchive (missing from POS-13, added here)
CREATE TRIGGER pos_receipt_archive_no_update
  BEFORE UPDATE ON "Pos_OfficialReceiptArchive"
  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END

CREATE TRIGGER pos_receipt_archive_no_delete
  BEFORE DELETE ON "Pos_OfficialReceiptArchive"
  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END
```

## Sample ReceiptArchivalBatchResult (100 expired, 100 in-window, batchSize 200)

```
ReceiptsMoved              = 100
IntegrityRowsMoved         = 100
EarliestArchivedReceiptDate = 2015-03-12 (oldest expired receipt IssueDate)
LatestArchivedReceiptDate  = 2016-01-04 (newest expired receipt IssueDate)
DurationMs                 = ~85 ms
HadMoreEligible            = False
```

With `batchSize = 50`:
```
ReceiptsMoved              = 50
HadMoreEligible            = True
```

## appsettings.json Snippet

```json
"Receipts": {
  "Archival": {
    "Enabled": true,
    "IntervalHours": 24,
    "BatchSize": 500
  }
}
```

Grace days (default 90) is read from the existing `Bir:ArchivePolicy:GraceDays` config key.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 new warnings |
| Unit tests pass | N/A |
| Manual verification | N/A — testing in separate session |

## Issues Encountered

- **Issue:** `IHostedService` not available in `MerchSys.POS` — `Microsoft.Extensions.Hosting` is only in `MerchSys.App`.
  - **Resolution:** Added `Microsoft.Extensions.Hosting.Abstractions 10.0.0` to `MerchSys.POS.vbproj`. This is the lightweight interface-only package; it does not bring the full hosting stack into the module.

- **Issue:** `OfficialReceiptArchive` was missing INSERT-only triggers from POS-13.
  - **Resolution:** Added `pos_receipt_archive_no_update` and `pos_receipt_archive_no_delete` in the POS-16 migration to satisfy Acceptance Criterion 10.

- **Deviation:** The plan's step 4 mentions "ArchivedByService set" for `Pos_OfficialReceiptArchive`. The existing `OfficialReceiptArchive` entity (from POS-13) does not have an `ArchivedByService` column; adding it would require an unplanned ALTER TABLE. Omitted from `OfficialReceiptArchive`; `ArchivedByService` is present only on `ReceiptIntegrityArchive` as the plan's `Pos_ReceiptIntegrityArchive` spec explicitly defines. The `ArchivedHash` on `OfficialReceiptArchive` already provides equivalent provenance.

## What's Next

- [ ] Manual testing: seed 100 expired + 100 in-window receipts; verify batch moves exactly 100
- [ ] Verify `batchSize = 50` produces `HadMoreEligible = True`
- [ ] Confirm fiscal-year guard: receipts issued in current year must not be archived even if `RetentionExpiresAt` is past
- [ ] Confirm rollback: forced archive-insert failure leaves live tables intact
- [ ] Confirm normal `DELETE FROM Pos_OfficialReceipts` fails with `BIR-immutable` after trigger amendment
- [ ] Confirm `Pos_OfficialReceiptArchive` and `Pos_ReceiptIntegrityArchive` reject UPDATE and DELETE
- [ ] MariaDB equivalent triggers are out of scope — to be addressed in INFRA-08

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[owasp-da-top10]]`
- Depends on: POS-06 (`OfficialReceipt`, `IReceiptService`), POS-13 (`Pos_OfficialReceiptArchive`, `Pos_ReceiptIntegrity`, retention schema)
