---
module: Infrastructure
agent: claude-code
date: 2026-05-12
plan-ref: Plans/VISTA_Modules/Infrastructure/10-sync-status-shell-indicator.md
status: completed
---

## Task Summary

Implemented the Sync Status Shell Indicator as specified in INFRA-10. A compact 24-pixel status bar was added to the bottom of `MainWindow.xaml`; it hosts a new `SyncStatusIndicator` UserControl wired to `SyncStatusIndicatorViewModel`, which subscribes to `INotificationService.SyncStatusChanged` and derives visual state from each transition.

**Plan:** `[[10-sync-status-shell-indicator]]`

## What Was Done

- Modified `MerchSys.SharedKernel/Interfaces/INotificationService.vb` — Added `Event SyncStatusChanged As EventHandler(Of SyncStatus)`, `ReadOnly Property CurrentSyncStatus As SyncStatus`, and `ReadOnly Property LastSuccessfulPushAt As Nullable(Of DateTimeOffset)` to the interface so consumers can subscribe and read state without depending on the concrete class.
- Modified `MerchSys.App/Services/DefaultNotificationService.vb` — Implemented the three new interface members. `LastSuccessfulPushAt` is set inside `NotifySyncStatusChanged` when the status transitions `Syncing → Online`, which is the only code path in `SyncWorker` where an actual push cycle has completed.
- Created `MerchSys.App/ViewModels/Shell/SyncStatusIndicatorViewModel.vb` — Contains `IndicatorSeverity` enum (`Healthy`, `Idle`, `Warning`, `Critical`) and the view-model. Subscribes to `INotificationService.SyncStatusChanged` on construction. Owns a 30-second `DispatcherTimer` that re-runs `Recompute` so `LastSyncDisplay` ticks forward between events. Implements `IDisposable`; `Dispose` stops the timer and removes both handlers.
- Created `MerchSys.App/Converters/SeverityToBrushConverter.vb` — New `IValueConverter` mapping `IndicatorSeverity` → `SolidColorBrush`. No equivalent converter existed in the project; a new one was introduced.
- Created `MerchSys.App/Views/Shell/SyncStatusIndicator.xaml` — Horizontal `StackPanel` UserControl with an `Ellipse` (fill bound via `SeverityToBrushConverter`), a `DisplayLabel` TextBlock, and a `LastSyncDisplay` TextBlock at 0.6 opacity. `ToolTip` bound to `TooltipText`.
- Created `MerchSys.App/Views/Shell/SyncStatusIndicator.xaml.vb` — Minimal code-behind, no logic.
- Modified `MerchSys.App/MainWindow.xaml` — Added `xmlns:shell` namespace reference, added `Grid.RowDefinitions` (`Height="*"` and `Height="24"`), set `Grid.Row="0"` on both the sidebar `Border` and the content `ContentControl`, and added a `Grid.Row="1"` `Border` (background `#1A252F`) spanning both columns hosting `<shell:SyncStatusIndicator DataContext="{Binding SyncStatusIndicator}"/>`. No pre-existing status-bar row was present, so a new `Grid.Row` was added per spec.
- Modified `MerchSys.App/ViewModels/MainWindowViewModel.vb` — Added `Public ReadOnly Property SyncStatusIndicator As SyncStatusIndicatorViewModel` and injected it via constructor.
- Modified `MerchSys.App/Application.xaml.vb` — Registered `SyncStatusIndicatorViewModel` as singleton and `Views.Shell.SyncStatusIndicator` as singleton before `MainWindowViewModel` and `MainWindow`.

## Indicator States (ASCII)

```
Healthy  ● Online              Last sync: 2 min ago
         (green ellipse)

Warning  ● Online (sync delayed)   Last sync: 15 min ago
         (amber ellipse)

Idle     ● Offline             Last sync: never
         (gray ellipse)

Idle     ● Syncing…            Last sync: 3 hr ago
         (gray ellipse)

Critical ● Sync error          Last sync: yesterday
         (red ellipse)
```

## Converter Note

No existing `SeverityToBrushConverter` or equivalent enum-to-brush converter was found in the project. A new one was created at `MerchSys.App/Converters/SeverityToBrushConverter.vb`. It is declared as a resource inside `SyncStatusIndicator.xaml` and is not registered globally.

## MainWindow.xaml Host Markup Added

```xml
<!-- Row definitions added to the main Grid -->
<Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <!-- INFRA-10: status bar row added; no status area existed prior to this plan -->
    <RowDefinition Height="24"/>
</Grid.RowDefinitions>

<!-- Status bar (Grid.Row="1") -->
<Border Grid.Row="1" Grid.ColumnSpan="2" Background="#1A252F">
    <shell:SyncStatusIndicator DataContext="{Binding SyncStatusIndicator}"/>
</Border>
```

Existing sidebar `Border` and content `ContentControl` had `Grid.Row="0"` added to them.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ Passed (Sync status indicator visible, no binding errors, disposes correctly) |

The 1 warning present in the build (`BC40000` in `MerchSys.POS/Data/Configurations/VatConfigurationMap.vb`) is pre-existing and unrelated to this plan.

## Issues Encountered

- **`INotificationService` lacked `SyncStatusChanged` event and status properties** — The plan referenced `IDefaultNotificationService` but the actual interface is `INotificationService`. The concrete `DefaultNotificationService` already exposed the event and `CurrentSyncStatus` but only as concrete members, not on the interface. `LastSuccessfulPushAt` did not exist at all. Resolution: extended the interface with all three members and updated `DefaultNotificationService` to implement them. The `LastSuccessfulPushAt` tracking logic was added to `NotifySyncStatusChanged` — it is set only on the `Syncing → Online` transition, which in `SyncWorker` corresponds exclusively to a completed push cycle.

## What's Next

- [x] Runtime smoke-test: launch the app and confirm the indicator renders in the status bar with no binding errors. *(completed/verified in Operator checklist)*
- [x] Confirm `Dispose` is invoked on shutdown (requires a test or debug trace on application exit). *(completed/verified in Operator checklist)*

## Cross-References

- Domain Wiki pages consulted: `concepts/client-server-wpf.md`, `concepts/modular-monolith.md`
- Predecessor plans: INFRA-05 (SyncWorker, DefaultNotificationService), INT-02 (MainWindow shell)

## Codebase Wiki Discrepancies

- `codebase_wiki/modules/shared-kernel/interfaces.md` lists `INotificationService` members as `NotifySyncStatusChanged(status)` and `CurrentStatus` — but the actual property is `CurrentSyncStatus`. The three new members (`SyncStatusChanged` event, `CurrentSyncStatus`, `LastSuccessfulPushAt`) added by this plan are not yet reflected in the codebase wiki.
- `codebase_wiki/modules/app/ui.md` does not yet include `Views/Shell/SyncStatusIndicator.xaml` or `ViewModels/Shell/SyncStatusIndicatorViewModel.vb`.
