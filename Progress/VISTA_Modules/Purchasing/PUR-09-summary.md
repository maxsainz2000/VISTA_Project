---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-05
plan-ref: Plans/VISTA_Modules/Purchasing/09-view-po-management.md
status: completed
---

## Task Summary

Implemented the PO Management WPF Views and ViewModels (PUR-09). Provides the Manager with a full-featured list and inline editor for Purchase Orders, including status filtering, search, and role-based button visibility for Owner read-only access.

**Plan:** `[[09-view-po-management]]`

## What Was Done

- Created `src/MerchSys.Purchasing/ViewModels/PurchaseOrderEditorViewModel.vb` — Editor VM with `POLineItem` class (auto-recalculating `LineTotal`), vendor dropdown, line items `ObservableCollection`, running total, `AddLineCommand`, `RemoveLineCommand`. PropertyChanged handlers wire/unwire on each `POLineItem` to propagate `TotalAmount` changes. `PrepareForNew` / `LoadFromPO` / `ToLineDtos` public API used by the list VM.
- Created `src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb` — List VM with `PORowItem` class, status/search filtering, `AsyncRelayCommand`s for New/Edit/Submit/Delete/SaveDraft/SubmitFromEditor/Cancel. Holds an `Editor` (PurchaseOrderEditorViewModel) instance and toggles `IsEditorOpen`. `IsManager` boolean controls role visibility.
- Created `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml` — UserControl with filter toolbar (Status ComboBox + Search TextBox + action buttons), status legend, main PO DataGrid with status-colored rows and badge column, and a bottom editor panel (gated by `IsEditorOpen`). Editor panel contains vendor ComboBox, ExpectedDeliveryDate DatePicker, Notes TextBox, editable line items DataGrid with per-row Remove button (via `RelativeSource` to UserControl DataContext), running total, and Save Draft / Submit PO / Cancel buttons.
- Created `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml.vb` — Code-behind wires `SelectionChanged` to `vm.SelectedOrder`, `MouseDoubleClick` to `EditPOCommand.ExecuteAsync`, and Escape key to clear `SearchText`.
- Modified `src/MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` — Added `IVendorService` / `VendorService` scoped registration (previously unregistered per codebase_wiki DI registry note).

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `IVendorService` was not registered in `PurchasingServiceCollectionExtensions`, but is required by `PurchaseOrderListViewModel` for the vendor dropdown.
  - **Resolution:** Added `services.AddScoped(Of IVendorService, VendorService)()` to the extensions module.

- **Note:** `IPurchaseOrderService.CreateDraftAsync` and `UpdateDraftAsync` do not accept `Notes` or `ExpectedDeliveryDate` parameters. These fields are present in the editor UI (per plan spec) and on the `PurchaseOrder` entity, but are not persisted by the current service interface. A future plan should extend the service interface to accept these optional fields.

## What's Next

- Register `PurchaseOrderListView` and `PurchaseOrderListViewModel` in the App DI container (planned for a shell/navigation plan).
- Navigate to the view from MainWindow/shell.
- Extend `IPurchaseOrderService` to persist `Notes` and `ExpectedDeliveryDate` on draft create/update.

## Cross-References

- Domain Wiki: `entities/module-purchasing.md`, `concepts/client-server-wpf.md`
- Agent Wiki consulted: `vbnet-lambda-param-shadows-local-variable`, `classlib-viewmodel-auto-refresh-timer`
