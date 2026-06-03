---
type: layer-manifest
module: MerchSys.Inventory
layer: Views
last-updated: 2026-06-03
---

# MerchSys.Inventory — Views & ViewModels

This page details the Presentation layer (Views and ViewModels) for the **MerchSys.Inventory** module.

## Views and ViewModels

| View File Path | ViewModel File Path | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml`<br>`src/MerchSys.App/Views/Inventory/StockDashboardView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb` | Main Inventory dashboard displaying products, quantities, values, status colors, and alerts. Includes filtering and auto-refresh functionalities. Displays Retail Price, Avg Cost, and FIFO Cost side-by-side (INV-15). |
| `src/MerchSys.App/Views/Inventory/ProductManagementView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductManagementView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb` | View for the product catalog management (manager-only). Handles adding/editing products, managing categories, setting retail prices, and configuring alert thresholds. Enforces SKU uniqueness. |
| `src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml`<br>`src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb` | Dedicated view for monitoring batch expiry dates. Features near-expiry alerts, expired batch listing, configurable threshold, and one-click write-off to shrinkage. Uses auto-refresh timer. |
| `src/MerchSys.App/Views/Inventory/ShrinkageView.xaml`<br>`src/MerchSys.App/Views/Inventory/ShrinkageView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ShrinkageViewModel.vb` | WPF View for recording and viewing inventory shrinkage (damage, spoilage, admin discrepancies). Shows history with financial impact. Includes Record Shrinkage dialog, history datagrid, filters, and period total summary. |
| `src/MerchSys.App/Views/Inventory/ProductPriceHistoryView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductPriceHistoryView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ProductPriceHistoryViewModel.vb` | Read-only popup dialog showing price change history ledger for a product (date, old/new prices, delta, modified by, and optional reason) with color-coded increase/decrease visual triggers. |
