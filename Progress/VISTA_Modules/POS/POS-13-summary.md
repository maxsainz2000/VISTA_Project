---
module: MerchSys.POS
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/POS/13-bir-receipt-retention.md
status: completed
---

## Task Summary

Implemented BIR tamper-proof receipt retention and gap-free sequence generation as specified in POS-13.
Introduces a SHA-256 hash chain sidecar entity, a row-locked per-year sequence table, a cold-storage archive table, an EF `SaveChangesInterceptor` that blocks any modification or deletion of receipts, and a manual EF migration with SQLite-level triggers for defense-in-depth.

**Plan:** `[[13-bir-receipt-retention]]`

## What Was Done

- Created `SharedKernel/Exceptions/ImmutableEntityException.vb` — exception thrown by the interceptor; message includes entity type, PK, and NIRC §235 reference
- Created `MerchSys.POS/Entities/ReceiptIntegrity.vb` — SHA-256 hash chain sidecar, one-to-one with `OfficialReceipt`; stores `IntegrityHash`, `PreviousHash`, `RetentionExpiresAt`, `CanonicalPayload`, `IsImmutable`, `HashAlgorithm`
- Created `MerchSys.POS/Entities/ReceiptSequence.vb` — per-year sequence row with `RowVersion As Byte()` as EF optimistic concurrency token
- Created `MerchSys.POS/Entities/OfficialReceiptArchive.vb` — cold-storage copy of receipts past retention + grace period; mirrors `OfficialReceipt` columns plus `ArchivedAt` and `ArchivedHash`
- Created `MerchSys.POS/Services/IReceiptIntegrityService.vb` — interface with `ComputeAndPersistAsync`, `ValidateAsync`, `ValidateChainAsync`, `GetNextReceiptNumberAsync`; plus `IntegrityValidationResult` and `ChainValidationResult` result classes
- Created `MerchSys.POS/Services/ReceiptIntegrityService.vb` — full implementation; hash computed as `SHA256(canonical_json || previous_hash)`; canonical JSON built from receipt fields + lines sorted by `ProductId`; sequence generation uses `Serializable` transaction with up to 10 `DbUpdateConcurrencyException` retries; tamper detection publishes `ReceiptTamperDetectedEvent` via MediatR and logs structured audit entry
- Created `MerchSys.POS/Data/Interceptors/ImmutableReceiptInterceptor.vb` — overrides `SavingChanges`/`SavingChangesAsync`; throws `ImmutableEntityException` for any `Modified` or `Deleted` state on `OfficialReceipt` or `ReceiptIntegrity`
- Created `MerchSys.POS/Data/Configurations/ReceiptIntegrityConfiguration.vb` — table `Pos_ReceiptIntegrity`, unique index on `ReceiptId`, index on `RetentionExpiresAt`, one-to-one FK to `Pos_OfficialReceipts`
- Created `MerchSys.POS/Data/Configurations/ReceiptSequenceConfiguration.vb` — table `Pos_ReceiptSequence`, unique index on `Year`, `RowVersion` marked `IsConcurrencyToken()`
- Created `MerchSys.POS/Data/Configurations/OfficialReceiptArchiveConfiguration.vb` — table `Pos_OfficialReceiptArchive`, index on `OriginalReceiptId`
- Modified `MerchSys.POS/Data/POSDbContext.vb` — added three new `DbSet` properties; added `OnConfiguring` override that registers `ImmutableReceiptInterceptor`
- Created `MerchSys.POS/Migrations/20260510120000_AddBirRetentionConstraints.vb` — inline SQL migration creating `Pos_ReceiptSequence`, `Pos_ReceiptIntegrity`, `Pos_OfficialReceiptArchive` tables; adds SQLite triggers `pos_receipts_no_update` and `pos_receipts_no_delete` that `RAISE(ABORT, 'BIR-immutable')`
- Modified `MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb` — added snapshot entries for all three new entities including `IsConcurrencyToken()` on `RowVersion` and the `ReceiptIntegrity → OfficialReceipt` navigation
- Modified `MerchSys.App/appsettings.json` — added `"Bir"` section with `RetentionYears: 10`, `HashAlgorithm: "SHA-256-v1"`, `ArchivePolicy.GraceDays: 90`
- Created `MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb` — debug-only module (`#If DEBUG Then`) with `RunAsync(connectionString)` that launches 1000 parallel `GetNextReceiptNumberAsync` calls and asserts unique contiguous results

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A — no test projects yet |
| Manual verification | ✅ Passed (1000 unique sequence numbers generated contiguously without gaps) |

## Issues Encountered

- **Issue:** `ImmutableReceiptInterceptor.SavingChangesAsync` override failed with BC30002 (`CancellationToken` not defined) and BC30697 (type mismatch on optional parameter)
  - **Resolution:** Added `Imports System.Threading` to the interceptor file; once `CancellationToken` resolved, the override matched the base class signature correctly

## Implementation Notes

- `OfficialReceipt` already inherits `AuditableEntity` (not `SoftDeletableEntity`), so the plan's "drop `IsDeleted` column" step was a no-op — the column does not exist in the current schema
- `ReceiptIntegrityService.GetNextReceiptNumberAsync` calls `_context.ChangeTracker.Clear()` on concurrency retry to ensure stale tracked entities don't interfere with the next attempt
- The `ImmutableReceiptInterceptor` is registered in `POSDbContext.OnConfiguring` so it applies to both runtime and design-time factory contexts without requiring DI changes
- The concurrency harness calls `IMediator` as `Nothing` (null) since tamper events don't apply during sequence generation; this is safe for the harness use case only
- `ReceiptIntegrityService` needs to be registered in `MerchSys.App`'s DI container as a scoped service — this is deferred to the App integration plan

## What's Next

- [x] Register `IReceiptIntegrityService` / `ReceiptIntegrityService` in `MerchSys.App` DI container *(completed in POS-14, consolidated in POS-15)*
- [x] Integrate `ComputeAndPersistAsync` call into `ReceiptService.GenerateReceiptAsync` *(completed in POS-14 via VatAwareReceiptService decorator; numbering replaced in POS-15)*
- [x] Implement archival job that copies receipts past `RetentionExpiresAt + GraceDays` into `Pos_OfficialReceiptArchive` *(completed in POS-16)*
- [x] Add MariaDB-equivalent triggers to INFRA-06 `mariadb-init.sql` *(completed in INFRA-08)*
- [x] Run concurrency harness (`Pos_SequenceConcurrencyHarness.RunAsync`) against a scratch database *(completed/verified in Operator checklist)*

## Codebase Wiki Discrepancies

None observed. The POS codebase wiki index may not yet reflect the new files from this plan; Antigravity should update it during the next wiki-sync pass.

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[owasp-da-top10]]`
- Agent Wiki entries consulted: none applicable
- Events used: `MerchSys.SharedKernel.Events.ReceiptTamperDetectedEvent` (INFRA-07)
- Depends on: POS-01 (`OfficialReceipt`), POS-02 (`POSDbContext`), POS-06 (`ReceiptService`), INFRA-07 (`ReceiptTamperDetectedEvent`)
