---
module: MerchSys.Accounting
plan-id: ACC-16
title: "VatPayableTile Financial Overview Placement & Navigation Wiring"
depends-on: [ACC-07, ACC-12, ACC-14]
estimated-files: 2
priority: medium
---

# VatPayableTile Financial Overview Placement & Navigation Wiring

## Context

ACC-12 delivered the `VatPayableTile` as a standalone `UserControl` and the `VatPayableKpiProvider` / `VatEnrichedFinancialOverviewService` decorator. ACC-14 delivered the `VatTileSmokeHarness` and confirmed the navigation wiring pattern. However, the 2026-05-15 Accounting audit noted that the tile is **not yet placed** into `FinancialOverviewView.xaml` — it exists as a ready-to-drop control but is invisible to the user.

This plan performs the cosmetic integration: inserting `VatPayableTile` into the Financial Overview view layout and wiring the `NavigateToVatReturnRequested` event to the shell navigation command.

## Prerequisites

- **ACC-07** (View — Financial Overview) — `FinancialOverviewView.xaml`, `FinancialOverviewView.xaml.vb`
- **ACC-12** (VAT Payable KPI) — `VatPayableTile.xaml` UserControl, `FinancialOverviewVatExtension` partial class
- **ACC-14** (VAT Tile Integration) — Navigation wiring pattern using type-based `NavigationItem` resolution

## Wiki References

- `concepts/vat-ready.md` — VAT Payable visibility requirement for dashboard

## Deliverables

```
MerchSys.App/Views/Accounting/
└── FinancialOverviewView.xaml                  ' Modified — add VatPayableTile to KPI grid

MerchSys.App/Views/Accounting/
└── FinancialOverviewView.xaml.vb               ' Modified — wire NavigateToVatReturnRequested
```

## Specification

### XAML Placement

Insert `VatPayableTile` into the existing KPI tile grid in `FinancialOverviewView.xaml`. The tile should appear after the existing AP tile and before any spacing/filler:

```xml
<!-- Existing KPI tiles -->
<local:RevenueTile Grid.Row="0" Grid.Column="0" />
<local:MarginTile Grid.Row="0" Grid.Column="1" />
<local:ArTile Grid.Row="1" Grid.Column="0" />
<local:ApTile Grid.Row="1" Grid.Column="1" />

<!-- NEW: VAT Payable tile -->
<components:VatPayableTile Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2"
    x:Name="VatPayableTile"
    DataContext="{Binding}" />
```

Add the `components` XML namespace if not already present:
```xml
xmlns:components="clr-namespace:MerchSys.App.Views.Accounting.Components"
```

If the grid does not have a third row, add `<RowDefinition Height="Auto"/>`.

### Navigation Event Wiring

In `FinancialOverviewView.xaml.vb`, handle the tile's click/navigation event:

```vb
Private Sub VatPayableTile_NavigateToVatReturnRequested(sender As Object, e As EventArgs)
    ' Use type-based NavigationItem resolution per INT-13 pattern
    Dim vm = TryCast(DataContext, FinancialOverviewViewModel)
    If vm IsNot Nothing Then
        vm.NavigateToVatReturnCommand.Execute(Nothing)
    End If
End Sub
```

The command should resolve `NavigationItem` by type (not by string key `"VatReturn"`) as mandated by INT-13's pattern. ACC-14's implementation already established this — verify and reuse.

## Implementation Notes

- This plan **modifies** ACC-07 source files. Previous plans (ACC-12, ACC-14) deliberately avoided this. Authorization is granted by the ACC-12 pending task which explicitly called out "cosmetic integration — modifies ACC-07 view file, requires separate authorisation."
- The exact grid layout (row/column positions) must be verified against the current XAML at implementation time — the positions shown above are illustrative.
- Owner role sees the tile but clicking does not navigate (gated by ACC-11's Manager-only check). No additional role gating is needed in this plan.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `VatPayableTile` is visible in the Financial Overview view at runtime.
3. Clicking the tile as **Manager** navigates to `VatReturnView`.
4. Clicking the tile as **Owner** does not navigate (existing ACC-11 gate applies).
5. The tile displays the current period's VAT Payable amount with correct severity colouring.
6. Navigation uses type-based `NavigationItem` resolution, not string invocation.
7. No runtime XAML binding errors in the Output window.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-16-summary.md` using `Progress/_template.md`. Include:
- Screenshot or description of the tile placement in the Financial Overview grid.
- The `NavigateToVatReturnRequested` wiring code.
- Confirmation that type-based navigation pattern is used per INT-13.

### Documentation
- Inline comment in XAML noting this tile was placed by ACC-16 and its data source is ACC-12's decorator.
