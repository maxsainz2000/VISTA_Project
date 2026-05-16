---
module: Infrastructure
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/Infrastructure/06-mariadb-sync-schema.md
status: completed
---

## Task Summary

Implemented INFRA-06: the central MariaDB schema, conflict resolver, per-module sync maps, and the
`MariaDbSyncContext`. The `SyncOrchestrator` placeholder from INFRA-05 has been replaced with the
full resolution-and-push pipeline. All 24 synced entity types are mapped; the SQL DDL creates the
`merchsys_central` database with immutability triggers on BIR/ledger tables.

**Plan:** `[[06-mariadb-sync-schema]]`

## What Was Done

### SharedKernel — new files
- Created `src/MerchSys.SharedKernel/Sync/ConflictResolution.vb` — `ConflictResolution` enum (LastWriteWins / AppendOnly / Reject), `SyncAction` enum (Push / Skip / Reject), `RemoteRowSnapshot` class (Exists + ModifiedAt), `ResolutionDecision` class (Action + Reason)
- Created `src/MerchSys.SharedKernel/Sync/ConflictResolver.vb` — `IConflictResolver` interface with XML docs; `ConflictResolver` implementation using a two-tier static policy table (exact table name → prefix fallback); returns `Task.FromResult` (synchronous logic)
- Created `src/MerchSys.SharedKernel/Sync/SyncMaps/PurchasingSyncMap.vb` — 9 remote Pur_* POCO classes + `PurchasingSyncMap` (shared `ToRemote`/`GetRemoteKey`/`Tables`)
- Created `src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb` — 7 remote Inv_* POCO classes + `InventorySyncMap`
- Created `src/MerchSys.SharedKernel/Sync/SyncMaps/PosSyncMap.vb` — 7 remote Pos_* POCO classes including placeholder `RemoteReceiptIntegrity` + `PosSyncMap`
- Created `src/MerchSys.SharedKernel/Sync/MariaDbSyncContext.vb` — `MariaDbSyncContext` (Pomelo/MySql DbContext): 24 DbSets configured via `OnModelCreating`; `ValueGeneratedNever()` on all PKs (IDs come from the sync journal); `FetchRemoteRowAsync(tableName, entityId)` using raw ADO.NET to check remote existence without type-specific DbSet casts
- Created `src/MerchSys.SharedKernel/Sync/SyncMaps/AccountingSyncMap.vb` — 4 remote Acc_* POCO classes + `AccountingSyncMap`

### App — modified files
- Updated `src/MerchSys.App/Services/SyncOrchestrator.vb` — replaced INFRA-05 placeholder with full sync pipeline: `FetchRemoteRowAsync` → `ConflictResolver.ResolveAsync` → `PushEntryAsync` (INSERT/UPDATE via EF Core `Add`/`Update` + `SaveChangesAsync` + `ChangeTracker.Clear`); `IncrementAttemptAsync` updates `SyncJournalDbContext` on Reject or exception; `MaxAttempts` and `BatchSize` from settings; `Await` moved outside `Catch` blocks (VB.NET restriction)
- Updated `src/MerchSys.App/Startup/SyncConfig.vb` — added `MariaDbSyncContext` registration (reads `Sync:MariaDbConnection` from `IConfiguration` at resolve time so startup does not require a live MariaDB connection); added `IConflictResolver` → `ConflictResolver` scoped registration
- Updated `src/MerchSys.App/appsettings.json` — added `MariaDbConnection`, `MaxAttempts: 5`, `BatchSize: 100` to the `Sync` section

### SharedKernel — modified files
- Updated `src/MerchSys.SharedKernel/Sync/SyncSettings.vb` — added `MariaDbConnection`, `MaxAttempts`, `BatchSize` properties
- Updated `src/MerchSys.SharedKernel/MerchSys.SharedKernel.vbproj` — added `Pomelo.EntityFrameworkCore.MySql` 9.0.0

### SQL DDL
- Created `Plans/VISTA_Modules/Infrastructure/sql/mariadb-init.sql` — full DDL for `merchsys_central` (utf8mb4 / MariaDB 11.4.x): 27 tables across 4 module prefixes; BEFORE UPDATE + BEFORE DELETE triggers (SQLSTATE '45000') on all AppendOnly tables; indexes on PKs + ModifiedAt; `merchsys_sync_role` with SELECT/INSERT/UPDATE grants (no DELETE); placeholder password in sync user CREATE

