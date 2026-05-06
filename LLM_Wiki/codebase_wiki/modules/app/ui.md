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
