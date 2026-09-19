---
module: Infrastructure
plan-id: INFRA-06
title: "MariaDB Central Schema & Reconciliation"
depends-on: [INFRA-05]
estimated-files: 6
---

# MariaDB Central Schema & Reconciliation

## Context

INFRA-05 established the probe and worker but stopped short of moving data. This plan defines the central MariaDB 11.4.x schema (mirrored from the local SQLite tables), the per-module sync mappers that translate journal entries to remote rows, and the conflict-resolution policy. Financial tables (POS receipts, credit payments, accounting journals) are **append-only** at the sync layer regardless of what the local row says — this is the BIR-driven half of why POS-13 enforces immutability locally as well.

## Prerequisites

- **INFRA-05** (Sync Worker) — `SyncJournal`, `ISyncableRepository`, `SyncOrchestrator` exist
- XAMPP MariaDB 11.4.x reachable on the LAN; admin credentials available for initial schema load

## Wiki References

- `concepts/offline-first-sync.md` — append-only rule for financial tables
- `concepts/client-server-wpf.md` — central server topology
- `analysis/cross-module-data-flow.md` — sync ordering rationale

## Deliverables

```
MerchSys.SharedKernel/Sync/
├── MariaDbSyncContext.vb
├── ConflictResolver.vb
├── ConflictResolution.vb            ' Enum: LastWriteWins, AppendOnly, Reject
└── SyncMaps/
    ├── PurchasingSyncMap.vb
    ├── InventorySyncMap.vb
    ├── PosSyncMap.vb
    └── AccountingSyncMap.vb

Plans/VISTA_Modules/Infrastructure/sql/
└── mariadb-init.sql
```

(Total file count covers `MariaDbSyncContext`, `ConflictResolver`, four `*SyncMap` files, and the SQL DDL script.)

## Specification

### MariaDbSyncContext
- Targets MariaDB via `Pomelo.EntityFrameworkCore.MySql`
- DbSet for every synced entity, keyed by table name with the same `Pur_/Inv_/Pos_/Acc_` prefixes
- Connection string source: `Sync:MariaDbConnection` from `appsettings.json`
- `ChangeTracker.QueryTrackingBehavior = NoTracking` — sync writes are scripted via `ExecuteSqlRaw`/`Add` for performance, not change-tracked

### ConflictResolution policy
```
Public Enum ConflictResolution
    LastWriteWins   ' Compare ModifiedAt; remote wins if newer
    AppendOnly      ' INSERT only; UPDATE/DELETE rejected
    Reject          ' Push fails on any existing remote row
End Enum
```

| Table prefix | Policy        | Rationale |
|--------------|---------------|-----------|
| `Pur_*`      | LastWriteWins | PO edits acceptable until received |
| `Inv_*`      | LastWriteWins | Stock counts and adjustments       |
| `Pos_OfficialReceipts`, `Pos_ReceiptIntegrity`, `Pos_CreditPayments` | AppendOnly | BIR/financial — see POS-13 |
| `Pos_*` (other) | LastWriteWins | Cart edits, transaction header before finalization |
| `Acc_*`      | AppendOnly    | Ledger rows are immutable once posted |

### ConflictResolver
```
Public Interface IConflictResolver
    Function ResolveAsync(entry As SyncJournalEntry, remote As Object) As Task(Of ResolutionDecision)
End Interface

Public Class ResolutionDecision
    Public Property Action As SyncAction        ' Push, Skip, Reject
    Public Property Reason As String
End Class
```

The default `ConflictResolver` implementation reads the policy table above (loaded from a static dictionary keyed by table prefix) and applies it. `Reject` decisions write the failure back to `SyncJournal.LastError` and increment `AttemptCount`; the worker stops retrying after `Sync:MaxAttempts` (default 5).

### Per-module SyncMap
Each `*SyncMap` is a static class with:
```
Public Shared Function ToRemote(entry As SyncJournalEntry) As Object
Public Shared Function GetRemoteKey(entry As SyncJournalEntry) As Object
Public Shared ReadOnly Property Tables As IReadOnlyList(Of String)
```

`ToRemote` deserializes `SyncJournalEntry.Payload` (JSON) into the matching MariaDB-mapped entity. The local-vs-remote class boundary is intentional: local entities can carry SQLite-specific shadow columns (e.g., `IsDeleted`); remote entities are the canonical contract.

### `mariadb-init.sql`
- `CREATE DATABASE merchsys_central CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`
- DDL for every synced table, mirroring local 3NF structure
- Audit columns (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`) on every table
- `Pos_OfficialReceipts`, `Pos_ReceiptIntegrity`, `Pos_CreditPayments`, all `Acc_*`: enforce immutability via `BEFORE UPDATE` and `BEFORE DELETE` triggers that `SIGNAL SQLSTATE '45000'`
- Indexes on every column used in reconciliation joins (PK + `ModifiedAt`)
- `merchsys_sync` role with `SELECT, INSERT, UPDATE` (no `DELETE`) granted to the sync user

### SyncOrchestrator integration
INFRA-05 defined the iteration shape; this plan supplies the body of `RunForModuleAsync(repository As ISyncableRepository)`:
1. `entries = Await repository.GetPendingChangesAsync()`
2. For each entry: resolve conflict, push (or skip/reject), accumulate `syncedIds`
3. `Await repository.MarkSyncedAsync(syncedIds)`
4. On exception: increment journal `AttemptCount`, set `LastError`, propagate to orchestrator

### appsettings.json additions
```
"Sync": {
  "MariaDbConnection": "Server=192.168.1.10;Port=3306;Database=merchsys_central;Uid=merchsys_sync;Pwd=...",
  "MaxAttempts": 5,
  "BatchSize": 100
}
```

## Implementation Notes

- **Reconciliation order** (Purchasing → Inventory → POS → Accounting) matches the event flow direction in `analysis/cross-module-data-flow.md`. Out-of-order push can cause Accounting to reference a Purchasing row that hasn't propagated yet.
- **Append-only enforcement is layered**: client-side (`ConflictResolver`), server-side (MariaDB triggers), and locally (POS-13 interceptor). Defense in depth.
- **Credentials** must not be checked in. `mariadb-init.sql` uses placeholder; real password lives in user-level `appsettings.Production.json` outside the repo.
- This plan does **not** implement an inbound (server → client) pull. VISTA is single-master per workstation in this phase.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. `mariadb-init.sql` runs cleanly against a fresh MariaDB 11.4.x instance
3. UPDATE/DELETE against `Pos_OfficialReceipts` on the central server returns SQLSTATE 45000
4. `ConflictResolver` applies `AppendOnly` to all four BIR/ledger tables
5. Per-module sync map round-trip (serialize→push→read back) preserves all audit columns
6. Sync orchestrator iterates modules in the documented order
7. `Sync:MaxAttempts` exhaustion stops retry and surfaces a notification

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-06-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments on `IConflictResolver` and each `*SyncMap` describing ownership and policy
- Header comment in `mariadb-init.sql` listing schema version and intended MariaDB version (11.4.x)

---

## Superseded Items

> **2026-05-XX (INFRA-17):** The `Pomelo.EntityFrameworkCore.MySql` dependency specified in this plan
> has been replaced with raw `MySqlConnector` (ADO.NET). `MariaDbSyncContext` no longer inherits
> `DbContext`; it is a lightweight connection wrapper. See INFRA-17 for details.
