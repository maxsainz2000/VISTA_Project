---
name: wpf-vista-tooltips
description: Rules for tooltip placement, shared tooltip style, and bound-tooltip-in-template sourcing in VISTA WPF views.
metadata:
  type: pattern
---

# WPF / VISTA — Tooltip Conventions (UX-22)

## Rule 1 — Reuse existing text; don't invent inconsistent labels

Tooltip text must match labels already used in the app. Rail/module buttons bind `ToolTip="{Binding ToolTipText}"` to `RailItem.ToolTipText` (set in `ActivityRailViewModel.BuildRailItems`), so tooltip and screen name never drift. For other buttons, use the exact wording from the command mnemonic label (e.g. "Remove from cart" matches the DataGrid column semantics).

## Rule 2 — Target icon-only controls; skip text-bearing ones

A button that already shows text does NOT need a tooltip. Target only controls where the sole affordance is an icon or visual glyph.

VISTA icon-only controls (and their canonical tooltip text):
| Control | File | Tooltip |
|---|---|---|
| Activity Rail module buttons | `ActivityRail.xaml` | Bound to `RailItem.ToolTipText` |
| "V" identity mark | `ActivityRail.xaml` | "VISTA — Villon Integrated Supply and Trade Application" |
| Password eye toggle | `LoginView.xaml` | "Show / hide password" |
| New-password eye toggle | `LoginView.xaml` | "Show / hide new password" |
| Cart remove button (X icon) | `SalesCartView.xaml` | "Remove from cart" |
| Dark mode toggle switch | `ModuleDetailPanel.xaml` | "Toggle dark mode" |
| Sparkline bars | `Sparkline.xaml` | Bound value (pre-existing) |

Text-bearing controls (no tooltip needed): all toolbar Refresh/Add/Edit/Delete buttons, Log Out, Retry, Generate, etc.

## Rule 3 — Bound tooltips in DataTemplates

When binding a `ToolTip` on a control *inside* a `DataTemplate`, set the attribute directly on the element:

```xaml
<Button ToolTip="{Binding ToolTipText}" .../>
```

This works because the `Button`'s DataContext is the template item, and WPF resolves the `ToolTip` attribute through the visual tree from the `Button` itself (not from a standalone `<ToolTip>` element). No `PlacementTarget`/`RelativeSource` workaround is required for this pattern.

If you instead wrap the tooltip in a `<Button.ToolTip><ToolTip>` element, the `ToolTip` is outside the visual tree and loses the DataContext — you would need:

```xaml
<Button.ToolTip>
    <ToolTip Content="{Binding RelativeSource={RelativeSource Self}, Path=PlacementTarget.DataContext.ToolTipText}"/>
</Button.ToolTip>
```

Prefer the attribute form to avoid this.

## Rule 4 — Shared tokenized tooltip style

An implicit `ToolTip` style is defined in `Components.xaml` (merged at position [4] in `Application.xaml`). It uses `DynamicResource` tokens and a `ControlTemplate` override for rounded corners:

```xaml
<Style TargetType="{x:Type ToolTip}">
    <Setter Property="Background"     Value="{DynamicResource SurfaceBrush}"/>
    <Setter Property="Foreground"     Value="{DynamicResource TextPrimaryBrush}"/>
    <Setter Property="BorderBrush"    Value="{DynamicResource SeparatorBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding"        Value="8,5"/>
    <Setter Property="FontFamily"     Value="{DynamicResource AppFontFamily}"/>
    <Setter Property="FontSize"       Value="{DynamicResource FontSizeCaption}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type ToolTip}">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="{DynamicResource RadiusSmall}"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

This style applies to every `ToolTip` in the app. Theme switches (Light ↔ Dark) automatically update `SurfaceBrush`, `TextPrimaryBrush`, and `SeparatorBrush`.

**No inline hex.** Never hardcode colors in tooltip markup — always use `DynamicResource`.

## Rule 5 — P1 automation-name candidates

The tooltip text on icon-only controls is the natural candidate for `AutomationProperties.Name` in the future Pro P1 accessibility pass. When writing tooltips, phrase them as action labels ("Remove from cart", not "Removes the selected item from the shopping cart") so they double as automation names.
