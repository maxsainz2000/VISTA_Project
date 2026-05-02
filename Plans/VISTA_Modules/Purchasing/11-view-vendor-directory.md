---
module: MerchSys.Purchasing
plan-id: PUR-11
title: "View — Vendor Directory"
depends-on: [PUR-05]
estimated-files: 4
---

# View — Vendor Directory

## Context

WPF View and ViewModel for vendor management — list all vendors, search, create/edit vendor details, view purchase history per vendor.

## Prerequisites

- **PUR-05** (Vendor Directory Service) — `IVendorService` implemented

## Deliverables

```
MerchSys.App/Views/Purchasing/
├── VendorDirectoryView.xaml
└── VendorDirectoryView.xaml.vb

MerchSys.Purchasing/ViewModels/
├── VendorListViewModel.vb
└── VendorEditorViewModel.vb
```

## Specification

### Vendor List

- DataGrid: Name, ContactPerson, Phone, Email, DefaultLeadTimeDays
- Search box with real-time filtering
- Buttons: Add Vendor, Edit, Delete (soft)
- Clicking a vendor shows detail panel with purchase history summary

### Vendor Editor (dialog or side panel)

- Form fields: Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes
- Validation: Name required + unique, Phone required, LeadTimeDays > 0
- Save / Cancel buttons

### Vendor Detail Panel

- Purchase history: total POs, total spent, last order date, average actual lead time
- Recent POs DataGrid (last 10): OrderNumber, Date, Status, Total

## Acceptance Criteria

1. `dotnet build` succeeds
2. Vendor list with search works
3. Add/edit/delete vendors
4. Duplicate name validation shows error
5. Purchase history displays correctly

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-11-summary.md`
