---
type: layer-manifest
module: MerchSys.App
layer: UI
last-updated: 2026-05-06
---

# MerchSys.App — UI (Views)

This page details the WPF View implementations (XAML and code-behind) in the **MerchSys.App** module.

## Accounting Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml`<br>`src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` | `FinancialOverviewView` | Primary Accounting dashboard. Features KPI cards, a 6-month trend chart, Top Products list, and an alerts panel. | `FinancialOverviewViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml`<br>`src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml.vb` | `IncomeStatementView` | Income Statement (P&L) View. Features period switching (Monthly/Quarterly/Annual), a "What This Means" panel, and a detailed P&L layout with a per-product margins tab. | `IncomeStatementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml`<br>`src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml.vb` | `SalesSummaryView` | Sales Summary View. Displays daily/weekly/monthly sales breakdowns by payment method with KPI summary cards and "What This Means" interpretation. | `SalesSummaryViewModel` (Constructor Injection) |
