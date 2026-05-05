---
type: layer-manifest
module: MerchSys.Purchasing
layer: Views
last-updated: 2026-05-05
---

# MerchSys.Purchasing — Views & ViewModels

This page details the Presentation layer (Views and ViewModels) for the **MerchSys.Purchasing** module.

## Views and ViewModels

| View File Path | ViewModel File Path | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml`<br>`src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb`<br>`src/MerchSys.Purchasing/ViewModels/PurchaseOrderEditorViewModel.vb` | WPF View and ViewModels for Purchase Order management. Includes a DataGrid for listing POs with status filtering (Draft, Submitted, Received, Verified, Closed) and search. The editor supports creating/editing draft POs with line items, vendor selection, and running total calculation. Enforces role-based access: Manager (full access), Owner (read-only). |
