---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-03
tags: [wpf, xaml, dashboard, layout, overflow]
---

# WPF VISTA Dashboard Layout Standards

This document establishes the official layout standards for VISTA dashboards, specifically covering the **Metric-Hierarchy vocabulary (the 40-30-20-10 rule)**, the **responsive FilterBar container**, and the **overflow safety standard**.

---

## 1. Metric-Hierarchy (The 40-30-20-10 Rule)

Dashboards must express distinct weight through type scale and styling rather than ad-hoc configurations or per-view font sizes.

| Tier | Weight / Role | Shared Card Style | Shared Value Style | Caption Style |
| :--- | :--- | :--- | :--- | :--- |
| **Primary** | Hero Metric (~40% weight) | `PrimaryMetricCardStyle` | `PrimaryMetricValueStyle` | `MetricCaptionStyle` |
| **Secondary** | Supporting KPI (~30% weight) | `SecondaryMetricCardStyle` | `SecondaryMetricValueStyle` | `MetricCaptionStyle` |
| **Tertiary** | Long Tail / Details (~20% weight) | `TertiaryMetricStyle` | Direct inline sizing (`FontSizeSubhead`/`FontSizeBody`) | `MetricCaptionStyle` |

### Key Guidelines
- **Primary Hero Card:** Accent-colored border (1px `AccentBrush`) with larger padding (`SpacingXL` = 24px) and soft elevation.
- **Secondary Card:** Neutral `SurfaceBrush` with standard padding (`SpacingL` = 16px).
- **Tertiary Card:** Ultra-compact card (`12,8` padding) with smaller corner radius (`RadiusSmall` = 6px).
- **Semantic Overrides:** Colors like `DangerBrush` or `WarningBrush` should be applied directly as `Foreground` property overrides on the value `TextBlock` and not via custom hex values.

### 1.1 The "Promote-Don't-Add" Rule
- **Core Principle:** Under a behavior-frozen UI rollout, a dashboard's Hero metric must bind directly to an **existing** property exposed by its ViewModel. Creating or modifying properties on the ViewModel is prohibited.
- **Fallback Rule:** If the ideal decision-relevant Hero metric is not yet present on the ViewModel, choose the next best available property, apply the `PrimaryMetricCardStyle` and `PrimaryMetricValueStyle`, and document the ideal metric in the implementation summary for future updates (e.g. UX-12/UX-13).

### 1.2 Canonical Dashboard Hero Mapping
The following table documents the designated Hero metrics, their VM bindings, and layout adaptations across all 10 VISTA dashboards:

| View File | Hero Metric | VM Binding Path | Layout / Composition Adaptation |
| :--- | :--- | :--- | :--- |
| [OwnerDashboardView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/OwnerDashboardView.xaml) | Today's Revenue | `TodayRevenue` | Standalone Hero row at the top; module cards demoted to secondary with values chunked in 2x2 grids. |
| [FinancialOverviewView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml) | Month-to-Date Revenue | `MonthToDateRevenue` | 1 hero (Col 0), 4 secondary, 3 tertiary in a compact strip. Alerts panel re-docked above the chart (Dock="Top"). |
| [DailySummaryView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/POS/DailySummaryView.xaml) | Total Sales | `TotalSalesDisplay` | Promote Total Sales to Hero; others secondary. Remove inline borders. |
| [SalesSummaryView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml) | Total Sales | `TotalSalesDisplay` | Promote Total Sales to Hero (Col 0); others secondary. |
| [StockDashboardView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Inventory/StockDashboardView.xaml) | Critical Stockout Risk | `CriticalStockoutCount` | Promote to Hero (Col 0); others secondary. De-duplicate triple status encoding by removing redundant triggers on the Expiry column. |
| [ExpiryMonitorView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml) | Total Value at Risk | `TotalValueAtRisk` | Promote to Hero (Col 0); others secondary. |
| [CreditManagementView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/POS/CreditManagementView.xaml) | Total Outstanding AR | `TotalOutstanding` | Promote to Hero (Col 0) with a 1.3* column; others secondary. |
| [ShrinkageView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Inventory/ShrinkageView.xaml) | Period Shrinkage Value | `PeriodTotalValue` | Promote to Hero (Col 0). Demote Filtered Records. Remove "Last Refreshed" card, relocating `LastRefreshed` binding to the toolbar. |
| [VatReturnView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/VatReturnView.xaml) | VAT Payable | `VatPayableDisplay` | Promote to Hero (Col 0, 1.3* width with dynamic background triggers); others secondary. |
| [VatReliefReportView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml) | Net VAT Payable | `NetVatPayable` | Apply `PrimaryMetricCardStyle` to Net VAT Payable band Border, and update value TextBlock base style to `PrimaryMetricValueStyle` preserving dynamic color triggers. |

---

## 2. Responsive FilterBar Container

To prevent clipping and ensure toolbars reflow gracefully on narrow viewports, all search and filter blocks must use the `FilterBarStyle` container with a horizontal `WrapPanel`.

### Canonical Code Snippet
```xml
<!-- Responsive filter bar container -->
<Border Style="{StaticResource FilterBarStyle}">
    <WrapPanel Orientation="Horizontal">
        <!-- Filter Item 1 -->
        <StackPanel Style="{StaticResource FilterBarItemStyle}" Orientation="Horizontal">
            <TextBlock Text="Search:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox Width="150" VerticalAlignment="Center"/>
        </StackPanel>
        
        <!-- Filter Item 2 -->
        <StackPanel Style="{StaticResource FilterBarItemStyle}" Orientation="Horizontal">
            <TextBlock Text="Status:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <ComboBox Width="120" VerticalAlignment="Center"/>
        </StackPanel>
    </WrapPanel>
</Border>
```

---

## 3. Overflow Safety Standard

Clipping dashboard content is unacceptable. Views must gracefully support scrolling on smaller monitors while keeping key navigation and status bars persistent.

### The Overflow Rule
Any view whose content height can exceed the viewport (e.g., dashboards, reports, and settings forms) must wrap its scrolling body in:
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" 
              HorizontalScrollBarVisibility="Disabled">
    <!-- Scrolling Body Content -->
</ScrollViewer>
```

### Persistent Chrome Placement
- Navigation elements, main toolbars, and overlay screens (like `BusyOverlay` or loading spinners) must sit **outside** the `ScrollViewer` so they do not scroll away.
- Grids or list views that support their own virtualized scrolling must **not** be double-wrapped.

## Related

- `[[wpf-vista-iconography]]`
