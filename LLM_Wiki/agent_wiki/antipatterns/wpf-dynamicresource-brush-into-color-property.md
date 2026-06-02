---
type: antipattern
module: MerchSys.App
agent: claude-code
date: 2026-06-03
tags: [wpf, xaml, theming, dynamicresource, solidcolorbrush, color, runtime-error]
error-code: XamlParseException
severity: runtime-error
---

## Problem

The shell threw at view load (UX-02 reskin):

```
System.Windows.Markup.XamlParseException
  Message='Set property 'System.Windows.FrameworkElement.Style' threw an exception.'
  Line number '240' ... ModuleDetailPanel.xaml:line 1
Inner Exception 1:
  InvalidOperationException: '#FFF5F5F7' is not a valid value for property 'Color'.
```

Originating markup (in both `ActivityRail.xaml` and `ModuleDetailPanel.xaml`):

```xml
<SolidColorBrush x:Key="HoverBackgroundBrush"
                 Color="{DynamicResource TextPrimaryBrush}" Opacity="0.06"/>
```

The intent was to "derive a subtle hover overlay from the text color." Two non-obvious traits:
- **Build is clean (0/0).** XAML resource type mismatches behind `DynamicResource` are not caught at compile time — they fail only when the dictionary is realized at load.
- **It only blows up in one theme.** The error value `#FFF5F5F7` is the Dark palette's `TextPrimaryBrush` (`#F5F5F7`). It surfaced because the app booted into the persisted Dark theme; the same markup may appear to "work" if you only ever test Light.

## Root Cause

`SolidColorBrush.Color` is typed `System.Windows.Media.Color` (a struct). The token `TextPrimaryBrush` is a **`SolidColorBrush`**, not a `Color`. Feeding a brush resource into a `Color` property is an unconvertible type mismatch — WPF stringifies the brush (`#FFF5F5F7`) and reports it as an invalid `Color`.

You cannot declaratively derive one brush's `Color` from another brush resource via `DynamicResource`. There is no markup path that says "take that brush's color and lower the alpha."

## Fix

A hover/interaction overlay is inherently **theme-specific** (dark ink over light surfaces, light ink over dark), so it belongs in each palette as a real semi-transparent brush — not derived inline.

```xml
<!-- Before (broken) — local resource in ActivityRail.xaml / ModuleDetailPanel.xaml -->
<SolidColorBrush x:Key="HoverBackgroundBrush"
                 Color="{DynamicResource TextPrimaryBrush}" Opacity="0.06"/>
<!-- ...consumed via {StaticResource HoverBackgroundBrush} -->

<!-- After (fixed) — defined per palette -->
<!-- Light.xaml -->
<SolidColorBrush x:Key="HoverBackgroundBrush" Color="#1D1D1F" Opacity="0.06"/>
<!-- Dark.xaml -->
<SolidColorBrush x:Key="HoverBackgroundBrush" Color="#FFFFFF" Opacity="0.08"/>
<!-- ...consumed via {DynamicResource HoverBackgroundBrush} so it re-colors on toggle -->
```

The opacity-fade animations on the `HoverBg`/`Bg` borders keep their `To="1"`; the brush itself now carries the alpha.

## Prevention

- **`Color="…"` only accepts a `Color`.** Never point it at a `…Brush` resource. If you see `'#AARRGGBB' is not a valid value for property 'Color'`, you've bound a brush into a Color slot.
- A semi-transparent variant of a themed color is **not derivable in markup** — author it explicitly in each palette (`Color` literal + `Opacity`, or an `#AARRGGBB` brush).
- Overlay/hover brushes are theme-specific → palette files (`Light.xaml`/`Dark.xaml`), kept in key parity. Theme-agnostic structure stays in `Tokens.xaml`.
- Consume palette brushes with `{DynamicResource}` (not `{StaticResource}`) so a runtime theme swap re-colors them.
- **Test both themes.** A clean build and a working Light theme do not prove the Dark dictionary realizes. Boot once per palette (or clear `%LOCALAPPDATA%\MerchSys\ui-settings.json`).

## Related

- Domain/stack: `[[client-server-wpf]]`
- Epic plan: `Plans/VISTA_Modules/Experience/00-macos-theme-overview.md` (token contract)
- Sibling WPF runtime-binding bug: `[[wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell]]`
