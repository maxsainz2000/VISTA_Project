---
title: WPF VISTA Iconography — Vector Geometry Icon System
type: pattern
module: MerchSys.App
tags: [wpf, xaml, theming, icons, geometry, design-system, dynamicresource]
agent: claude-code
date: 2026-06-03
status: active
---

# WPF VISTA Iconography — Vector Geometry Icon System

## Rule (one line)

**No emoji or text-glyph icons in views. Every icon is a hand-drawn `Geometry` rendered by a `Path`,
filled with a brush *token* via `DynamicResource`.** (Established UX-07.)

## Why

Emoji/dingbat glyphs (`↻ ⚡ ✓ ✗ ★ ⚠ 🔴 ✅ 💡 📋 📦 📊 💰 📈 💵 📱 🏦 👁 📂`) baked into
`Content=`/`Text=` strings render inconsistently across machines/font fallbacks, **ignore the theme**
(a color-emoji `🔴` cannot become `DangerBrush` or recolor for dark mode), and read as unfinished
against Inter + the token palette. They are the wrong primitive for a controlled design system.

## Mechanism

1. **`Themes/Icons.xaml`** holds the library: keyed `<Geometry>` resources drawn on a shared
   **24×24** viewbox (one coordinate system, consistent optical weight), plus an `IconBase` `Path`
   style:

   ```xml
   <Geometry x:Key="IconWarningGeometry">F0 M12,3 L22,20 L2,20 Z M11,9 ...</Geometry>

   <Style x:Key="IconBase" TargetType="Path">
       <Setter Property="Stretch" Value="Uniform"/>
       <Setter Property="Width"  Value="16"/>
       <Setter Property="Height" Value="16"/>
       <Setter Property="Fill"   Value="{DynamicResource TextPrimaryBrush}"/>
       <Setter Property="VerticalAlignment" Value="Center"/>
       <Setter Property="SnapsToDevicePixels" Value="True"/>
   </Style>
   ```

2. **Merge order** (`Application.xaml`): `Icons.xaml` merges **before** `Components.xaml` so component
   templates can reference icon geometries. Slot order: Tokens → palette (Light/Dark) → Controls →
   Controls.DataGrid → **Icons** → Components.

3. **Consumption** is a `Path` + geometry key + a `Fill` token where the role differs from default:

   ```xml
   <!-- static semantic icon -->
   <Path Style="{StaticResource IconBase}" Data="{StaticResource IconWarningGeometry}"
         Fill="{DynamicResource WarningBrush}"/>

   <!-- inline action-button icon: inherit the button's foreground via binding -->
   <StackPanel Orientation="Horizontal">
       <Path Style="{StaticResource IconBase}" Data="{StaticResource IconRefreshGeometry}"
             Width="13" Height="13" Margin="0,0,6,0"
             Fill="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}"/>
       <TextBlock Text="Refresh" VerticalAlignment="Center"/>
   </StackPanel>
   ```

## Patterns by role

- **Action button** (`↻ Refresh`, `✓ Accept`): replace the glyph-in-string with a horizontal
  `StackPanel` (icon `Path` + label `TextBlock`). Bind the icon `Fill` to the host
  `Button.Foreground` so it tracks the button's role color (white check on a green
  `SuccessButtonStyle`, white on the accent-filled selected pay-method button) and stays
  theme-reactive. **Preserve the button's `Command`, `CommandParameter`, `x:Name`, and `Style`.**
- **Trigger-driven DataGrid status cell** (`★`, `⚠`, `✅`/`🔴`, `✗`/`★`): convert the
  `TextBlock` + `Setter Property="Text"` into a `Path` whose `Data`/`Fill`/`Visibility` is driven by
  the **same** `DataTrigger`s on the **same** bound property. Do **not** change binding paths or
  trigger conditions — only the rendered target.
  ```xml
  <Path Width="14" Height="14" HorizontalAlignment="Center" VerticalAlignment="Center">
      <Path.Style>
          <Style TargetType="Path" BasedOn="{StaticResource IconBase}">
              <Setter Property="Data" Value="{StaticResource IconCheckGeometry}"/>
              <Setter Property="Fill" Value="{DynamicResource SuccessBrush}"/>
              <Style.Triggers>
                  <DataTrigger Binding="{Binding IsBlocked}" Value="True">
                      <Setter Property="Data" Value="{StaticResource IconCircleFillGeometry}"/>
                      <Setter Property="Fill" Value="{DynamicResource DangerBrush}"/>
                  </DataTrigger>
              </Style.Triggers>
          </Style>
      </Path.Style>
  </Path>
  ```
- **Leading-glyph label/banner** (`💡 …`, `⚠ …`, `🔴 …`): wrap in a horizontal `StackPanel` (icon +
  the existing label), move any `Visibility`/`Margin` binding onto the wrapper.
- **DataGrid column header glyph** (`★`): set `<DataGridTemplateColumn.Header>` to a `Path` (drop the
  `Header="★"` string attribute).

## Semantic icon → token map

| Glyph role | Token |
|---|---|
| Warning / caution (`⚠`) | `WarningBrush` |
| Blocked / error dot (`🔴`) | `DangerBrush` |
| OK / accept (`✓`/`✅`) | `SuccessBrush` |
| Insight / "what this means" (`💡`) | `AccentBrush` |
| Neutral pictograph (tiles, clipboard, folder, eye) | `TextSecondaryBrush` |
| Inline button icon | bound to host `Button.Foreground` |

## Path-data fill-rule prefixes

Filled paths only (no stroke). Prefix the path mini-language string to pick a fill rule:

- **`F0` (EvenOdd, also the default)** — carves holes: a second non-overlapping subpath inside a
  larger one becomes negative space (warning exclamation, banknote centre, box seams, phone speaker,
  eye whites/pupil ring).
- **`F1` (Nonzero)** — unions overlapping same-direction subpaths into one solid shape (refresh
  ring + arrowhead, open-folder back + front flap). Use this when subpaths overlap and must merge.

## Traps that still apply

- **Theme-reactive only.** Every `Fill` is a `DynamicResource` token (or a binding to a themed
  `Foreground`). Never hardcode an icon color. Never feed a `SolidColorBrush` token into a `Color`
  property (`[[wpf-dynamicresource-brush-into-color-property]]`).
- A `<Setter>` may target a `DependencyProperty` only — `Path.Data`, `Path.Fill`, `Path.Visibility`
  are all DPs, so trigger-swapping them is valid (`[[wpf-setter-targets-clr-property-not-dependencyproperty]]`).
- **A clean build does not prove a `Geometry`/`Style` resource loads.** Setter-target and
  resource-realization errors surface only when the style is sealed at render time. Boot the app and
  realize every icon (both themes) at least once.
- No new NuGet — icons are hand-drawn geometry, no icon-font / MahApps / FluentIcons.

## Related

`[[wpf-vista-theming-conventions]]`, `[[wpf-dynamicresource-brush-into-color-property]]`,
`[[wpf-setter-targets-clr-property-not-dependencyproperty]]`.
