---
module: MerchSys.Purchasing
plan-id: PUR-09
title: "View — PO Management"
depends-on: [PUR-03]
estimated-files: 4
---

# View — PO Management

## Context

WPF Views and ViewModels for the Purchase Order list and create/edit screens. Manager can view all POs filtered by status, create new draft POs with line items, edit drafts, and submit POs for receiving.

## Prerequisites

- **PUR-03** (PO Lifecycle Service) — `IPurchaseOrderService` fully implemented

## Wiki References

- `entities/module-purchasing.md` — user roles: Manager = full access, Owner = read-only
- `concepts/client-server-wpf.md` — WPF + MVVM + CommunityToolkit.Mvvm

## Deliverables

```
MerchSys.App/Views/Purchasing/
├── PurchaseOrderListView.xaml
└── PurchaseOrderListView.xaml.vb

MerchSys.Purchasing/ViewModels/
├── PurchaseOrderListViewModel.vb
└── PurchaseOrderEditorViewModel.vb
```

## Specification

### PO List View

- DataGrid showing all POs: OrderNumber, Vendor, Status, OrderDate, TotalAmount
- Filter/tab by status: All, Draft, Submitted, Received, Verified, Closed
- Search box: filter by OrderNumber or VendorName
- Buttons: New PO, Edit (Draft only), Submit, Delete (Draft only)
- Double-click row opens detail/edit mode

### PO Editor (in-page or dialog)

- Vendor dropdown (from VendorService)
- Line items DataGrid: Product, Qty, UnitCost, LineTotal (auto-calculated)
- Add/Remove line buttons
- Running total at bottom
- Expected delivery date picker
- Notes text area
- Save Draft / Submit buttons

### ViewModels

Use `CommunityToolkit.Mvvm`:
- `ObservableObject` base class
- `[ObservableProperty]` for bindable properties (or VB equivalent: `Partial` + `ObservableProperty`)
- `[RelayCommand]` for button commands
- Constructor injection of `IPurchaseOrderService` and `IVendorService`

### Role-Based Visibility

- **Manager:** all buttons visible
- **Owner:** read-only — hide New/Edit/Submit/Delete buttons (use a simple boolean flag for now)

## Implementation Notes

- Use `CommunityToolkit.Mvvm` source generators (VB.NET compatible attributes)
- Bind DataGrid to `ObservableCollection(Of PurchaseOrderViewModel)`
- Status filter uses a command that re-queries the service
- Line total auto-recalculates: `LineTotal = Qty × UnitCost`
- XAML should use `{Binding}` with the ViewModel as DataContext

## Acceptance Criteria

1. `dotnet build` succeeds
2. PO list displays with status filtering
3. New PO can be created with line items
4. Only Draft POs are editable
5. Submit transitions PO to Submitted status
6. Owner role hides action buttons

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-09-summary.md`
