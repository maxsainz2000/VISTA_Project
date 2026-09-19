---
type: pattern
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
tags: [wpf, mvvm, viewmodel, classlib, timer, threading, vb-net]
---

## Context

A ViewModel in a `net10.0` classlib (e.g. `MerchSys.Inventory`) needs to auto-refresh data on a fixed interval (e.g. every 60 seconds). `DispatcherTimer` (`System.Windows.Threading`) is **not available** in plain class libraries — it lives in `WindowsBase.dll` which is only referenced by WPF projects.

## The Pattern

1. Use `System.Timers.Timer` (available in all .NET projects).
2. Capture `SynchronizationContext.Current` in the ViewModel constructor — this is the WPF `DispatcherSynchronizationContext` when the ViewModel is resolved from DI on the UI thread.
3. In the timer `Elapsed` callback (which fires on a ThreadPool thread), use `_uiContext.Post(...)` to marshal the async refresh call back to the UI thread.

```vb
Imports System.Threading
Imports System.Timers

Private ReadOnly _refreshTimer As System.Timers.Timer
Private ReadOnly _uiContext As SynchronizationContext

Public Sub New(...)
    ' Must be called on the UI thread (i.e., via DI during WPF startup)
    _uiContext = SynchronizationContext.Current

    _refreshTimer = New System.Timers.Timer(60_000) With {.AutoReset = True}
    AddHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
    _refreshTimer.Start()
End Sub

Private Sub OnRefreshTick(sender As Object, e As ElapsedEventArgs)
    If _uiContext IsNot Nothing Then
        _uiContext.Post(
            Sub(o)
                Dim t = LoadDataAsync()
            End Sub, Nothing)
    End If
End Sub
```

## Why It Works

- `SynchronizationContext.Current` on the WPF UI thread returns `DispatcherSynchronizationContext`, which posts work items onto the dispatcher queue.
- `Post(callback)` runs the callback on the UI thread, so `ObservableCollection` mutations and property change notifications are safe.
- `LoadDataAsync()` uses `Await` internally; its continuations also run on the captured UI context.

## Rules

- Always import `System.Threading` (for `SynchronizationContext`) AND `System.Timers` separately. Do NOT rely on the bare name `Timer` — it's ambiguous with `System.Threading.Timer`; use the fully qualified `System.Timers.Timer`.
- Capture `SynchronizationContext.Current` in the constructor, not in the timer callback (which runs on a ThreadPool thread where `Current` is `Nothing`).
- Use `System.Timers.Timer` not `DispatcherTimer` in classlib projects.

## Related

- `[[vbnet-list-count-property-shadows-linq-extension]]` — another VB.NET classlib build pitfall
