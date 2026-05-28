---
module: MerchSys.Infrastructure
plan-id: INFRA-28
title: "Connection Status Indicator + Multi-Client Configuration"
depends-on: [INFRA-25]
estimated-files: 8
priority: high
amendment-ref: AMD-2026-05-28-01
---

# INFRA-28: Connection Status Indicator + Multi-Client Configuration

## Context

With pure client-server, every client must:
1. Be configured with the host laptop's IP/hostname for the MariaDB instance.
2. Show its connection state to the operator at all times.
3. Disable mutation commands when the DB is unreachable.
4. Recover automatically when connectivity returns.

The previous `SyncStatusIndicator` (Online / Offline / Error sync-state tri-state) is gone (INFRA-27). This plan introduces `ConnectionStatusIndicator` (Online / Reconnecting / Offline), wired to a connection-health probe.

## Prerequisites

- **INFRA-25** — DbContexts on MariaDB.

(Can run in parallel with INFRA-26 and INFRA-27; sequence in any order after 25.)

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` §2.4 (Connectivity Requirement)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (Connection Model, Connectivity Requirement)
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md`

## Deliverables

```
MerchSys.App/Services/ConnectionHealthMonitor.vb              ' NEW — periodic ping + state machine
MerchSys.App/Services/IConnectionHealthMonitor.vb             ' NEW — interface (Online/Reconnecting/Offline + event)

MerchSys.App/ViewModels/Shell/ConnectionStatusViewModel.vb    ' NEW — binds monitor state to UI
MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml       ' NEW — small badge in shell header
MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml.vb    ' NEW — code-behind

MerchSys.App/Startup/ConnectionConfig.vb                      ' NEW — DI registration

MerchSys.App/appsettings.json                                 ' MOD — add Connection:HealthCheckIntervalSeconds, RetryBackoffSeconds, MaxRetries
MerchSys.App/appsettings.Example.json                         ' NEW — template for each client laptop (with placeholder host)

MerchSys.App/Views/Shell/MainWindow.xaml                      ' MOD — slot ConnectionStatusIndicator into header
MerchSys.App/Behaviors/DisableOnOfflineBehavior.vb            ' NEW — attached behavior for mutation buttons
```

## Specification

### Connection state machine

```
       ┌──────────┐  ping fails        ┌──────────────┐  ping fails (N retries)   ┌─────────┐
       │  Online  │ ─────────────────► │ Reconnecting │ ───────────────────────► │ Offline │
       └──────────┘                    └──────────────┘                          └─────────┘
            ▲                                  │                                      │
            │                                  │ ping succeeds                        │
            │                                  ▼                                      │
            └──────────────────────────────────────────────────────────────────────────┘
                                          manual retry (user clicks "Retry")
```

States:
- **Online** — last health-check succeeded within `HealthCheckIntervalSeconds`. Mutation commands enabled.
- **Reconnecting** — at least one health-check failed; retrying with exponential backoff (2s, 4s, 8s, 16s, 32s) up to `MaxRetries` (5). Mutation commands disabled.
- **Offline** — retries exhausted. Mutation commands disabled. "Retry now" button appears in the indicator.

### Health check

```vb
Private Async Function PingAsync() As Task(Of Boolean)
    Try
        Using conn As New MySqlConnection(_connStr)
            Await conn.OpenAsync()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT 1"
                cmd.CommandTimeout = 3
                Await cmd.ExecuteScalarAsync()
            End Using
            Return True
        End Using
    Catch
        Return False
    End Try
End Function
```

Runs on `HealthCheckIntervalSeconds` cadence (default 15s). On failure, transition to Reconnecting and start backoff loop.

### `appsettings.Example.json`

Template each client laptop copies and customizes:

```json
{
  "ConnectionStrings": {
    "MerchSysCentral": "Server=<HOST-IP-OR-NAME>;Port=3306;Database=merchsys_central;User Id=vista_app;Password=<PASSWORD>;ConnectionTimeout=5;DefaultCommandTimeout=10;"
  },
  "Connection": {
    "HealthCheckIntervalSeconds": 15,
    "RetryBackoffSeconds": [2, 4, 8, 16, 32],
    "MaxRetries": 5
  },
  "Client": {
    "WorkstationName": "<eg: Cashier-1, Cashier-2, Manager-Office, Owner-Office>"
  }
}
```

Document in INFRA-29's runbook how operators populate this per laptop.

### UI badge

A small pill in the shell header:

| State | Color | Text | Icon |
|---|---|---|---|
| Online | Green `#27AE60` | "Online" | filled dot |
| Reconnecting | Orange `#F39C12` | "Reconnecting…" | spinner |
| Offline | Red `#E74C3C` | "Offline — Retry" | outlined dot + button |

Clicking the badge in Offline state forces an immediate `PingAsync` (resets the backoff schedule on success).

### `DisableOnOfflineBehavior`

Attached behavior bindable to any `Button.Command` route. Subscribes to `IConnectionHealthMonitor.StateChanged`. When state ≠ Online, sets `IsEnabled = False`. When Online, restores.

Apply to all mutating buttons: "Save PO", "Issue OR", "Add Vendor", "Record Shrinkage", etc. Read-only views (dashboards, reports) remain functional in Reconnecting/Offline using the most recent data EF has cached — though queries hitting the network will fail; that's surfaced via the existing exception handlers.

### Logging

Connection state transitions log at `Information` level. Health check failures log at `Warning`. Offline transition logs at `Error`. Do not spam logs during steady-state Online (one log line per successful ping = noise).

## Acceptance Criteria

1. Fresh app launch against reachable MariaDB shows "Online" badge within 5 seconds.
2. Stopping the MariaDB service mid-session: indicator transitions to "Reconnecting…" within `HealthCheckIntervalSeconds`, then to "Offline" after retries exhausted. Mutating commands grey out.
3. Restarting the MariaDB service: clicking "Retry" or waiting for next manual probe brings indicator to "Online", commands re-enable.
4. `appsettings.Example.json` exists and is documented in INFRA-29's runbook.
5. Connection string lives only in `appsettings.json` (or `appsettings.Production.json` per client); no hardcoded host IP in source.
6. Build: 0 errors / 0 warnings.

## Out of Scope (Defer)

- Auto-failover to a backup MariaDB host. Operationally handled via INFRA-29's failover runbook (manual `appsettings.json` edit + restart).
- Encrypted password storage in `appsettings.json` (currently plain text). Future security plan should address — see OWASP DA3.
- Per-client telemetry collection of connection stability. Out of scope until operationally needed.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-28-summary.md` per `Progress/_template.md`. Include:

- State-machine transition diagram (or reference to this plan's diagram).
- Screenshot or text capture of the badge in each state.
- A demonstration of the disable-on-offline behavior (button greyed during simulated outage).
- `appsettings.Example.json` content.

### Documentation

- XML doc on `IConnectionHealthMonitor` describing the state machine and event contract.
- Header comment in `appsettings.Example.json` explaining what each value does and how to populate `WorkstationName`.
- `CLAUDE.md` section "MariaDB CLI" updated to reference `WorkstationName` for log triage.
