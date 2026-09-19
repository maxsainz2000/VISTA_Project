---
module: MerchSys.Inventory
plan-id: INV-10
title: "View — Stock Dashboard"
depends-on: [INV-05, INV-06, INV-09]
estimated-files: 3
---

# View — Stock Dashboard

## Context

The main Inventory screen — a real-time stock dashboard showing all products with quantities, values, stock status, expiry alerts, and stockout estimates. This is the most frequently used Inventory view.

## Prerequisites

- **INV-05** (Dashboard Service), **INV-06** (Low Stock Alerts), **INV-09** (Stockout Estimation)

## Wiki References

- `entities/module-inventory.md` — "I3 — No dashboard → Real-Time Stock Dashboard"

## Deliverables

```
MerchSys.App/Views/Inventory/
├── StockDashboardView.xaml
└── StockDashboardView.xaml.vb

MerchSys.Inventory/ViewModels/
└── StockDashboardViewModel.vb
```

## Specification

### Dashboard Layout

1. **Summary Cards (top row):**
   - Total Products | Total Stock Value (₱) | Low Stock Count | Near-Expiry Count | Critical Stockout Count

2. **Product DataGrid (main area):**
   - ProductName, Category, CurrentStock, Unit, RetailPrice, StockValue, StockStatus (color-coded), ExpiryStatus, DaysUntilStockout
   - Row colors: Red = Out of Stock, Orange = Low Stock, Yellow = Near-Expiry, Green = Normal
   - Sort by any column; default sort by StockStatus (critical first)

3. **Filter bar:**
   - Category dropdown, Status filter (All/Low/Normal/Out), Search box

4. **Auto-refresh:** Refresh data every 60 seconds or on manual refresh button

### Product Detail (on row click or drill-down)

- Batch list showing all stock batches: ReceiptDate, QtyReceived, QtyRemaining, UnitCost, ExpiryDate
- Recent movements: last 30 days inflow/outflow

## Acceptance Criteria

1. `dotnet build` succeeds
2. Dashboard displays all products with correct aggregations
3. Color-coded status indicators
4. Filters work (category, status, search)
5. Summary cards show correct counts
6. Owner role sees same dashboard (read-only)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-10-summary.md`
