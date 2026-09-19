---
type: antipattern
module: MerchSys.App
agent: claude-code
date: 2026-06-03
tags: [wpf, xaml, theming, controltemplate, setter, scrollbar, track, dependencyproperty, runtime-error]
error-code: XamlParseException
severity: runtime-error
---

## Problem

The app threw on the Login view as soon as the first `TextBox` realized (UX-03 implicit control styles):

```
System.Windows.Markup.XamlParseException
  Message='Set property 'System.Windows.Setter.Property' threw an exception.'
  Line number '82' and line position '37'.
Inner Exception 1:
  ArgumentNullException: Value cannot be null. (Parameter 'property')
```

Originating markup (implicit `ScrollBar` style in `Themes/Controls.xaml`) — an `Orientation=Horizontal`
trigger trying to reconfigure the `Track` for horizontal layout:

```xml
<ControlTemplate.Triggers>
    <Trigger Property="Orientation" Value="Horizontal">
        <Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/>
        <Setter TargetName="PART_Track" Property="DecreaseRepeatButton">   <!-- line 82 -->
            <Setter.Value><RepeatButton Command="ScrollBar.PageLeftCommand" .../></Setter.Value>
        </Setter>
        <Setter TargetName="PART_Track" Property="IncreaseRepeatButton"> ... </Setter>
        <Setter TargetName="PART_Track" Property="Thumb"> ... </Setter>
    </Trigger>
</ControlTemplate.Triggers>
```

Three non-obvious traits — all shared with the sibling brush-into-`Color` trap
(`[[wpf-dynamicresource-brush-into-color-property]]`):

- **Build is clean (0/0).** Whether a `Setter`'s `Property` resolves to a real `DependencyProperty`
  is checked when the style is *sealed at runtime*, not at compile time.
- **It detonates somewhere surprising.** The error fires on the **Login `TextBox`**, not on a
  scrollbar — because the `TextBox` template hosts a `ScrollViewer` (`PART_ContentHost`) which
  instantiates `ScrollBar`s, which pull in this implicit style. The first control that realizes a
  scrollbar trips it.
- **The line number points at the `Setter`, not the property's declaration.** `line 82, position 37`
  is the `Property="DecreaseRepeatButton"` attribute.

## Root Cause

`Track.DecreaseRepeatButton`, `Track.IncreaseRepeatButton`, and `Track.Thumb` are **plain CLR
properties**, not `DependencyProperty`s. A `<Setter>` can only target a `DependencyProperty` — it
resolves the `Property="..."` string to a `DependencyProperty` and calls into the property system.
For a CLR-only property the lookup yields `null`, and the setter throws
`ArgumentNullException (Parameter 'property')` at style-sealing time.

(`IsDirectionReversed` *is* a DP and would have been fine on its own; the three element-swap setters
are the illegal part.)

You cannot mutate a `Track`'s child elements (its repeat buttons / thumb) through triggers at all —
they are not settable via the property system.

## Fix

A `Track` cannot be reconfigured per-orientation via setters, so author **two complete
`ControlTemplate`s** (vertical default + horizontal) and swap the whole `Template` in a
`Style.Trigger` — `Control.Template` *is* a `DependencyProperty`, so that setter is legal.

```xml
<!-- After (fixed) -->
<ControlTemplate x:Key="VerticalScrollBarTemplate" TargetType="{x:Type ScrollBar}"> ...
    <Track IsDirectionReversed="True"> PageUp/PageDown + Thumb Width=8 </Track> ...
</ControlTemplate>
<ControlTemplate x:Key="HorizontalScrollBarTemplate" TargetType="{x:Type ScrollBar}"> ...
    <Track IsDirectionReversed="False"> PageLeft/PageRight + Thumb Height=8 </Track> ...
</ControlTemplate>

<Style TargetType="{x:Type ScrollBar}">
    <Setter Property="Template" Value="{StaticResource VerticalScrollBarTemplate}"/>
    <Style.Triggers>
        <Trigger Property="Orientation" Value="Horizontal">
            <Setter Property="Width" Value="Auto"/>
            <Setter Property="Height" Value="8"/>
            <Setter Property="Template" Value="{StaticResource HorizontalScrollBarTemplate}"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

## Prevention

- **A `<Setter Property="X">` requires `X` to be a `DependencyProperty`.** If you see
  `Setter.Property threw ... ArgumentNullException (Parameter 'property')`, the named property is a
  CLR property (or a typo), not a DP. The control's docs/Object-Browser will show `DependencyProperty`
  fields for the settable ones (e.g. `Track.IsDirectionReversedProperty` exists;
  `Track.DecreaseRepeatButtonProperty` does **not**).
- **You can't swap a templated element's children via triggers.** To change structure per state/
  orientation, swap the entire `Template` (a DP) — don't try to reach into named sub-elements that
  aren't DP-backed.
- **Test the styles at runtime, in a view that actually hosts the control.** A clean build proves
  nothing about style-sealing; the `ScrollBar` style only sealed when a `TextBox`/`ScrollViewer`
  first realized. Boot the app and open an input form + a scrolling list before declaring control
  styles done.

## Related

- Sibling "compiles clean / fails at dictionary realization" theming trap:
  `[[wpf-dynamicresource-brush-into-color-property]]`
- Domain/stack: `[[client-server-wpf]]`
- Plan: `Plans/VISTA_Modules/Experience/03-control-styles.md` (UX-03 implicit control styles)
