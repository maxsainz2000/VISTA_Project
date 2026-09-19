---
module: MerchSys.App
agent: antigravity
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/11-layout-resilience-sweep.md
status: completed
---

# Implementation Progress Report - UX-11

## Task Summary

Implemented the UX-11 Layout-Resilience Sweep across all affected VISTA views in the `MerchSys.App` project. This includes wrapping stacked card/section bodies in `ScrollViewer` elements (Set A) to prevent silent clipping on short or zoomed windows, and converting rigid, fixed-column `Grid` toolbars to responsive `WrapPanel` layouts using the `FilterBarStyle` and `FilterBarItemStyle` shared components (Set B) to prevent control truncation on narrow screens. All bindings, commands, event handlers, and styles remain fully intact, maintaining strict "behavior frozen" compliance.

**Plan:** `[[11-layout-resilience-sweep.md]]`

## What Was Done

- Modified XAML layouts for 11 Views in `WPF_Applications/MerchSys/src/MerchSys.App/Views/`:
  - **`Accounting/FinancialOverviewView.xaml`**: Wrapped the main dashboard body (KPI cards, What This Means, Alerts panel, and top products cards/charts) in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside the scroll wrap. Relieved zero-height collapse risk by giving the trend chart `Border` and Top Products `Border` explicit `MinHeight="240"`.
  - **`Accounting/VatReturnView.xaml`**: Wrapped upper elements (What This Means, Period/Filing Controls Panel, Status Message, and Summary Cards row) in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside. Kept the `LINES DETAIL` container (containing the virtualized `DataGrid` and `EmptyStatePanel`) outside the scroll wrap to prevent double-wrapping and maintain grid virtualization.
  - **`Accounting/VatReliefReportView.xaml`**: Wrapped Net VAT Payable card and Sales/Purchases Summary cards in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside. Kept the `TRAILING 12 MONTHS GRID` border (containing the `DataGrid` and `EmptyStatePanel`) outside to maintain independent grid scrolling.
  - **`OwnerDashboardView.xaml`**: Wrapped the main KPI card grid in a `ScrollViewer` and changed its row definitions from `*` to `Auto` to support natural expansion and scrolling on short windows. Kept the header and loading overlay outside.
  - **`Inventory/ProductManagementView.xaml`**:
    - **Products Tab Toolbar**: Converted the 15-column fixed `Grid` toolbar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped the Category and Search label-input controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle` to keep labels paired with inputs.
    - **Product Editor Overlay**: Wrapped the `Border` dialog card in a `ScrollViewer` with stretch alignments, adding a `Margin="20"` to the card to prevent modal clipping/cutoff on short viewport heights.
  - **`POS/VatSettingsView.xaml`**: Wrapped the entire stacked settings card list (Validation Errors, Registration Status, Tax Rates, Business Info, and Status Message) in a `ScrollViewer` + `StackPanel` container, keeping the toolbar and `BusyOverlay` outside.
  - **`Inventory/StockDashboardView.xaml`**: Converted the 11-column fixed `Grid` filter bar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped Category, Status, and Search controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.
  - **`Purchasing/PurchaseOrderListView.xaml`**: Converted the fixed-column `Grid` toolbar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped Status and Search filters in `StackPanel` elements styled with `FilterBarItemStyle`, and wrapped the manager action buttons `StackPanel` in `FilterBarItemStyle` so the entire action group wraps as a single unit.
  - **`Inventory/ShrinkageView.xaml`**: Converted the `DockPanel`/`StackPanel` filter toolbar into a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped From Date, To Date, Product, and Reason controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.
  - **`Inventory/ExpiryMonitorView.xaml`**: Converted the mixed `Grid` toolbar into a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped the threshold days input and buttons in a horizontal `StackPanel` styled with `FilterBarItemStyle`.
  - **`Accounting/TamperAuditReportView.xaml`**: Converted the horizontal `StackPanel` filter bar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped From and To Date pickers in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 warnings, 0 errors) |
| Unit tests pass | N/A |
| Manual verification | ✅ |

## Issues Encountered

- **Chart Layout Collapse inside ScrollViewer**: Placing `DockPanel-fill` or dynamically-sized elements like bar charts inside a vertical `ScrollViewer` collapses their height to zero because the `ScrollViewer` measures children with infinite vertical space. Sized cards containing charts with an explicit `MinHeight="240"` to mitigate this.
- **Dangling Tags during Large replacements**: When replacing large XAML blocks, parser tags must be carefully audited to ensure closing elements (`</Border>`, `</Grid>`) are correctly matched. Corrected a dangling `Grid` tag on the Expiry Monitor View toolbar by converting it to `WrapPanel` correctly.

## What's Next

- [x] Complete layout-resilience sweep (UX-11).
- [x] Implement trend data visualization and sparklines — resolved by UX-12.

## Cross-References

- Domain Wiki pages consulted: None
- Agent Wiki entries consulted: `[[wpf-vista-dashboard-layout]]`, `[[wpf-vista-theming-conventions]]`
