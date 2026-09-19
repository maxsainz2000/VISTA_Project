---
module: MerchSys.Inventory
plan-id: INV-13
title: "View — Shrinkage"
depends-on: [INV-07]
estimated-files: 3
---

# View — Shrinkage

## Context

WPF View for recording and viewing inventory shrinkage — damage, spoilage, admin discrepancies. Shows history with financial impact.

## Prerequisites

- **INV-07** (Shrinkage Recording Service)

## Deliverables

```
MerchSys.App/Views/Inventory/
├── ShrinkageView.xaml
└── ShrinkageView.xaml.vb

MerchSys.Inventory/ViewModels/
└── ShrinkageViewModel.vb
```

## Specification

### Screen Layout

1. **Record Shrinkage button** — opens dialog
2. **Shrinkage History DataGrid:** Date, Product, Quantity, UnitCost, TotalValue, Reason, Notes, RecordedBy
3. **Filters:** Date range, Product, Reason type
4. **Summary:** Total shrinkage value for selected period

### Record Dialog
- Product dropdown, Quantity (integer), Reason dropdown (Damage/Spoilage/Expiry/Admin Error), Notes (text), optional: specific batch selection
- Validation: Qty > 0, Qty ≤ available stock
- Confirmation before saving

## Acceptance Criteria

1. `dotnet build` succeeds
2. Shrinkage recorded with correct financial impact
3. History filterable by date, product, reason
4. Period total calculated correctly

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-13-summary.md`
