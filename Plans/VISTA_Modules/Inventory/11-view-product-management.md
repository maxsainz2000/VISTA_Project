---
module: MerchSys.Inventory
plan-id: INV-11
title: "View — Product Management"
depends-on: [INV-02]
estimated-files: 3
---

# View — Product Management

## Context

WPF View for the product catalog — add/edit products, manage categories, set retail prices, configure alert thresholds. Manager-only screen.

## Prerequisites

- **INV-02** (Data Access) — `Product`, `ProductCategory` DbSets with seed data

## Deliverables

```
MerchSys.App/Views/Inventory/
├── ProductManagementView.xaml
└── ProductManagementView.xaml.vb

MerchSys.Inventory/ViewModels/
└── ProductManagementViewModel.vb
```

## Specification

### Product List
- DataGrid: Name, SKU, Category, RetailPrice, Unit, HasExpiry, MinThreshold, IsActive
- Search box, Category filter
- Add Product, Edit, Deactivate (soft delete) buttons

### Product Editor (dialog)
- Fields: Name, SKU, Category (dropdown), RetailPrice, Unit (dropdown), HasExpiry (checkbox), MinimumThreshold, Description
- Validation: Name required, SKU unique, Price > 0, Threshold ≥ 0

### Category Management (sub-tab)
- Simple list of categories with Add/Edit/Delete
- Default categories pre-populated from seed data

## Acceptance Criteria

1. `dotnet build` succeeds
2. Product CRUD works; SKU uniqueness enforced
3. Category management functional
4. Deactivated products excluded from main views

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-11-summary.md`
