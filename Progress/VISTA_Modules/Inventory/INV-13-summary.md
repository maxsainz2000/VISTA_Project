---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/13-view-shrinkage.md
status: completed
---

## Task Summary

Implemented the Shrinkage View — a WPF screen for recording inventory losses and viewing their history with financial impact. Depends on INV-07 (ShrinkageService, IStockService).

**Plan:** `[[13-view-shrinkage]]`

## What Was Done

- Created `MerchSys.Inventory/ViewModels/ShrinkageViewModel.vb` — ViewModel with three helper classes (`ShrinkageRowItem`, `ShrinkageProductItem`, `ShrinkageBatchItem`) and the main `ShrinkageViewModel`
- Created `MerchSys.App/Views/Inventory/ShrinkageView.xaml` — UserControl with summary cards, filter toolbar, history DataGrid, and MVVM overlay dialog
- Created `MerchSys.App/Views/Inventory/ShrinkageView.xaml.vb` — Code-behind with DI constructor injection, DatePicker sync, reason filter handler, dialog confirmation/validation, and quantity input guard

## Architecture Notes

**ViewModel (`ShrinkageViewModel`):**
- Depends on `IShrinkageService` and `IStockService` (both from INV-07)
- Loads full history via `GetShrinkageHistoryAsync()` and filters client-side by date range, product, and reason
- `ApplyFilters()` recomputes `PeriodTotalValue` and `FilteredCount` on every filter change
- Dialog state is managed entirely on the VM (`IsDialogOpen`, `DialogSelectedProduct`, `DialogQuantity`, `DialogReason`, `DialogNotes`, `DialogSelectedBatch`, `DialogAvailableStock`)
- Selecting a product in the dialog triggers `LoadDialogBatchesAsync()` to populate optional FIFO batch list via `IStockService.GetStockBatchesAsync()`
- `ValidateDialog()` is called by the View's code-behind before showing the confirm MessageBox
- Does **not** use a timer (no auto-refresh needed; user-driven Refresh button)

**View (`ShrinkageView.xaml`):**
- Root `Grid` wraps a `DockPanel` (main content) and an overlay `Grid` (dialog), enabling the dialog to dim the background
- Three summary cards: Period Shrinkage Value, Records (Filtered), Last Refreshed
- Toolbar: Record Shrinkage button (red), From/To DatePickers, Product ComboBox (bound to `FilterProducts`), Reason ComboBox (inline `ComboBoxItem`s with Tag-based selection), status message, Refresh
- DataGrid history columns: Date, Product, Qty, Unit Cost, Total Value, Reason (color-coded by DataTrigger), Notes, Recorded By
- Dialog overlay: Product ComboBox (`DialogProducts`, stock shown in DisplayText), Available Stock label, Quantity TextBox, Reason ComboBox (bound to `ReasonOptions` string array with `SelectedItem`), Notes TextBox, Batch ComboBox (`DialogBatches`), Cancel/Confirm buttons

**Code-behind (`ShrinkageView.xaml.vb`):**
- DatePickers cannot bind directly to `DateTime` (non-nullable); `SelectedDateChanged` handlers sync to VM
- Reason filter ComboBox uses inline `ComboBoxItem`s with `Tag` — handler extracts `Tag.ToString()` to set `FilterReason`
- Confirm button calls `vm.ValidateDialog()` first, then shows `MessageBox.YesNo`, then invokes `ExecuteRecordCommand` via `Dispatcher.InvokeAsync`
- `QuantityBox_PreviewTextInput` restricts input to digits only

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None. Build succeeded with 0 errors, 0 warnings on first attempt.

## What's Next

- Wire `ShrinkageView` and `ShrinkageViewModel` into the DI container and main navigation (deferred to a later integration plan)
- INV-14 or subsequent view plans

## Cross-References

- Domain Wiki pages consulted: none required beyond existing plan
- Agent Wiki entries consulted: `[[vbnet-list-count-property-shadows-linq-extension]]`, `[[vbnet-leading-dot-fluent-chains]]`, `[[classlib-viewmodel-auto-refresh-timer]]`
