---
type: error-fix
module: MerchSys.App
agent: claude-code
date: 2026-06-05
tags: [wpf, xaml, vb-net, visual-state-manager, storyboard, listbox, datagrid, command-palette, runtime-error]
error-code: InvalidOperationException
severity: runtime-error
---

## Problem

Pressing `Ctrl+K` to open the command palette (UX-27 exposed this; the mutual-exclusion
swap that closes the shortcuts overlay and opens the palette auto-selects the first
result) crashed with:

```
System.InvalidOperationException
  HResult=0x80131509
  Message=Cannot resolve all property references in the property path 'Foreground'.
          Verify that applicable objects support the properties.
```

Source: `Themes/Controls.xaml`, the implicit `ListBoxItem` `Style` → `Template` →
`VisualStateManager` `SelectionStates` group:

```xml
<VisualState x:Name="Selected">
    <Storyboard>
        <DoubleAnimation Storyboard.TargetName="SelectionOverlay" Storyboard.TargetProperty="Opacity" To="1" Duration="0"/>
        <ObjectAnimationUsingKeyFrames Storyboard.TargetProperty="Foreground">     <!-- BROKEN -->
            <DiscreteObjectKeyFrame KeyTime="0" Value="{DynamicResource SelectionForegroundBrush}"/>
        </ObjectAnimationUsingKeyFrames>
    </Storyboard>
</VisualState>
```

**The build compiled clean (0 errors / 0 warnings).** Like all `Style`/`ControlTemplate`
trigger and VSM content, this is parsed and applied at runtime — it only throws when the
`Selected` visual state is actually entered (i.e. when a list item becomes selected).

The same broken pattern was also present (latent/dead) in the `DataGridRow` template in
`Themes/Controls.DataGrid.xaml`.

## Root Cause

A VSM `Storyboard` animation with **no `Storyboard.TargetName`** targets the **root
element of the `ControlTemplate`'s visual tree**, not the templated control itself.

- The `ListBoxItem` template root is `<Grid Margin="2,1">`. A `Grid` has no `Foreground`
  property.
- The `DataGridRow` template root is `<Border Name="DGR_Border">`. A `Border` has no
  `Foreground` property.

So `Storyboard.TargetProperty="Foreground"` cannot resolve against the target element →
`InvalidOperationException: Cannot resolve all property references in the property path
'Foreground'`.

`Foreground` is defined on `Control` (and `TextElement`), not on `Grid`/`Border`/`Panel`.
The intent was to recolor the *item's own* `Foreground` (which the command palette's
`{Binding Foreground, RelativeSource={RelativeSource AncestorType=ListBoxItem}}` reads to
recolor its text and icon on selection). A no-`TargetName` VSM storyboard cannot reach the
templated parent's `Foreground` — there is no way to name the templated parent for a VSM
target.

Why it surfaced only now: `ListBoxItem`'s built-in VSM drives real states named
`Selected` / `SelectedUnfocused` / `Unselected`, so the broken storyboard fires the moment
any `ListBox` item is selected. The command palette is the first styled `ListBox` whose
selection is reliably driven (auto-select first result). The `DataGridRow` copy never fired
because `DataGridRow`'s built-in VSM does not drive a `SelectionStates/Selected` state by
that name — it sat dead, but was the same time-bomb.

## Fix

Stop animating `Foreground` via VSM. Set the item's own `Foreground` with a plain
**`Style.Triggers` `IsSelected` property trigger** instead — it targets the templated
control directly, needs no animation, and the command palette's `RelativeSource`-to-
`ListBoxItem` binding picks it up reactively. Keep the `SelectionOverlay` opacity
`DoubleAnimation` (the eased visual highlight) untouched.

```xml
<!-- ListBoxItem (Controls.xaml) / DataGridRow (Controls.DataGrid.xaml) -->
<Style.Triggers>
    <Trigger Property="IsSelected" Value="True">
        <Setter Property="Foreground" Value="{DynamicResource SelectionForegroundBrush}"/>
    </Trigger>
</Style.Triggers>
```

Removed the two `ObjectAnimationUsingKeyFrames Foreground` blocks (the `Selected` and
`SelectedUnfocused` states) from the `ListBoxItem` template and the one from the
`DataGridRow` template.

## Prevention

- **A VSM `Storyboard` with no `Storyboard.TargetName` targets the template's visual root,
  not the templated control.** If that root is a `Panel`/`Border`/`Decorator`, it has no
  `Foreground`/`Font*`/text properties — animating them throws "Cannot resolve all property
  references in the property path '…'".
- **To change a templated control's own text-related property (Foreground, FontWeight) on a
  state, prefer a property `Trigger` on the `Style`** (`IsSelected`, `IsMouseOver`,
  `IsFocused`) over a VSM storyboard. Triggers target the control directly; VSM is for
  animating *named child elements* of the template (overlays, transforms, opacity).
- If you must animate text colour, target a **named child `Control`/`ContentPresenter`** and
  animate `(TextElement.Foreground)` on it, never the bare `Foreground` on a `Panel` root.
- A clean `dotnet build` does **not** validate VSM/trigger storyboards — they apply at
  runtime. Any new templated control with selection/hover states must be exercised through
  every state at least once before it is considered verified. "Build is 0/0" is
  necessary-but-not-sufficient. (Same lesson as
  `[[wpf-dynamicresource-on-condition-binding]]`.)

## Related

- Links to related Agent Wiki entries: `[[wpf-dynamicresource-on-condition-binding]]`,
  `[[wpf-vista-skeleton-loaders]]`, `[[wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell]]`
- Links to Domain Wiki pages: `[[client-server-wpf]]`
