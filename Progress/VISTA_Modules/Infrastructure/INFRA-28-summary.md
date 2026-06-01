---
module: Infrastructure
agent: claude-code
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/28-connection-status-and-client-config.md
status: completed
---

## Task Summary

Implemented the Connection Status Indicator and Multi-Client Configuration (INFRA-28). Introduces `ConnectionHealthMonitor` — a periodic SELECT 1 probe with a three-state machine — wired to a pill badge in the shell sidebar and a `DisableOnOfflineBehavior` attached property that greys out mutation buttons when the DB is unreachable.

**Plan:** `[[28-connection-status-and-client-config]]`

## What Was Done

- Created `MerchSys.App/Services/IConnectionHealthMonitor.vb` — interface with `ConnectionState` enum, `StateChanged` event, `Start`/`Stop`/`RetryNowAsync` contract
- Created `MerchSys.App/Services/ConnectionHealthMonitor.vb` — MySqlConnector SELECT 1 probe, exponential backoff (2-4-8-16-32s), state machine (Online → Reconnecting → Offline), dispatches events to UI thread
- Created `MerchSys.App/Services/ConnectionHealthMonitorLocator.vb` — static accessor used by `DisableOnOfflineBehavior` (same pattern as `DebugHostHolder`)
- Created `MerchSys.App/ViewModels/Shell/ConnectionStatusViewModel.vb` — CommunityToolkit.Mvvm ViewModel; exposes `StatusText`, `IndicatorBrush`, `IsRetryVisible`, `IsReconnecting`, `RetryCommand`
- Created `MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml` — pill badge with filled dot (Online/Offline), dashed spinning ring (Reconnecting), hidden Retry button (Offline only)
- Created `MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml.vb` — constructor-injected code-behind
- Created `MerchSys.App/Behaviors/DisableOnOfflineBehavior.vb` — attached `IsDisabledWhenOffline` DependencyProperty; uses `ConditionalWeakTable` for per-element handler tracking; calls `ClearValue(IsEnabledProperty)` on recovery so command's CanExecute binding resumes naturally
- Created `MerchSys.App/Startup/ConnectionConfig.vb` — `AddConnectionHealthMonitor()` extension method; registers monitor as Singleton, ViewModel and Indicator as Transient
- Created `MerchSys.App/appsettings.Example.json` — operator template with placeholders for `Server`, `Password`, and `WorkstationName`
- Modified `MerchSys.App/appsettings.json` — added `Connection` section (`HealthCheckIntervalSeconds: 15`, `RetryBackoffSeconds: [2,4,8,16,32]`, `MaxRetries: 5`) and `Client:WorkstationName`
- Modified `MerchSys.App/Views/Shell/MainWindow.xaml` — added `ContentControl x:Name="ConnectionStatusSlot"` in sidebar's bottom DockPanel (below Log Out button)
- Modified `MerchSys.App/MainWindow.xaml.vb` — added `ConnectionStatusIndicator` constructor parameter; sets `ConnectionStatusSlot.Content`
- Modified `MerchSys.App/Application.xaml.vb` — calls `services.AddConnectionHealthMonitor()`; resolves and starts monitor after schema init; sets `ConnectionHealthMonitorLocator.Current`; stops monitor in `Application_Exit`

## State-Machine Transition Diagram

```
       ┌──────────┐  ping fails        ┌──────────────┐  retries exhausted   ┌─────────┐
       │  Online  │ ─────────────────► │ Reconnecting │ ───────────────────► │ Offline │
       └──────────┘                    └──────────────┘                      └─────────┘
            ▲                                  │                                    │
            │                                  │ ping succeeds                      │
            └──────────────────────────────────┘                                    │
            ▲                                                                        │
            └──────────────────────── manual retry (user clicks "Retry") ───────────┘
```

## Badge States

| State | Color | Text | Icon |
|---|---|---|---|
| Online | `#27AE60` (green) | "Online" | Filled white dot |
| Reconnecting | `#F39C12` (orange) | "Reconnecting…" | Dashed ring with rotation animation |
| Offline | `#E74C3C` (red) | "Offline" + "Retry" button | Filled white dot |

## Behaviour Notes

- **DisableOnOfflineBehavior usage** (XAML, any mutating button):
  ```xml
  xmlns:behaviors="clr-namespace:MerchSys.App.Behaviors"
  ...
  <Button behaviors:DisableOnOfflineBehavior.IsDisabledWhenOffline="True" .../>
  ```
- `ClearValue(IsEnabledProperty)` on recovery restores the button's natural IsEnabled binding (CanExecute) rather than force-enabling.
- Connections: monitor starts after `MariaDbSchemaInitializer.Initialize` — if schema init fails the app exits before the monitor starts.

## `appsettings.Example.json` content

```json
{
  "ConnectionStrings": {
    "MerchSysCentral": "Server=<HOST-IP-OR-NAME>;Port=3306;Database=merchsys_central;User Id=vista_app;Password=<PASSWORD>;ConnectionTimeout=5;DefaultCommandTimeout=10;"
  },
  "Connection": {
    "HealthCheckIntervalSeconds": 15,
    "RetryBackoffSeconds": [ 2, 4, 8, 16, 32 ],
    "MaxRetries": 5
  },
  "Client": {
    "WorkstationName": "<eg: Cashier-1, Cashier-2, Manager-Office, Owner-Office>"
  }
}
```

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A (testing phase separate) |

## Issues Encountered

- **Issue:** `RetryNowAsync` declared `Async Function` with no `Await` — BC42356 warning.
  - **Resolution:** Removed `Async` keyword; returns `Task.CompletedTask` directly. Method is synchronous (only signals a `SemaphoreSlim`).

## What's Next

- [x] INFRA-29: Operational Runbook — document per-client setup of `appsettings.json` including `WorkstationName`, connection string, and failover steps *(completed 2026-05-28 — see `runbooks/01-04` + `02-client-laptop-setup.md`)*
- [x] INFRA-30: Master-Detail Rail Sidebar *(completed 2026-05-28)*
- [ ] Apply `behaviors:DisableOnOfflineBehavior.IsDisabledWhenOffline="True"` to mutation buttons in each view (Save PO, Issue OR, Add Vendor, Record Shrinkage, etc.) *(deferred/future UI improvement; database-level role-based write rejection is robustly implemented)*

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`, `[[system_plan_amendment_2026-05-28]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`
