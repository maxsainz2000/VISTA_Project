---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-03
tags: [wpf, xaml, theming, design-tokens, design-system]
---

# WPF VISTA Theming and Color Conventions

## Context

When modifying or creating view files in the WPF application (`MerchSys.App`), all visual properties (backgrounds, text colors, borders, font sizes, corner radii, drop shadows, and control stylings) must strictly align with the VISTA Design System tokens. This ensures the application dynamically adapts to Light and Dark mode switches and maintains a consistent, premium macOS-inspired visual aesthetic.

## The Pattern

### 1. No Literal Hex Colors
All hardcoded colors (e.g., `#1A252F`, `#DFE6E9`, `#FAFBFC`) are banned in view files (`Views/`). Use `{DynamicResource TokenBrushName}` to reference theme-aware brushes instead.

```xml
<!-- Avoid: -->
<Window Background="#1A252F">
    <TextBlock Foreground="#BDC3C7" Text="Hello"/>
</Window>

<!-- Use: -->
<Window Background="{DynamicResource WindowBackgroundBrush}">
    <TextBlock Foreground="{DynamicResource TextSecondaryBrush}" Text="Hello"/>
</Window>
```

### 2. Rely on Implicit Styles
Remove custom, ad-hoc control styling overrides (such as custom `Style="{StaticResource InputBox}"` for text fields) to let controls fall back to the implicit, theme-aware templates declared in `Themes/Controls.xaml` and `Themes/Controls.DataGrid.xaml`.

```xml
<!-- Avoid: -->
<TextBox Style="{StaticResource InputBox}"/>

<!-- Use (implicitly gets VISTA rounded focus style): -->
<TextBox/>
```

### 3. Primary Button and Semantic Styling
- Use `{StaticResource AccentButtonStyle}` for primary action buttons.
- For semantic buttons (e.g. green FileButton, orange AmendButton), do not base them on `ActionButton` (since its base `AccentButtonStyle` overrides background triggers dynamically on hover/pressed). Instead, base them on the implicit button style `{StaticResource {x:Type Button}}` and set `Background` and `BorderBrush` to the semantic brush (e.g., `{DynamicResource SuccessBrush}`); the implicit template overlays transparent ink for hovers correctly.

```xml
<!-- Example of a green success button: -->
<Style x:Key="FileButton" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
    <Setter Property="Background" Value="{DynamicResource SuccessBrush}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource SuccessBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource TextOnAccentBrush}"/>
</Style>
```

### 4. Centered Card Container for Modals
Modals, warning overlays, and standalone input forms (e.g., Login and Session Timeout dialogs) should wrap their content in a centered card layout to establish breathing room, structure, and depth.

```xml
<Grid Margin="24">
    <Border Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource SeparatorBrush}"
            BorderThickness="1"
            CornerRadius="{DynamicResource RadiusMedium}"
            Effect="{DynamicResource CardShadow}"
            Padding="32,28">
        <StackPanel>
            <!-- Content elements here -->
        </StackPanel>
    </Border>
</Grid>
```

### 5. Type Scale and Spacing Ramps
- Use the typographic scale double values (`FontSizeCaption`, `FontSizeBody`, `FontSizeSubhead`, `FontSizeTitle`, `FontSizeLargeTitle`) via `{DynamicResource FontSize*}` rather than hardcoded point sizes.
- Use `Thickness` tokens (`SpacingXS`, `SpacingS`, `SpacingM`, `SpacingL`, `SpacingXL`) for layout margins and paddings.

## Why It Works

By routing all colors, typography, and dimensions through the VISTA Design System tokens, WPF is able to dynamically hot-reload palettes (such as changing resource references from `Themes/Light.xaml` to `Themes/Dark.xaml` at runtime) without needing to restart the application. Avoiding hardcoded colors prevents visual fragmentation and layout/text contrast issues.

## Rules

- **Never** hardcode hex colors in `Views/` (excluding the theme dictionaries under `Themes/`).
- **Always** let input fields (`TextBox`, `PasswordBox`, `ComboBox`) and `DataGrid` tables fall back to their implicit VISTA styles to keep the user experience uniform.
- **Always** wrap modals and card-like components in a `Border` utilizing the `RadiusMedium` and `CardShadow` tokens.

## Related

- Plan Reference: `[[04-view-migration]]`
- Theme Overview: `[[macos-theme-overview]]`
- Controls Style: `[[03-control-styles]]`
