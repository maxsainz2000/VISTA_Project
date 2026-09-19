---
module: Infrastructure
plan-id: INFRA-05
title: "Sync Worker & Dual-Condition Connectivity Probe"
depends-on: [INFRA-01, INFRA-02, INFRA-03]
estimated-files: 8
---

# Sync Worker & Dual-Condition Connectivity Probe

## Context

VISTA is offline-first: every workstation operates against a local SQLite file (`merchsys.db`) and synchronizes to a central MariaDB instance only when the network is healthy. The dual-condition rule (network adapter up **AND** server reachable on TCP probe) prevents partial-connectivity states that produce silent sync failures. This plan establishes the probe, the background worker that runs it, and the per-module sync abstraction. It does not implement actual data movement to MariaDB — that is INFRA-06.

## Prerequisites

- **INFRA-01** (Solution Scaffold) — `MerchSys.App` and `MerchSys.SharedKernel` exist; `Microsoft.Extensions.Hosting` package available
- **INFRA-02** (Shared Kernel) — `AuditableEntity`, `INotificationService` defined
- **INFRA-03** (Database Contexts) — module DbContexts exist; SQLite file location resolved

## Wiki References

- `concepts/offline-first-sync.md` — dual-condition synchronization rule
- `concepts/client-server-wpf.md` — WPF stack and sync behavior
- `Sources/system_plan.md` — central MariaDB topology

## Deliverables

```
MerchSys.SharedKernel/
├── Sync/
│   ├── ISyncProbe.vb
│   ├── DualConditionSyncProbe.vb
│   ├── SyncProbeResult.vb
│   ├── SyncStatus.vb
│   ├── ISyncableRepository.vb
│   ├── SyncJournal.vb
│   └── SyncJournalDbContext.vb

MerchSys.App/
└── Services/
    ├── SyncWorker.vb
    └── SyncOrchestrator.vb
```

## Specification

### SyncStatus (enum)
```
Offline      ' No network or no server
Probing      ' Probe in flight
Online       ' Both conditions met, idle
Syncing      ' Actively pushing/pulling
Error        ' Last sync attempt failed
```

### ISyncProbe / SyncProbeResult
```
Public Interface ISyncProbe
    Function ProbeAsync() As Task(Of SyncProbeResult)
End Interface

Public Class SyncProbeResult
    Public Property NetworkAvailable As Boolean
    Public Property ServerReachable As Boolean
    Public Property LatencyMs As Integer?
    Public Property ProbedAt As DateTime
    Public Property Error As String         ' Nullable

    Public ReadOnly Property IsHealthy As Boolean
        Get
            Return NetworkAvailable AndAlso ServerReachable
        End Get
    End Property
End Class
```

### DualConditionSyncProbe
- Condition A: `NetworkInterface.GetIsNetworkAvailable()`
- Condition B: TCP connect to `Sync:CentralServerHost:Sync:CentralServerPort` with `Sync:ProbeTimeoutMs` (default 2000ms)
- Both must be `True` for `IsHealthy`
- Logs failures (exception type only, not stack) via `ILogger(Of DualConditionSyncProbe)`

### ISyncableRepository
```
Public Interface ISyncableRepository
    ReadOnly Property ModuleName As String
    Function GetPendingChangesAsync() As Task(Of IReadOnlyList(Of SyncJournalEntry))
    Function MarkSyncedAsync(journalIds As IEnumerable(Of Long)) As Task
End Interface
```
Each module implements one in its `Data/` folder (in INFRA-06 plans for the modules).

### SyncJournal entity (`Sync_Journal` table)
```
Inherits AuditableEntity

Property TableName As String              ' e.g., "Pos_OfficialReceipts"
Property RowId As Long                    ' PK of source row
Property Operation As String              ' "INSERT" | "UPDATE" | "DELETE"
Property Payload As String                ' JSON snapshot
Property AttemptCount As Integer
Property LastError As String              ' Nullable
Property SyncedAt As DateTime?            ' Null = pending
Property ModuleName As String             ' "POS", "Inventory", etc.
```

Index on `(ModuleName, SyncedAt)` to make pending-changes queries cheap.

### SyncJournalDbContext
- Standalone context (separate file from any module DbContext) so any layer can append journal rows without taking a module-specific dependency.
- Uses the same SQLite file as other contexts (`merchsys.db`).

### SyncWorker (`BackgroundService`)
```
Public Class SyncWorker
    Inherits BackgroundService

    ' Dependencies via constructor:
    '   ISyncProbe, SyncOrchestrator, INotificationService,
    '   IOptionsMonitor(Of SyncSettings), ILogger(Of SyncWorker)
End Class
```

Loop:
1. `Await Task.Delay(SyncSettings.ProbeIntervalSeconds, stoppingToken)`
2. Update current `SyncStatus` to `Probing`; raise `SyncStatusChanged` notification
3. Call `ISyncProbe.ProbeAsync()`
4. On `IsHealthy = True` and previous status was `Offline`: call `SyncOrchestrator.RunAsync(stoppingToken)`
5. On exception: status = `Error`, log, continue loop

Default `ProbeIntervalSeconds = 30`. Configurable via `appsettings.json`.

### SyncOrchestrator
- Resolves all `ISyncableRepository` instances from DI
- Iterates in declared order: Purchasing → Inventory → POS → Accounting (matches event flow direction in `analysis/cross-module-data-flow.md`)
- Per-module: pulls pending journal entries, **delegates actual transmission to INFRA-06**, marks synced on success
- Stops on first module error and reports via `INotificationService` (subsequent modules retry next probe cycle)

### appsettings.json schema additions
```
"Sync": {
  "CentralServerHost": "192.168.1.10",
  "CentralServerPort": 3306,
  "ProbeIntervalSeconds": 30,
  "ProbeTimeoutMs": 2000
}
```

### DI Registration (in `MerchSys.App/Startup/`)
- Register `ISyncProbe` → `DualConditionSyncProbe` (singleton)
- Register `SyncOrchestrator` (scoped)
- Register `SyncWorker` as `IHostedService`
- Register `SyncJournalDbContext` with same SQLite connection string

## Implementation Notes

- **Probe must not block the UI thread** — all I/O is async
- **TCP probe uses `TcpClient.ConnectAsync` with a `CancellationTokenSource(timeout)`** — do not rely on `TcpClient.ReceiveTimeout` (it doesn't apply to connect)
- The orchestrator does not perform conflict resolution — that is in INFRA-06
- `SyncJournal` is append-only from the worker's perspective; only the worker sets `SyncedAt`
- Module repositories are responsible for **writing to** `Sync_Journal` when they perform local writes (will be added per-module in follow-up plans)

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. `DualConditionSyncProbe` returns `IsHealthy = False` within 2s when MariaDB host is unreachable
3. `SyncWorker` is registered as `IHostedService` in `MerchSys.App` startup
4. `SyncStatus` transitions surface via `INotificationService` (visible in shell status bar)
5. `Sync_Journal` table is created on first run via EF migration
6. No module project references `SyncWorker` directly — they only implement `ISyncableRepository`
7. Probe interval and timeout are read from `appsettings.json`, not hard-coded

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-05-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments on `ISyncProbe`, `ISyncableRepository`, and `SyncJournal` describing the offline-first contract
- Inline comment on `DualConditionSyncProbe.ProbeAsync` citing the rule from `concepts/offline-first-sync.md`
