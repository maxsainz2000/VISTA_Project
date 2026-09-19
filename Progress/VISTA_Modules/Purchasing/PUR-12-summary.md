---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-06
plan-ref: Plans/VISTA_Modules/Purchasing/12-view-ap-ledger.md
status: completed
---

## Task Summary

Implemented the AP Ledger screen (PUR-12): WPF View and ViewModel for accounts payable management.
Covers outstanding balance summary, full invoice grid with overdue highlighting, status/vendor filters,
and an inline payment recording dialog.

**Plan:** `[[12-view-ap-ledger]]`

## What Was Done

- Modified `MerchSys.Purchasing/Services/IAccountsPayableService.vb` — added `GetAllAsync()` method (needed for All and Paid filters; existing service only exposed outstanding/overdue queries)
- Modified `MerchSys.Purchasing/Services/AccountsPayableService.vb` — implemented `GetAllAsync()`: returns all AP entries with Vendor and PurchaseOrder navigation, ordered by InvoiceDate descending
- Modified `MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` — registered `IAccountsPayableService → AccountsPayableService` (Scoped) and `APLedgerViewModel` (Transient)
- Created `MerchSys.Purchasing/ViewModels/APLedgerViewModel.vb` — includes `APLedgerRow` and `VendorSelectorItem` helper classes; full filter logic (All/Outstanding/Overdue/Paid + vendor); inline payment dialog state and commands; `IsAllFilterActive` / `IsOutstandingFilterActive` / `IsOverdueFilterActive` / `IsPaidFilterActive` boolean properties for XAML DataTrigger binding without converters
- Created `MerchSys.App/Views/Purchasing/APLedgerView.xaml` — summary header with total outstanding; filter toolbar with four toggle-style status buttons (active state via DataTrigger on `IsXxxFilterActive`); vendor ComboBox; DataGrid with all plan-specified columns (VendorName, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, AmountPaid, Balance, IsPaid, IsOverdue); overdue row highlighting (amber), paid row highlighting (green); payment dialog overlay using Grid + Rectangle backdrop + centered Border card
- Created `MerchSys.App/Views/Purchasing/APLedgerView.xaml.vb` — code-behind with DI constructor injection

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A — deferred to separate testing phase |

## Issues Encountered

None. Build succeeded on first attempt.

## What's Next

- DI registration in `Application.xaml.vb` is still a TODO placeholder — `APLedgerViewModel` is registered in `PurchasingServiceCollectionExtensions` but won't be resolvable until the App wires up `AddPurchasingServices()` and `APLedgerView` is added to the navigation shell.
- Navigation integration (adding the AP Ledger tab/page to MainWindow) is outside this plan's scope.

## Cross-References

- Domain Wiki pages consulted: none
- Agent Wiki entries consulted: `[[vbnet-leading-dot-fluent-chains]]`, `[[vbnet-lambda-param-shadows-local-variable]]`
