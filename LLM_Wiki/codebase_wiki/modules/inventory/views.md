---
type: layer-manifest
module: MerchSys.Inventory
layer: Views
last-updated: 2026-05-04
---

# MerchSys.Inventory — Views & ViewModels

This page details the Presentation layer (Views and ViewModels) for the **MerchSys.Inventory** module.

## Views and ViewModels

| View File Path | ViewModel File Path | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml`<br>`src/MerchSys.App/Views/Inventory/StockDashboardView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb` | Main Inventory dashboard displaying products, quantities, values, status colors, and alerts. Includes filtering and auto-refresh functionalities. |
| `src/MerchSys.App/Views/Inventory/ProductManagementView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductManagementView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb` | View for the product catalog management (manager-only). Handles adding/editing products, managing categories, setting retail prices, and configuring alert thresholds. Enforces SKU uniqueness. |
| `src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml`<br>`src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml.vb` | `src/MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb` | Dedicated view for monitoring batch expiry dates. Features near-expiry alerts, expired batch listing, configurable threshold, and one-click write-off to shrinkage. Uses auto-refresh timer. |
