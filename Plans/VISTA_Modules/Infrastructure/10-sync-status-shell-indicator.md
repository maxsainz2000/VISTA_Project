---
module: MerchSys.Infrastructure
plan-id: INFRA-10
title: "Sync Status Shell Indicator"
depends-on: [INFRA-05, INT-02]
estimated-files: 3
---

# Sync Status Shell Indicator

## Context

INFRA-05 publishes `DefaultNotificationService.SyncStatusChanged` events whenever the dual-condition connectivity probe transitions between Online, Offline, Probing, or Error states. The 2026-05-11 Infrastructure audit notes that the `MainWindowViewModel` shell does not yet subscribe to this event, so the user has no visual indication of whether the application is currently pushing data to MariaDB or running in pure offline mode. Both INFRA-05 and INFRA-06 progress summaries list this as a "What's Next" item; it has never been actioned.

This plan adds a small, well-bounded status-bar indicator at the bottom of `MainWindow.xaml`, wired to the existing event. No new service abstraction; this is purely UI presentation of state INFRA-05 already publishes.

## Prerequisites

- **INFRA-05** (Sync Worker) — `DefaultNotificationService.SyncStatusChanged`, `SyncStatus` enum, `LastSuccessfulPushAt` property
- **INT-02** (Shell Navigation) — `MainWindow.xaml`, `MainWindowViewModel`, the existing status-bar `Grid.Row` or equivalent

## Wiki References

- `concepts/client-server-wpf.md` — Offline-first behaviour; sync is opportunistic, not blocking
- `concepts/modular-monolith.md` — UI presentation belongs in `MerchSys.App`, not in any module library

## Deliverables

```
MerchSys.App/ViewModels/Shell/
└── SyncStatusIndicatorViewModel.vb                     ' New

MerchSys.App/Views/Shell/
└── SyncStatusIndicator.xaml                            ' New UserControl

MerchSys.App/Views/Shell/MainWindow.xaml                ' Modified — host the indicator
```

## Specification

### SyncStatusIndicatorViewModel

```
Public Class SyncStatusIndicatorViewModel
    Inherits ObservableObject

    Public Sub New(notifications As IDefaultNotificationService)
        ' Subscribe to SyncStatusChanged on construction
        ' Set initial state via notifications.CurrentStatus
    End Sub

    <ObservableProperty>
    Private _displayLabel As String                     ' "Online", "Offline", "Syncing…", "Sync error"

    <ObservableProperty>
    Private _severity As IndicatorSeverity              ' Healthy | Idle | Warning | Critical

    <ObservableProperty>
    Private _lastSyncDisplay As String                  ' "Last sync: 2 min ago" — derived

    <ObservableProperty>
    Private _tooltipText As String                      ' Multi-line detail

End Class

Public Enum IndicatorSeverity
    Healthy = 0          ' Online + recent successful push
    Idle = 1             ' Offline by design (no network)
    Warning = 2          ' Online but last successful push > 10 minutes ago
    Critical = 3         ' Sync error / probe failure
End Enum
```

State derivation rules (recompute on every `SyncStatusChanged`):

- `SyncStatus.Online + LastSuccessfulPushAt within 10 min` ⇒ `Healthy`, "Online"
- `SyncStatus.Online + LastSuccessfulPushAt older than 10 min` ⇒ `Warning`, "Online (sync delayed)"
- `SyncStatus.Probing` ⇒ `Idle`, "Syncing…"
- `SyncStatus.Offline` ⇒ `Idle`, "Offline"
- `SyncStatus.Error` ⇒ `Critical`, "Sync error"

`LastSyncDisplay` uses relative formatting: `"just now"`, `"<n> min ago"`, `"<n> hr ago"`, `"yesterday"`, `"never"`.

A 30-second `DispatcherTimer` re-runs the derivation so the relative timestamp ticks forward even when no event fires. The timer is owned by the view-model, started in the constructor, stopped on `Dispose`.

### SyncStatusIndicator.xaml

