---
module: MerchSys.Infrastructure
plan-id: INFRA-27
title: "Decommission Sync Layer + Remove SQLite"
depends-on: [INFRA-25, INFRA-26]
estimated-files: 40
priority: high
amendment-ref: AMD-2026-05-28-01
---

# INFRA-27: Decommission Sync Layer + Remove SQLite

## Context

After INFRA-25 (DbContexts on MariaDB) and INFRA-26 (concurrency), the sync infrastructure is dead code. This plan deletes it and removes every remaining SQLite reference.

This is the largest single delete in the project's history. Treat it as one logical change: remove all of it together, in one PR, with a clean build. Half-removed sync code is worse than fully-present sync code because it leaves dangling references and confused intent.

## Prerequisites

- **INFRA-25** — Module DbContexts on MariaDB; `SyncWorker` already disabled.
- **INFRA-26** — Concurrency tokens + FOR UPDATE in place; no remaining dependency on sync for correctness.

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` §2.5 (Removed Components)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (Removed Components — Supersession)
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md`

## Deliverables — Files to DELETE

### Sync infrastructure (SharedKernel)
```
MerchSys.SharedKernel/Sync/SyncJournal.vb
MerchSys.SharedKernel/Sync/SyncJournalDbContext.vb
MerchSys.SharedKernel/Sync/SyncProbeResult.vb
MerchSys.SharedKernel/Sync/SyncSettings.vb
MerchSys.SharedKernel/Sync/SyncStatus.vb
MerchSys.SharedKernel/Sync/ConflictResolution.vb
MerchSys.SharedKernel/Sync/ConflictResolver.vb
MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb
MerchSys.SharedKernel/Sync/SyncMaps/PosSyncMap.vb
MerchSys.SharedKernel/Sync/SyncMaps/PurchasingSyncMap.vb
MerchSys.SharedKernel/Sync/SyncMaps/AccountingSyncMap.vb
MerchSys.SharedKernel/Persistence/ISyncableRepository.vb
MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb
MerchSys.SharedKernel/Persistence/SyncJournalDescriptor.vb
```

### Sync orchestration (App)
```
MerchSys.App/Services/SyncOrchestrator.vb
MerchSys.App/Services/SyncWorker.vb
MerchSys.App/Services/Sync/MariaDbSyncContext.vb
MerchSys.App/Services/Sync/MariaDbSyncTransmitter.vb
MerchSys.App/Services/Sync/ISyncTransmitter.vb            ' if exists
MerchSys.App/Startup/SyncableRepositoryRegistration.vb
MerchSys.App/Startup/SyncConfig.vb
```

### Sync UI (App)
```
MerchSys.App/ViewModels/Shell/SyncStatusIndicatorViewModel.vb
MerchSys.App/Views/Shell/SyncStatusIndicator.xaml
MerchSys.App/Views/Shell/SyncStatusIndicator.xaml.vb
```

(`SyncStatusIndicator` is replaced by `ConnectionStatusIndicator` in INFRA-28. Delete here; create new there.)

### Network probe + helpers
```
MerchSys.App/Services/NetworkAvailabilityChanged.vb       ' or wherever the probe lives
MerchSys.SharedKernel/Interfaces/INotificationService.vb  ' MOD only — remove NotifySyncStatusChanged
MerchSys.App/Services/DefaultNotificationService.vb       ' MOD — remove NotifySyncStatusChanged impl
```

### SQLite-era schema bootstrap + migrations
```
MerchSys.App/Data/DatabaseInitializer.vb                  ' DELETE — replaced by MariaDbSchemaInitializer (INFRA-24)
MerchSys.App/Data/DatabaseConfig.vb                       ' DELETE — SQLite path config no longer needed
MerchSys.Purchasing/Migrations/                           ' DELETE entire directory
MerchSys.Inventory/Migrations/                            ' DELETE entire directory
MerchSys.POS/Migrations/                                  ' DELETE entire directory
MerchSys.Accounting/Migrations/                           ' DELETE entire directory
```

### Module handler call sites (modify, not delete)
Every call to `ISyncableRepository.SaveChangesWithJournalAsync(...)` rewrites to `_db.SaveChangesAsync(...)`. Affected files include (search-and-replace, verify each):
```
MerchSys.Inventory/Handlers/SaleCompletedHandler.vb
MerchSys.Inventory/Handlers/GoodsReceivedHandler.vb
MerchSys.Inventory/Handlers/ShrinkageHandler.vb
MerchSys.Purchasing/Handlers/...
MerchSys.POS/Handlers/...
MerchSys.Accounting/Handlers/SaleRevenueHandler.vb
```

(Exact list determined during implementation via `Grep "SaveChangesWithJournalAsync"`.)

### `appsettings.json` cleanup

