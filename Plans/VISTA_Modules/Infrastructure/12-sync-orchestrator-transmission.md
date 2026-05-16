---
module: MerchSys.Infrastructure
plan-id: INFRA-12
title: "SyncOrchestrator Real Data Transmission"
depends-on: [INFRA-05, INFRA-06, INFRA-09]
estimated-files: 3
priority: medium
---

# SyncOrchestrator Real Data Transmission

## Context

INFRA-05 delivered the `SyncWorker` background service and `SyncOrchestrator` with a **placeholder** `MarkSyncedAsync` stub — the push loop detects connectivity and iterates `Sync_Journal` entries, but the actual MariaDB INSERT/UPDATE/DELETE transmission is a no-op. INFRA-06 delivered the MariaDB central schema and reconciliation tables. INFRA-09 delivered `ISyncableRepository` implementations that populate `Sync_Journal` on local writes.

The 2026-05-15 Infrastructure audit identified this as the missing middle piece: `SyncOrchestrator.RunAsync` must be upgraded to perform real data transmission from `Sync_Journal` entries to the corresponding MariaDB central tables.

## Prerequisites

- **INFRA-05** (Sync Worker & Connectivity Probe) — `SyncOrchestrator`, `SyncWorker`, `Sync_Journal` schema, connectivity probe
- **INFRA-06** (MariaDB Central Schema) — Central table definitions, `appsettings.json` MariaDB connection string
- **INFRA-09** (ISyncableRepository) — `Sync_Journal` is now populated by module write paths, `SyncJournalDescriptor` model

## Wiki References

- `concepts/offline-first-sync.md` — Local-first model; central is a replica
- `concepts/client-server-wpf.md` — Conflict resolution: last-writer-wins for non-financial, reject-on-conflict for financial

## Deliverables

```
MerchSys.Infrastructure/Services/Sync/
├── SyncOrchestrator.vb                         ' Modified — replace stub with real transmission
├── ISyncTransmitter.vb                         ' New — abstraction for table-level push
└── MariaDbSyncTransmitter.vb                   ' New — Pomelo-based implementation
```

## Specification

### ISyncTransmitter

```vb
Public Interface ISyncTransmitter
    Function TransmitBatchAsync(
        entries As IReadOnlyList(Of SyncJournalEntry),
        cancellationToken As CancellationToken
    ) As Task(Of TransmitResult)
End Interface

Public Class TransmitResult
    Public Property SuccessCount As Integer
    Public Property FailedCount As Integer
    Public Property Errors As IReadOnlyList(Of TransmitError)
End Class
```

### MariaDbSyncTransmitter

Implementation using the Pomelo MariaDB `DbContext` (from INFRA-06):

1. Group entries by `TableName`.
2. For each group, map `OperationKind` to the appropriate SQL:
   - `Insert` → `INSERT INTO central_<table> ... ON DUPLICATE KEY UPDATE ...` (upsert for idempotency)
   - `Update` → `UPDATE central_<table> SET ... WHERE PK = ...`
   - `Delete` → `DELETE FROM central_<table> WHERE PK = ...`
3. Execute within a transaction per batch.
4. On success, mark transmitted `Sync_Journal` entries as `SyncedAt = DateTime.UtcNow`.

### SyncOrchestrator.RunAsync Modification

Replace the `MarkSyncedAsync` stub with:

```vb
' 1. Query pending Sync_Journal entries (SyncedAt IS NULL), ordered by OccurredAt
' 2. Batch into groups of 100
' 3. For each batch: Await _transmitter.TransmitBatchAsync(batch, ct)
' 4. On success: update SyncedAt on transmitted entries
' 5. On partial failure: log errors, continue with next batch
' 6. Publish SyncStatusChanged notification with counts
```

### Conflict Handling

- **Non-financial tables** (Inventory, Purchasing): last-writer-wins via `ON DUPLICATE KEY UPDATE`.
- **Financial tables** (POS receipts, Accounting entries): reject-on-conflict — if the central row already exists with a different hash, log a conflict entry and skip. Do not overwrite financial data.
- Financial table detection: check if `TableName` starts with `Pos_Official` or `Acc_` — these use reject semantics.

## Implementation Notes

- The `Pwd=CHANGE_ME` placeholder in `appsettings.json` must be replaced before this plan can be tested against a real MariaDB instance. That is an operator task tracked in `INFRA-verification-checklist.md`.
- Batch size of 100 is a starting point; make it configurable via `appsettings.json` under `Sync:BatchSize`.
- The transmitter must handle `MySqlException` for connection failures gracefully — log and retry on next `SyncWorker` cycle, do not crash the background service.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally`.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `ISyncTransmitter` is a public abstraction in `MerchSys.Infrastructure`.
3. `MariaDbSyncTransmitter.TransmitBatchAsync` correctly maps Insert/Update/Delete operations.
4. After transmission, `Sync_Journal` entries have `SyncedAt` populated.
5. Financial tables use reject-on-conflict semantics.
6. Non-financial tables use upsert semantics.
7. Connection failures are logged but do not crash `SyncWorker`.
8. Batch size is configurable via `appsettings.json`.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-12-summary.md` using `Progress/_template.md`. Include:
- The conflict resolution strategy for financial vs. non-financial tables.
- The batch processing flow diagram or pseudocode.

### Documentation
- XML doc on `ISyncTransmitter` describing the batch semantics and idempotency guarantee.
- Inline comment in `SyncOrchestrator.RunAsync` noting this replaces the INFRA-05 placeholder stub.
