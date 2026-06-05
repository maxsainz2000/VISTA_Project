---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, mvvm, overlay, modal, keyboard-shortcut, accessibility]
---

# Pattern: Reusable Shell Overlay Recipe (WPF/VB.NET)

This pattern documents the structure, visual presentation, keyboard routing, focus safety, and text-input guards for full-screen modal overlays within a WPF application using MVVM.

## Context

When implementing modal overlays (such as search palettes, keyboard cheat sheets, or notifications) that sit over the main application shell:
1. **Consistent Visuals**: Overlays should reuse the standard backdrop dimming (`OverlayBrush`), centered card (`SurfaceBrush`), and shadow tokens (`CardShadow`) for a premium feel.
2. **Mutual Exclusion**: Only one overlay should be visible at any time to prevent layout conflicts or stacked lighting effects.
3. **Esc Key Dismissal**: A global Escape key handler must dismiss the top-most active overlay and mark the key event handled.
4. **Focus Restoration**: Focus must transition gracefully to the overlay on show, and restore to the previously focused control on hide.
5. **Keyboard Input Guard**: Bare keys (like `?` for a cheatsheet) must not trigger the overlay if the user is typing into text fields or password inputs.

## The Pattern

### 1. Visual Structure
The overlay sits at the root level of the layout Grid, spanning all columns and rows to dim the background. A centered Border acts as the card:

```xml
<Grid Background="{DynamicResource OverlayBrush}" MouseDown="Background_MouseDown">
    <Border Width="680" Height="Auto" VerticalAlignment="Center" HorizontalAlignment="Center"
            Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource SeparatorBrush}"
            BorderThickness="1"
            CornerRadius="{DynamicResource RadiusLarge}"
            Effect="{StaticResource CardShadow}"
            MouseDown="OverlayPanel_MouseDown">
        <!-- Content goes here -->
    </Border>
</Grid>
```

### 2. Code-Behind Mouse Handling
Suppress backdrop dismissal clicks by handling the card's `MouseDown` event:

```vb
Public Sub Background_MouseDown(sender As Object, e As MouseButtonEventArgs)
    ' Close the overlay
    Dim vm = TryCast(DataContext, OverlayViewModel)
    vm?.CloseCommand.Execute(Nothing)
End Sub

Public Sub OverlayPanel_MouseDown(sender As Object, e As MouseButtonEventArgs)
    ' Prevent click inside the card from closing the overlay
    e.Handled = True
End Sub
```

### 3. Keyboard Focus Safety
Stash the focused element when visible and restore it on close:

```vb
Private Sub Overlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
    If CType(e.NewValue, Boolean) Then
        _previouslyFocusedElement = Keyboard.FocusedElement
        Dispatcher.InvokeAsync(Sub()
                                   Me.Focus()
                                   Keyboard.Focus(Me)
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

### 4. MainWindow Preview Key Hook & Text Input Guard
Intercept the shortcut keys at the Window level. Prevent bare characters (like `?`) from opening the overlay if a text input control has focus:

```vb
Private Sub MainWindow_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
    If _viewModel IsNot Nothing Then
        ' 1. Escape key dismissal
        If _viewModel.IsShortcutsOverlayOpen AndAlso e.Key = Key.Escape Then
            _viewModel.ShortcutsOverlay.CloseCommand.Execute(Nothing)
            e.Handled = True
            Exit Sub
        End If

        ' 2. Focus-Guarded trigger
        If e.Key = Key.OemQuestion Then
            Dim hasControl = (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control
            Dim hasShift = (Keyboard.Modifiers And ModifierKeys.Shift) = ModifierKeys.Shift
            
            If hasShift AndAlso Not hasControl Then
                ' Check if focused element is a text input field
                Dim focused = Keyboard.FocusedElement
                Dim isTextInput = False
                If focused IsNot Nothing Then
                    If TypeOf focused Is System.Windows.Controls.Primitives.TextBoxBase OrElse
                       TypeOf focused Is System.Windows.Controls.PasswordBox Then
                        isTextInput = True
                    End If
                End If

                If Not isTextInput Then
                    _viewModel.ShortcutsOverlay.ToggleCommand.Execute(Nothing)
                    e.Handled = True
                    Exit Sub
                End If
            End If
        End If
    End If
End Sub
```

### 5. Mutual Exclusion Logic in Parent ViewModel
Coordinate visibility properties in the main ViewModel property handlers to ensure only one is active:

```vb
Private Sub OnShortcutsOverlayPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
    If e.PropertyName = NameOf(ShortcutsOverlayViewModel.IsOpen) Then
        OnPropertyChanged(NameOf(IsShortcutsOverlayOpen))
        If ShortcutsOverlay.IsOpen AndAlso CommandPalette.IsOpen Then
            CommandPalette.IsOpen = False
        End If
    End If
End Sub
```

## Why It Works

- **Visual Consistency**: By styling the shortcuts card using identical tokens as the command palette, the application builds a cohesive visual identity for modal dialogs and overlays.
- **Escape Path Isolation**: Window-level `PreviewKeyDown` ensures that `Escape` is captured before the active text fields or other controls swallow the input, assuring reliable modal close behavior.
- **No Input Hijacking**: Restricting bare keys based on focus types ensures editable text areas remain fully functional, while providing high keyboard discoverability across the rest of the application shell.
