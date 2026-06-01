---
module: Infrastructure
agent: antigravity + claude-code
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/27-sync-layer-decommission.md
status: completed
---

> **Regression guard:** After this plan ships, any commit re-introducing SQLite or sync-layer code (ISyncableRepository, SyncOrchestrator, Sync_Journal, etc.) is a regression.

## Task Summary

Complete decommission of the SQLite offline-first sync layer per AMD-2026-05-28-01. 127 files changed, ~9,150 lines removed. Antigravity completed the bulk of the deletion; claude-code completed residual source-level cleanup (NoSyncAttribute, DatabaseInitializer, stale XML doc comments).

**Plan:** `[[27-sync-layer-decommission]]`

## What Was Done

### Deleted Files (selected — full list in git log)

**SharedKernel Sync infrastructure:**
- `MerchSys.SharedKernel/Sync/SyncJournal.vb`
- `MerchSys.SharedKernel/Sync/SyncJournalDbContext.vb`
- `MerchSys.SharedKernel/Sync/SyncProbeResult.vb`
- `MerchSys.SharedKernel/Sync/SyncSettings.vb`
- `MerchSys.SharedKernel/Sync/SyncStatus.vb`
- `MerchSys.SharedKernel/Sync/ConflictResolution.vb`
- `MerchSys.SharedKernel/Sync/ConflictResolver.vb`
- `MerchSys.SharedKernel/Sync/DualConditionSyncProbe.vb`
- `MerchSys.SharedKernel/Sync/ISyncProbe.vb`
- `MerchSys.SharedKernel/Sync/ISyncableRepository.vb`
- `MerchSys.SharedKernel/Sync/MariaDbSyncContext.vb`
- `MerchSys.SharedKernel/Sync/SyncMaps/AccountingSyncMap.vb`
- `MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb`
- `MerchSys.SharedKernel/Sync/SyncMaps/PosSyncMap.vb`
- `MerchSys.SharedKernel/Sync/SyncMaps/PurchasingSyncMap.vb`
- `MerchSys.SharedKernel/Persistence/ISyncableRepository.vb`
- `MerchSys.SharedKernel/Persistence/SyncJournalDescriptor.vb`
- `MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb`
- `MerchSys.SharedKernel/Persistence/NoSyncAttribute.vb` *(completed in cleanup pass)*

**SQLite-era migrations (per-module):**
- All `Migrations/` directories deleted from Purchasing, Inventory, POS, Accounting modules

**Module syncable repositories:**
- `MerchSys.Accounting/Data/AccountingSyncableRepository.vb`
- `MerchSys.POS/Data/PosSyncableRepository.vb`
- `MerchSys.Purchasing/Data/PurchasingSyncableRepository.vb`

**Debug/test harnesses:**
- `MerchSys.Accounting/Debug/VatLedgerSchemaHarness.vb`
- `MerchSys.Accounting/Debug/VatLedgerSchemaHarnessRunner.vb`
- `MerchSys.Accounting/Debug/VatTileSmokeHarness.vb`
- `MerchSys.POS/Debug/ReceiptArchivalHarness.vb`
- `MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb`

**App SQLite bootstrap:**
- `MerchSys.App/Data/DatabaseInitializer.vb` *(completed in cleanup pass)*

### Modified Files (selected)

- All module handlers (`CreditPaymentAccountingHandler`, `GoodsReceivedAccountingHandler`, `SaleRevenueHandler`, `ShrinkageAccountingHandler`, Inventory handlers, POS services, Purchasing services) — rewrote `SaveChangesWithJournalAsync` → `SaveChangesAsync`
- `MerchSys.App/Data/DatabaseConfig.vb` — removed SQLite `DatabasePath` property; updated XML doc to reflect MariaDB-only context; removed unused `System.IO` import
- `MerchSys.SharedKernel/Interfaces/INotificationService.vb` — removed `NotifySyncStatusChanged` member
- `MerchSys.App/Services/DefaultNotificationService.vb` — removed `NotifySyncStatusChanged` implementation
- `MerchSys.Accounting/Entities/TamperAuditEntry.vb` — removed `<NoSync>` attribute and `Sync_Journal` doc reference *(cleanup pass)*
- `MerchSys.POS/Entities/ReceiptIntegrity.vb` — removed `<NoSync>` attribute and `Sync_Journal` doc reference *(cleanup pass)*
- `MerchSys.App/Services/IAuthenticationService.vb` — removed stale `DatabaseInitializer` comment reference *(cleanup pass)*
- `CLAUDE.md` — updated architecture notes; removed "Legacy SQLite + sync code (being removed)" paragraph
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` — updated to reflect completed pivot

## Grep Verification (INFRA-27 Acceptance Criteria)

All checks run against `/WPF_Applications/MerchSys/src/` excluding `bin/` and `obj/`:

| Check | Result |
|---|---|
| `SqliteConnection` in source | ✅ 0 hits |
| `Microsoft.Data.Sqlite` in source | ✅ 0 hits |
| `EntityFrameworkCore.Sqlite` in source | ✅ 0 hits |
| `Sync_Journal` in source | ✅ 0 hits (comments updated) |
| `ISyncableRepository` in source | ✅ 0 hits |
| `SyncOrchestrator` in source | ✅ 0 hits |
| `LastWriteWins` in source | ✅ 0 hits |
| `Sync:` in `appsettings.json` | ✅ 0 hits |

*(Hits in `bin/`/`obj/` are stale Release build artifacts from before INFRA-25; they disappear on next Release build.)*

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A (testing phase separate) |

## Issues Encountered

- **Issue:** Token exhaustion during Antigravity's pass left `NoSyncAttribute.vb`, `DatabaseInitializer.vb`, and `<NoSync>` attribute usages as residual sync references.
  - **Resolution:** claude-code cleanup pass: deleted both files, stripped `<NoSync>` decorations from `TamperAuditEntry` and `ReceiptIntegrity`, updated stale comments.

## What's Next

- [x] INFRA-29: Operational Runbook *(completed 2026-05-28 — 4 runbooks + PowerShell backup script + Task Scheduler XML)*
- [x] INFRA-30: Master-Detail Activity Rail Sidebar *(completed 2026-05-28 — 60px rail + 220px module detail panel, keyboard shortcuts Ctrl+1–4)*
- [x] Operator: delete `%LOCALAPPDATA%\MerchSys\merchsys.db` on each client after first successful MariaDB launch (manual, per runbook) *(completed during operator setup)*

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`, `[[system_plan_amendment_2026-05-28]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`
