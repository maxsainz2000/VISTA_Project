---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-03
tags: [wpf, xaml, mvvm, command-palette, navigation, search, debouncing]
---

# Pattern: Spotlight-Style Global Command Palette (WPF/VB.NET)

This pattern documents the design, keyboard hooking, focus restoration, and navigation routing for the global spotlight-style command palette overlay.

## Context

When implementing a global spotlight-style search overlay (Command Palette) in a modular WPF/VB.NET application:
1. **Single Source of Truth**: The shell's active role-visible screens must be aggregated in a single place (`AllNavigableItems`) rather than relying on legacy flat groups or per-module collections scattered across dashboard files.
2. **Activity Rail & Panel Sync**: When navigating cross-module from the palette, simply calling the navigation command leaves the Activity Rail and Module Detail Panel out of sync. You must pre-set `ActiveModule` before invoking navigation.
3. **Keyboard Hook & Bubbling Interception**: Control keys like `Esc`, `↑`, `↓`, and `Enter` must be captured locally by the palette control to prevent them from triggering window-level input bindings or bubbling.
4. **Debounced Async Entity Search**: To prevent typing lag on database reads, search queries (e.g. products) must be debounced using task cancellation tokens and executed off the UI thread.
5. **Focus Safety**: When opening the palette, focus must transition to the search box, and it must return to the previously focused UI element on close.

## The Pattern

### 1. Single Nav Aggregator
Maintain a single tuple-aggregated collection in the `MainWindowViewModel`:

```vb
Public ReadOnly Property AllNavigableItems As IReadOnlyList(Of (Module As AppModule, Item As NavigationItem))
```

This collection is rebuilt during `RebuildModuleCollections()` (which gets called on startup and login/logout refreshes).

### 2. Set-Module-Then-Navigate Fix
To prevent rail desyncs, resolve the target's module and set `ActiveModule` before navigating:

```vb
Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = selected.TargetItem.ViewType)
If pair.Item IsNot Nothing Then
    mainVm.ActiveModule = pair.Module
    If mainVm.NavigateCommand.CanExecute(pair.Item) Then
        mainVm.NavigateCommand.Execute(pair.Item)
    End If
End If
```

### 3. Key Interception and Bubbling
Focus anomalies or timing bugs (e.g. logging in via the `Enter` key) can prevent local key handling inside a UserControl from firing. Therefore, intercept control keys (`Esc`, `↑`, `↓`, `Enter`) at the **Window level** (e.g. in `MainWindow_PreviewKeyDown`) when the palette is open, and delegate UI scrolling to the control:

```vb
Private Sub MainWindow_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
    If _viewModel IsNot Nothing AndAlso _viewModel.IsCommandPaletteOpen Then
        Select Case e.Key
            Case Key.Escape
                _viewModel.CommandPalette.CloseCommand.Execute(Nothing)
                e.Handled = True
            Case Key.Up
                _viewModel.CommandPalette.MoveSelectionUp()
                If CommandPaletteControl IsNot Nothing Then
                    CommandPaletteControl.ScrollSelectedIndexIntoView()
                End If
                e.Handled = True
            Case Key.Down
                _viewModel.CommandPalette.MoveSelectionDown()
                If CommandPaletteControl IsNot Nothing Then
                    CommandPaletteControl.ScrollSelectedIndexIntoView()
                End If
                e.Handled = True
            Case Key.Enter
                _viewModel.CommandPalette.ExecuteSelectedCommand.Execute(Nothing)
                e.Handled = True
        End Select
    End If
End Sub
```

Additionally, set `Focusable="False"` on visual search affordance buttons to prevent them from acquiring default focus on window startup (which would cause key up / Enter events from login to immediately trigger the palette).

### 4. Debounced Async Lookup (CancellationTokenSource)
Debounce query updates by canceling previous tasks:

```vb
Private Async Sub OnQueryChanged(newQuery As String)
    If _searchCts IsNot Nothing Then
        _searchCts.Cancel()
        _searchCts.Dispose()
    End If

    _searchCts = New CancellationTokenSource()
    Dim token = _searchCts.Token

    Try
        Await Task.Delay(250, token)
        Await RunSearchAsync(newQuery, token)
    Catch ex As TaskCanceledException
        ' Ignored
    End Try
End Sub
```

### 5. Focus Stash and Restore
Save focus on `IsVisibleChanged = True` and restore it when `IsVisibleChanged = False` using `Dispatcher.InvokeAsync`:

```vb
Private Sub CommandPalette_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
    If CType(e.NewValue, Boolean) Then
        _previouslyFocusedElement = Keyboard.FocusedElement
        Dispatcher.InvokeAsync(Sub()
                                   SearchTextBox.Focus()
                                   Keyboard.Focus(SearchTextBox)
                               End Sub, DispatcherPriority.Input)
    Else
        If _previouslyFocusedElement IsNot Nothing Then
            Dispatcher.InvokeAsync(Sub()
                                       _previouslyFocusedElement.Focus()
                                       Keyboard.Focus(_previouslyFocusedElement)
                                   End Sub, DispatcherPriority.Input)
        End If
    End If
End Sub
```

## Why It Works

- **Unified Navigation Model**: Rebuilding `AllNavigableItems` in `RefreshNavigation()` guarantees that the command palette matches the exact role-gates established by the login session.
- **Syncing UI States**: Syncing `ActiveModule` before `NavigateCommand` forces the layout's active panel and rail buttons to update, matching visual state with content.
- **WPF Focus Scope Integrity**: Keeping the focus stashed in a private code-behind field prevents the app from losing focus when closing the command palette lightbox.

## Related

- Domain Wiki: `[[wpf-vista-state-feedback]]`, `[[wpf-mainwindow-not-shell-window]]`
- Agent Wiki antipatterns: `[[wpf-setter-targets-clr-property-not-dependencyproperty]]`
