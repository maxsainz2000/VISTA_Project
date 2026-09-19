---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, vb-net, mvvm, data-freshness, RelativeTimeConverter]
---

# Data Freshness & Manual Refresh Chip

## Context

When building modular monolith desktop applications where users view high-traffic dashboards and lists (e.g. stock, orders, financial KPIs), users need to know exactly how fresh the screen's data is. A manual refresh option is needed to fetch the latest state without refreshing the entire page, and a relative loading timestamp ("just now", "5m ago") should display the age of the loaded data.

To prevent memory leaks and unnecessary CPU overhead in ticking multiple individual timers, a unified ticking pattern is required.

## The Pattern

The pattern uses:
1. **`IFreshnessAware` Interface**: Enforces that target ViewModels contain a `LastLoadedAt As DateTime?` property.
2. **`FreshnessTimer` (Shared Clock)**: A single, low-frequency static `DispatcherTimer` ticking every 30 seconds that raises a static `Tick` event.
3. **`RelativeTimeConverter`**: Converts the timestamp into relative text ("just now", "1m ago", "2h ago").
4. **`FreshnessChip` UserControl**: A self-contained visual component containing:
   - A `TextBlock` displaying the relative time.
   - A `Button` containing a vector refresh path.
   - Subscriptions to the static `FreshnessTimer.Tick` event when loaded, and unsubscription when unloaded.
   - Text color and icon fill shifting to `WarningBrush` past a 5-minute staleness threshold.

### ViewModel Implementation
```vb
Public Class StockDashboardViewModel
    Inherits ViewModelBase
    Implements IFreshnessAware

    Private _lastLoadedAt As DateTime?
    Public Property LastLoadedAt As DateTime? Implements IFreshnessAware.LastLoadedAt
        Get
            Return _lastLoadedAt
        End Get
        Set(value As DateTime?)
            SetProperty(_lastLoadedAt, value)
        End Set
    End Property

    Public Async Function LoadDataAsync() As Task
        IsBusy = True
        Try
            ' ... load data from DB ...
            
            ' Stamp timestamp only on successful load
            LastLoadedAt = DateTime.Now
        Catch ex As Exception
            ' Keep old timestamp on failure
            ErrorMessage = ex.Message
        Finally
            IsBusy = False
        End Try
    End Function
End Class
```

### XAML View Integration
```xaml
<views:FreshnessChip LastLoadedAt="{Binding LastLoadedAt}"
                     RefreshCommand="{Binding RefreshCommand}"
                     VerticalAlignment="Center"
                     Margin="0,0,12,4"/>
```

## Why It Works

- **No Memory Leaks**: Subscribing to events of a static class (`FreshnessTimer`) creates a strong reference from the event publisher to the subscriber (the `UserControl`). If not unsubscribed, this prevents garbage collection of the view. Subscribing on `Loaded` and unsubscribing on `Unloaded` guarantees no memory leaks.
- **Low Performance Overhead**: Instead of each screen running its own timer thread, a single static clock updates all active chips. On each tick, `BindingOperations.GetBindingExpression(...).UpdateTarget()` is called to force re-evaluation of the relative time binding.
- **Failure Resilience**: The timestamp is only updated *after* successful data fetches. If a refresh fails, the previous loading time remains, keeping the user aware of how stale their local copy is.

## Rules

- Always implement `IFreshnessAware` on ViewModels that support data freshness.
- Only update `LastLoadedAt` at the very end of successful load/refresh paths inside viewmodels.
- Ensure the `FreshnessChip` is unsubscribed from `FreshnessTimer.Tick` in the `Unloaded` handler.

## Related

- Links to related entries: `[[wpf-vista-state-feedback]]`, `[[wpf-vista-formatting]]`
- Links to Domain Wiki pages: `[[modular-monolith]]`
