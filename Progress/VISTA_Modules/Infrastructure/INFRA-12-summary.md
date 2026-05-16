---
module: Infrastructure
agent: claude-code
date: 2026-05-16
plan-ref: Plans/VISTA_Modules/Infrastructure/12-sync-orchestrator-transmission.md
status: completed
---

## Task Summary

Implemented INFRA-12: `ISyncTransmitter` abstraction and `MariaDbSyncTransmitter` Pomelo-backed implementation. Modified `SyncOrchestrator` to delegate the actual remote write operations to `ISyncTransmitter`, replacing the prior inline per-entry `SaveChangesAsync` push pattern from INFRA-05.

**Plan:** `[[12-sync-orchestrator-transmission]]`

## What Was Done

- Created `src/MerchSys.App/Services/Sync/ISyncTransmitter.vb` — interface with `TransmitBatchAsync`, plus `TransmitResult` and `TransmitError` result types
- Created `src/MerchSys.App/Services/Sync/MariaDbSyncTransmitter.vb` — Pomelo-backed `ISyncTransmitter`; groups entries by `TableName`, wraps each group in its own transaction, handles INSERT / UPDATE / DELETE, applies financial reject-on-conflict and non-financial upsert semantics
- Modified `src/MerchSys.App/Services/SyncOrchestrator.vb` — restructured `RunForModuleAsync` into three phases (conflict resolution, batch transmission via `ISyncTransmitter`, mark-synced); removed inline `PushEntryAsync` and `ToRemoteEntity` (moved to transmitter); added `ISyncTransmitter` constructor parameter
- Modified `src/MerchSys.App/Startup/SyncConfig.vb` — registered `ISyncTransmitter` → `MariaDbSyncTransmitter` as a scoped service

## Conflict Resolution Strategy

Two-layer design: orchestrator-level and transmitter-level.

**Orchestrator layer (`IConflictResolver`)** — runs per entry before any remote write:
- `LastWriteWins` (Pur_*, Inv_*, Pos_* non-financial): pushes only if local is newer or remote doesn't exist
- `AppendOnly` (Pos_OfficialReceipts, Pos_ReceiptIntegrity, Pos_CreditPayments, Acc_*): pushes only if remote row is absent (INSERT only)
- `Reject`: flags and stops retrying after `MaxAttempts`

**Transmitter layer (`MariaDbSyncTransmitter`)** — secondary safety net at SQL execution time:
- **Non-financial tables** (Pur_*, Inv_*, Pos_* non-financial): upsert — existence check before ADD or UPDATE prevents duplicate-key errors when the same entry is transmitted more than once before being marked synced
- **Financial tables** (Pos_OfficialReceipts, Pos_ReceiptIntegrity, Pos_CreditPayments, Acc_*): INSERT is silently skipped if a remote row already exists; UPDATE and DELETE proceed normally

## Batch Processing Flow

```
SyncOrchestrator.RunForModuleAsync
│
├─ Phase 1: Conflict resolution (per entry)
│   ├─ FetchRemoteRowAsync → RemoteRowSnapshot
│   ├─ IConflictResolver.ResolveAsync → Push | Skip | Reject
│   ├─ Push  → append to toTransmit list
│   ├─ Skip  → append to toSkip list
│   └─ Reject → IncrementAttemptAsync immediately
│
├─ Phase 2: Batch transmission
│   └─ ISyncTransmitter.TransmitBatchAsync(toTransmit)
│       ├─ Group by TableName
│       └─ Per group (transaction scope):
│           ├─ INSERT (non-financial): FetchRemoteRowAsync → Add or Update
│           ├─ INSERT (financial):     FetchRemoteRowAsync → Add if absent, skip if exists
│           ├─ UPDATE: _mariaDb.Update → SaveChangesAsync
│           └─ DELETE: _mariaDb.Remove → SaveChangesAsync
│       Returns TransmitResult { SuccessCount, FailedCount, Errors }
│
└─ Phase 3: Mark synced
    ├─ Successful transmits + conflict-skipped entries → repo.MarkSyncedAsync
    ├─ Transmission failures → IncrementAttemptAsync per TransmitResult.Errors entry
    └─ SyncStatusChanged notification (Online or Error)
```

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `err` is a VB.NET built-in identifier (the global `Err` object); using it as a `For Each` loop variable caused BC30068 ("Expression is a value and cannot be target of assignment") and BC30456.
  - **Resolution:** Renamed loop variable to `txErr`.
  - **Agent Wiki entry:** `[[vbnet-err-builtin-shadows-loop-variable]]` (pre-existing entry, no new entry needed)

- **Issue:** `CancellationToken` not resolved in `ISyncTransmitter.vb` because `Imports System.Threading` was missing.
  - **Resolution:** Added missing import. The cascading effect caused the `Implements ISyncTransmitter.TransmitBatchAsync` signature in `MariaDbSyncTransmitter` not to match (BC30401 / BC30149) since the interface type was unresolved.

## What's Next

- [ ] INFRA-verification-checklist: replace `Pwd=CHANGE_ME` in `appsettings.json` with the actual MariaDB password before testing against a live MariaDB instance
- [ ] Integration test: verify `TransmitBatchAsync` idempotency — transmit the same batch twice and confirm no duplicate-key errors for non-financial tables

## Cross-References

- Domain Wiki pages consulted: `[[concepts/offline-first-sync.md]]`, `[[concepts/client-server-wpf.md]]`
- Agent Wiki entries consulted: `[[vbnet-err-builtin-shadows-loop-variable]]`

## Codebase Wiki Discrepancies

- None observed. The plan referred to `MerchSys.Infrastructure/Services/Sync/` as the target directory; the actual placement is `MerchSys.App/Services/Sync/` since `MerchSys.Infrastructure` is not a .NET project (only contains SQL migration files).