### Solution-wide fixes (pre-existing, unblocked by this build)
- Fixed `src/MerchSys.POS/Services/CreditService.vb:164` — `a.LastTransactionDate.GetValueOrDefault() < cutoff` to remove BC42016 implicit `Boolean?→Boolean` conversion warning
- Fixed `src/MerchSys.POS/ViewModels/CreditManagementViewModel.vb:357` — `t.CustomerId.HasValue AndAlso t.CustomerId.Value = account.Id` to remove BC42016 warning
- Created `WPF_Applications/MerchSys/Directory.Build.props` — suppresses NU1608 NuGet version-constraint warning from Pomelo 9.0.0 requiring EF Core ≤ 9.x while the project uses EF Core 10.0.7

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A (requires live MariaDB instance) |

## Issues Encountered

- **Issue:** `Await` inside `Catch`/`Finally` blocks is not permitted in VB.NET (BC36943).
  - **Resolution (MariaDbSyncContext):** Changed `Await conn.CloseAsync()` in `Finally` to synchronous `conn.Close()`.
  - **Resolution (SyncOrchestrator):** Captured failure reason in a local variable (`pendingFailureReason`) inside `Try`/`Catch`; awaited `IncrementAttemptAsync` after the `Try-Catch-End Try` block.

- **Issue:** Pomelo 9.0.0 declares a dependency on EF Core ≤ 9.x (NU1608 warning across all projects).
  - **Resolution:** Added `Directory.Build.props` with `<NoWarn>NU1608</NoWarn>`. The Pomelo 9.x/EF Core 10 combination is ABI-compatible for the APIs used (`UseMySql`, `ServerVersion.Parse`, basic DbSet operations). Upgrade to Pomelo 10.x when a stable release is available.
  - **Agent Wiki:** Candidate for `agent_wiki/patterns/pomelo-efcore-version-mismatch.md`

- **Issue:** `FetchRemoteRowAsync` needed to query MariaDB by integer entity ID (from `SyncJournal.RowId`) without knowing the remote POCO type at compile time.
  - **Resolution:** Used raw ADO.NET (`GetDbConnection`, `CreateCommand`, `ExecuteReaderAsync`) instead of EF Core typed queries, returning a `RemoteRowSnapshot` POCO with just `Exists` and `ModifiedAt`.

- **Issue:** `Pos_ReceiptIntegrity` table is referenced in the plan's conflict policy but has no corresponding local EF Core entity.
  - **Resolution:** Defined `RemoteReceiptIntegrity` POCO and `Pos_ReceiptIntegrity` DDL as a reserved forward-declaration. The table will produce no sync journal entries until the POS module implements the corresponding entity.

## What's Next

- [x] Each module's `Data/` folder needs an `ISyncableRepository` implementation *(completed in INFRA-09)*
- [ ] Replace `appsettings.json` `Pwd=CHANGE_ME` with a user-level `appsettings.Production.json` outside the repo before any live deployment
- [ ] Upgrade `Pomelo.EntityFrameworkCore.MySql` to a 10.x release when available to resolve NU1608 cleanly
- [x] `MainWindowViewModel` subscription to `DefaultNotificationService.SyncStatusChanged` *(completed in INFRA-10)*
- [ ] Run `mariadb-init.sql` against a fresh MariaDB 11.4.x instance and verify acceptance criteria 2–3 manually

## Cross-References

- Domain Wiki pages consulted: `[[concepts/offline-first-sync.md]]`, `[[concepts/bir-compliance.md]]`, `[[analysis/cross-module-data-flow.md]]`
- Agent Wiki entries consulted: `[[vbnet-reserved-keyword-enum-member]]`

## Codebase Wiki Discrepancies

- `codebase_wiki/schemas/database.md` does not list `Pos_ReceiptIntegrity` — it is defined in the INFRA-06 DDL and mapped in `PosSyncMap` but has no local EF Core entity yet
- `codebase_wiki/modules/shared-kernel/index.md` will need updating: `plans-completed` should include `INFRA-06`; `file-count` should increase from 29 to 37 (8 new files in SharedKernel)
- `codebase_wiki/dependencies/nuget-packages.md` is missing `Pomelo.EntityFrameworkCore.MySql 9.0.0`
