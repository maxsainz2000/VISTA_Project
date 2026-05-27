---
type: layer-manifest
module: MerchSys.Purchasing
layer: Views
last-updated: 2026-05-27
---

# MerchSys.Purchasing — Views & ViewModels

This page details the Presentation layer (Views and ViewModels) for the **MerchSys.Purchasing** module.

## Views and ViewModels

| View File Path | ViewModel File Path | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml`<br>`src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb`<br>`src/MerchSys.Purchasing/ViewModels/PurchaseOrderEditorViewModel.vb` | WPF View and ViewModels for Purchase Order management. Includes a DataGrid for listing POs with status filtering (Draft, Submitted, Received, Verified, Closed) and search. The editor supports creating/editing draft POs with line items, vendor selection, expected delivery date, notes, and running total calculation. Enforces role-based access: Manager (full access), Owner (read-only). |
| `src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml`<br>`src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/GoodsReceivingViewModel.vb` | WPF View and ViewModel for the goods receiving process. Manager selects a Submitted PO, enters actual quantities received (pre-filled from ordered), captures expiry dates, notes discrepancies (required if qty differs), and confirms receipt via `IGoodsReceivingService`. |
| `src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml`<br>`src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/VendorListViewModel.vb`<br>`src/MerchSys.Purchasing/ViewModels/VendorEditorViewModel.vb` | WPF View and ViewModels for vendor management. Provides a full vendor directory with real-time search, inline create/edit panel with 8-field form validation (Name, Contact, Phone, Email, Address, Lead Time, Notes), and a detail panel showing purchase history (total spent, average lead time) and last 10 recent orders. |
| `src/MerchSys.App/Views/Purchasing/APLedgerView.xaml`<br>`src/MerchSys.App/Views/Purchasing/APLedgerView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/APLedgerViewModel.vb` | WPF View and ViewModel for accounts payable management. Provides a full AP ledger with status filtering (All, Outstanding, Overdue, Paid), vendor-specific filtering, and overdue row highlighting. Supports recording payments via an inline dialog with balance validation. |
| `src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml`<br>`src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/ReorderSuggestionsViewModel.vb` | WPF View and ViewModel for reviewing reorder suggestions. Supports generating suggestions, status filtering (Pending, Accepted, Dismissed), and bulk/single acceptance (auto-creates draft POs). Includes a secondary tab for editing per-product reorder configuration (thresholds, safety stock, seasonal multipliers). |
| `src/MerchSys.App/Views/Purchasing/VendorCatalogView.xaml`<br>`src/MerchSys.App/Views/Purchasing/VendorCatalogView.xaml.vb` | `src/MerchSys.Purchasing/ViewModels/VendorCatalogViewModel.vb` | Manager-only master-detail catalog editor interface for managing vendor-product catalog entries. Supports adding products (via a search dialog powered by `GetProductsForCatalogQuery`), updating last unit cost, updating notes, and soft-deleting catalog entries. |