Remove `Sync` block entirely (`ProbeIntervalSeconds`, `MariaDbConnectionString` separate from the main one, `MaxAttempts`, `BatchSize`, etc.). Keep only `ConnectionStrings:MerchSysCentral` and any non-sync config.

### Project file cleanup

Remove from any `.vbproj` that referenced them:
```xml
<PackageReference Include="MySqlConnector" Version="..." />   <!-- IF only used by sync transmitter -->
```

`MySqlConnector` is **kept** in `MerchSys.App` because `MariaDbSchemaInitializer` uses it (INFRA-24). Verify before removing.

### Operator checklist + skill references

```
.claude/skills/debug-test/SKILL.md                        ' MOD — remove SQLite/sync-specific debug steps
Operator/manager-verification-checklist.md                ' MOD — remove "Sync Journal" check sub-rows, MariaDB-sync sub-checks
Operator/testing-session-protocol.md                      ' MOD — remove SQLite + sync references
```

The MariaDB sub-checks in `manager-verification-checklist.md` Part 8 stay; the wording changes from "after sync" to "immediately" (no sync latency).

## Specification

### Deletion order (to keep the build green)

1. Comment out (or stub) `ISyncableRepository` and its registration. Build.
2. Delete all `MerchSys.App/Services/Sync*` files. Build — should fail only on direct references which were already commented in step 1.
3. Delete all `MerchSys.SharedKernel/Sync*` files. Build — handler call sites should already use `SaveChangesAsync` directly (rewritten in step 1).
4. Delete the per-module `Migrations/` folders. Build.
5. Delete `DatabaseInitializer.vb` and `DatabaseConfig.vb`. Build.
6. Search for residual `SqliteConnection`, `Microsoft.Data.Sqlite`, `Sync_Journal`, `merchsys.db`, `LastWriteWins` references — should yield zero hits.
7. Final build: 0 errors / 0 warnings.

### Handler rewrite pattern

Before:
```vb
Await _syncableRepo.SaveChangesWithJournalAsync(_db, journalDescriptor)
```

After:
```vb
Await _db.SaveChangesAsync()
```

If any handler relied on `journalDescriptor` to encode something other than journaling (audit-trail metadata, etc.), that data is captured via the audit columns or `Acc_TamperAuditLog` directly — verify nothing material is lost.

### `merchsys.db` cleanup on client laptops

The old SQLite file at `%LOCALAPPDATA%\MerchSys\merchsys.db` remains on disk after the app stops creating it. Document in the runbook (INFRA-29) that the file is safe to delete manually after the client first launches against MariaDB successfully.

Do **not** add code to auto-delete it — silent file deletion is the kind of thing that horrifies operators 6 months later when they're trying to recover something. Manual is fine.

### Test artifacts

If `Operator/debug-logs/` contains active session logs referencing sync test cases (Test 6 "Transmission idempotency" etc.), annotate those logs as historical-post-2026-05-28 rather than deleting them.

## Acceptance Criteria

1. `Grep "SqliteConnection"` returns zero hits.
2. `Grep "Microsoft.Data.Sqlite"` returns zero hits.
3. `Grep "EntityFrameworkCore.Sqlite"` returns zero hits.
4. `Grep "Sync_Journal"` returns zero hits.
5. `Grep "ISyncableRepository"` returns zero hits.
6. `Grep "SyncOrchestrator"` returns zero hits.
7. `Grep "merchsys.db"` returns zero hits in `/src/`.
8. `Grep "LastWriteWins"` returns zero hits.
9. Build: 0 errors / 0 warnings.
10. App launches against MariaDB, login works, Stock Dashboard renders 20 products, sale checkout writes to MariaDB. Smoke test matches INFRA-25 smoke test.
11. `appsettings.json` has no `Sync:*` section.
12. Per-module `.vbproj` files have no SQLite or sync-only NuGet references.

## Out of Scope (Defer)

- Connection-status indicator UI → **INFRA-28**.
- Operational runbook → **INFRA-29**.
- Master-detail rail sidebar → **INFRA-30**.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-27-summary.md` per `Progress/_template.md`. Include:

- Full list of files deleted (with line counts removed).
- Full list of files modified with diff summary.
- Output of each grep check above proving zero hits.
- A note that prior `Operator/debug-logs/` session logs referencing sync were annotated as historical (list affected log filenames).
- A reminder line at the top: "After this plan ships, future agents should treat any commit re-introducing SQLite or sync as a regression."

### Documentation

- Update `CLAUDE.md` — remove the "Legacy SQLite + sync code (being removed)" paragraph (no longer accurate after this plan ships).
- Update `LLM_Wiki/agent_wiki/index.md` — change the three "historical (sync layer removed)" status entries from a future tense to "removed in INFRA-27".
- Append entry to `LLM_Wiki/agent_wiki/log.md` confirming the deletion.
- Update `LLM_Wiki/wiki/concepts/offline-first-sync.md` superseded-by status from "scheduled" to "completed" (if there's such a distinction).