A horizontal `StackPanel` UserControl:

```xml
<StackPanel Orientation="Horizontal" VerticalAlignment="Center"
            ToolTip="{Binding TooltipText}">
    <Ellipse Width="10" Height="10" Margin="0,0,6,0"
             Fill="{Binding Severity, Converter={StaticResource SeverityToBrushConverter}}" />
    <TextBlock Text="{Binding DisplayLabel}" Margin="0,0,8,0" />
    <TextBlock Text="{Binding LastSyncDisplay}" Opacity="0.6" />
</StackPanel>
```

The `SeverityToBrushConverter` maps the four `IndicatorSeverity` values to brushes. If a similar converter already exists in the app for other status indicators, reuse it; do not introduce a parallel converter for the same enum-to-brush mapping.

### MainWindow.xaml host

Add the indicator to the existing status-bar area at the bottom of the shell. If `MainWindow.xaml` already has a `StatusBar` or `Border` row at the bottom, host inside that — do not introduce a new `Grid.Row`. If no status area exists yet, add one as `Grid.RowDefinition Height="24"` at the bottom of the main grid; document the change in the implementation summary.

The host binding:

```xml
<shell:SyncStatusIndicator DataContext="{Binding SyncStatusIndicator}" />
```

`MainWindowViewModel` exposes `Public ReadOnly Property SyncStatusIndicator As SyncStatusIndicatorViewModel` populated from DI.

### DI registration

```
services.AddSingleton(Of SyncStatusIndicatorViewModel)
```

Singleton lifetime: there is exactly one shell, exactly one indicator. Scoped would cause the timer and event subscription to dispose on every scope.

## Implementation Notes

- The audit lists this as a follow-up to INFRA-05 and INFRA-06, but it is purely a UI concern and does not move any infrastructure code. Keep all changes in `MerchSys.App`.
- The 10-minute "sync delayed" threshold is a starting heuristic. Do not promote it to a configurable setting in this plan; if it needs tuning, a future plan can move it to `appsettings.json`.
- The `DispatcherTimer` must be marshalled to the UI thread (it is by default in WPF); do not introduce a `System.Threading.Timer` — that path requires manual `Dispatcher.Invoke` for property updates.
- Memory leak guard: unsubscribe from `SyncStatusChanged` and stop the timer in `Dispose`. The view-model implements `IDisposable`; the shell's lifetime owns it.
- Per the feedback memory: no `Await` in `Catch`/`Finally`. The subscription handler is synchronous; this is mostly a non-issue here but applies if the handler grows.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `SyncStatusIndicatorViewModel` subscribes to `IDefaultNotificationService.SyncStatusChanged` on construction.
3. The view-model recomputes `DisplayLabel`, `Severity`, and `LastSyncDisplay` synchronously when the event fires.
4. A 30-second timer drives `LastSyncDisplay` updates between events.
5. `SyncStatusIndicator.xaml` renders without runtime binding errors at app startup with a populated `SyncStatusIndicatorViewModel`.
6. The indicator is hosted in `MainWindow.xaml`'s status-bar area; no new top-level grid row is introduced if a status row already existed.
7. The `Ellipse` colour visibly changes when `SyncStatus` transitions Online → Offline → Error.
8. `LastSyncDisplay` reads `"never"` on a fresh install with no successful push recorded.
9. The view-model is registered as a singleton; resolving it twice returns the same instance.
10. On application shutdown, the view-model's `Dispose` is called and the timer stops (no background thread leak).

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-10-summary.md` using `Progress/_template.md`. Include:

- A small screenshot or ASCII representation of the indicator in each of the four severity states.
- Confirmation that an existing `SeverityToBrushConverter` was reused (or a note explaining why a new one was needed).
- The exact `MainWindow.xaml` host markup that was added.

### Documentation
- XML doc on `SyncStatusIndicatorViewModel` describing the 10-minute "sync delayed" heuristic and citing its origin (this plan).
- Inline comment in the timer setup explaining why 30 seconds is the chosen cadence.
