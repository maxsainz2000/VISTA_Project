---
name: wpf-vista-design-gallery
description: Pattern for building a living catalogue-from-live-dictionaries design gallery in WPF — enumerates tokens/components/icons from the real merged ResourceDictionaries so the gallery can never drift from reality. Doubles as the design-system realization and regression surface.
metadata:
  type: pattern
---

# WPF VISTA Design Gallery — Catalogue-from-Live-Dictionaries

## Pattern

A Developer-role-only `UserControl` that renders every design token, shared component, and icon from the app's live `ResourceDictionary` merged chain. No values are copied into the gallery — everything is pulled via `{DynamicResource}`. Toggling the theme (`IThemeService.Toggle()`) instantly recolors the entire gallery, making it a visual regression surface: if any swatch or component fails to recolor, a token key is broken.

## Implemented in

`Views/DeveloperTools/DesignGalleryView.xaml` (UX-43)

## Why this approach

**Why:** The alternative — a static screenshot or a documentation page with hardcoded hex values — drifts from reality the moment a token changes. Pulling from the live dictionaries means the gallery is always synchronized with the actual design system state. The developer can boot the app, switch to the Developer Tools → "Design Gallery" nav item, and instantly see whether every token, component, and icon realizes correctly in both Light and Dark themes.

**How to apply:** Whenever a new design token or shared component is added:
1. Add a `{DynamicResource}` swatch/instance to the gallery — no need to maintain a separate document.
2. The gallery serves as the "does this exist?" answer for the entire codebase.

## Key mechanics

### Token swatches without inline hex

```xml
<!-- CORRECT: pulls live brush token — recolors on theme swap -->
<Border Width="68" Height="40" Background="{DynamicResource AccentBrush}"
        CornerRadius="{DynamicResource RadiusSmall}"/>

<!-- WRONG: hardcoded hex breaks the regression-surface purpose -->
<!-- <Border Background="#007AFF"/> -->
```

### BusyOverlay preview via local-value override

`BusyOverlay` hides itself when `IsBusy=False` or `(IsBusy=True AND LastLoadedAt=null)` using `Style.DataTrigger`. The gallery needs it visible. WPF DP precedence: local value (level 3) beats style triggers (level 6), so:

```xml
<!-- Visibility="Visible" as XAML attribute = local value → wins over DataTriggers -->
<shell:BusyOverlay Visibility="Visible" IsBusy="True" Message="Loading products…"/>
```

This is a display-only preview — the spinner animates but no data loads.

### Sparkline with inline literal Points

`Sparkline.Points` is `IEnumerable(Of Double)`. Use `x:Array`:

```xml
<shell:Sparkline Height="48" Width="120">
    <shell:Sparkline.Points>
        <x:Array Type="{x:Type sys:Double}">
            <sys:Double>10</sys:Double>
            <sys:Double>25</sys:Double>
            <sys:Double>38</sys:Double>
        </x:Array>
    </shell:Sparkline.Points>
</shell:Sparkline>
```

`Double[]` implements `IEnumerable(Of Double)`, so the assignment is valid. Requires `xmlns:sys="clr-namespace:System;assembly=mscorlib"` (consistent with `Tokens.xaml`).

### DeltaIndicator — set Percent not Direction

`Direction` is a read-only DP computed from `Percent`. Set `Percent` only:

```xml
<shell:DeltaIndicator Percent="12.5"/>   <!-- Up, SuccessBrush -->
<shell:DeltaIndicator Percent="-8.3"/>   <!-- Down, DangerBrush -->
<shell:DeltaIndicator Percent="0"/>      <!-- Flat, TextSecondaryBrush -->
```

### Confirmation dialog — static preview, no commands

`ConfirmationDialog` is a `Window` (not a `UserControl`), so it can't be embedded. Render a static layout preview using standard XAML elements with `IsEnabled="False"` on the action buttons. No `ICommand` is bound. The preview must document that the real dialog fires through `IConfirmationPresenter`.

## Role gating

The gallery nav item is added exclusively inside `MainWindowViewModel.BuildDeveloperToolsItems()`, which already gates on `_session.CurrentRole = UserRole.Developer`. No XAML-level gating needed.

## Namespace declarations required

```xml
xmlns:shell="clr-namespace:MerchSys.App.Views.Shell"
xmlns:sys="clr-namespace:System;assembly=mscorlib"
```

Note: `x:Class` uses the relative suffix only (`Views.DeveloperTools.DesignGalleryView`); `clr-namespace` references always use the full root-namespace-prefixed form.

## Related

- [[wpf-vista-theming-conventions]] — token naming convention the gallery enumerates
- [[wpf-vista-iconography]] — IconBase style and geometry keys the gallery renders
- [[wpf-vista-state-feedback]] — BusyOverlay/EmptyStatePanel/ErrorStatePanel components shown in the gallery
