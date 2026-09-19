---
type: pattern
module: Infrastructure
agent: antigravity
date: 2026-06-06
tags: [wpf, xaml, accessibility, wcag, mvvm, vb-net]
---

# WPF Accessibility / WCAG 2.2 AA Implementation Pattern

## Context

When developing views in the VISTA project, all interactive controls, grids, modal dialogs, and overlays must comply with WCAG 2.2 AA requirements. This is achieved by ensuring that screen readers (like Narrator) can announce control meanings and row content, and that focus is trapped inside modals/overlays.

## The Pattern

### 1. Reusable Focus Trapping via Attached Properties
Rather than manually trapping Tab and Shift+Tab key sequences, create and attach a reusable dependency property that sets `KeyboardNavigation` to **Cycle** mode on the modal container.

> **Do not set `FocusManager.IsFocusScope=True` for modal trapping.** A focus scope is designed for menus/toolbars; it diverges *logical* focus from *keyboard* focus and can interfere with `IsDefault`/RoutedCommand resolution and focus restoration on an overlay. `TabNavigation="Cycle"` (plus `ControlTabNavigation="Cycle"` for Ctrl+Tab) is what actually contains focus — that alone is the trap.

```vb
Public Class AccessibilityHelper
    Public Shared ReadOnly IsFocusTrapProperty As DependencyProperty =
        DependencyProperty.RegisterAttached(
            "IsFocusTrap",
            GetType(Boolean),
            GetType(AccessibilityHelper),
            New PropertyMetadata(False, AddressOf OnIsFocusTrapChanged))

    Public Shared Function GetIsFocusTrap(dp As DependencyObject) As Boolean
        Return CBool(dp.GetValue(IsFocusTrapProperty))
    End Function

    Public Shared Sub SetIsFocusTrap(dp As DependencyObject, value As Boolean)
        dp.SetValue(IsFocusTrapProperty, value)
    End Sub

    Private Shared Sub OnIsFocusTrapChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        Dim element = TryCast(d, UIElement)
        If element Is Nothing Then Return

        If CBool(e.NewValue) Then
            KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.Cycle)
            KeyboardNavigation.SetControlTabNavigation(element, KeyboardNavigationMode.Cycle)
        Else
            KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.Continue)
            KeyboardNavigation.SetControlTabNavigation(element, KeyboardNavigationMode.Continue)
        End If
    End Sub
End Class
```

Usage in XAML:
```xml
<Border helpers:AccessibilityHelper.IsFocusTrap="True" ...>
```

### 2. Screen Reader Naming
Bind row/control labels to dynamic data using `AutomationProperties.Name` and escape brackets properly using `StringFormat`.

```xml
<Setter Property="AutomationProperties.Name" Value="{Binding ProductName, StringFormat='Product: \{0\}'}"/>
```

## Why It Works

- **Focus containment:** `KeyboardNavigation.TabNavigation="Cycle"` natively keeps keyboard focus cycling within the container, ensuring keyboard-only users cannot tab into obscured windows/controls behind an active overlay — without the side effects of a `FocusManager` focus scope.
- **Token-driven contrast:** Every control binds colors via `DynamicResource`, so naming and focus work identically in both Light and Dark themes.

## Rules

- **Modals & Overlays:** Always apply `helpers:AccessibilityHelper.IsFocusTrap="True"` to the modal container.
- **Icon Buttons & Actions:** Always define an `AutomationProperties.Name` (and `HelpText` where needed) on icon-only buttons.
- **DataGrid Rows:** Define a `DataGrid.RowStyle` that targets `DataGridRow` to bind dynamic `AutomationProperties.Name` (e.g., using product name, vendor name, etc.) and `AutomationProperties.HelpText` (e.g., details about item counts or totals).

## Related

- Links: `[[wpf-vista-ui-settings-persistence]]`
