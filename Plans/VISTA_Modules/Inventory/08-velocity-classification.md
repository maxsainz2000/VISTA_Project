---
module: MerchSys.Inventory
plan-id: INV-08
title: "Velocity Classification"
depends-on: [INV-03]
estimated-files: 2
---

# Velocity Classification

## Context

Implements product velocity classification — categorizing products as fast-moving, slow-moving, or dead stock based on sales history. Addresses Problem I7 (no fast/slow product view).

## Prerequisites

- **INV-03** (Stock Management) — stock deduction history for velocity calculation

## Wiki References

- `sources/inventory-module-paper.md` — "Product Velocity Classification"
- `entities/module-inventory.md` — "I7 — No fast/slow product view"

## Deliverables

```
MerchSys.Inventory/Services/
├── IVelocityService.vb
└── VelocityService.vb
```

## Specification

### IVelocityService
```
ClassifyAllProductsAsync(daysToAnalyze As Integer) As Task(Of List(Of ProductVelocityDto))
GetVelocityForProductAsync(productId As Integer, daysToAnalyze As Integer) As Task(Of ProductVelocityDto)
```

### ProductVelocityDto
```
ProductId, ProductName, Category, TotalUnitsSold, AvgDailySales, DaysAnalyzed, Classification ("Fast", "Moderate", "Slow", "Dead"), CurrentStock, DaysOfStockRemaining
```

### Classification Logic

Based on average daily sales over the analysis period (default 90 days):
- **Fast:** AvgDailySales ≥ 2.0 units/day
- **Moderate:** 0.5 ≤ AvgDailySales < 2.0
- **Slow:** 0.0 < AvgDailySales < 0.5
- **Dead:** AvgDailySales = 0 (no sales in the period)

`DaysOfStockRemaining = CurrentStock / AvgDailySales` (∞ for dead stock)

### Data Source

Calculate from `StockBatch` deduction history — track how many units were deducted per product over the analysis window. This requires either:
- A separate `StockMovement` log table (recommended — add to INV-01 if not present), OR
- Derive from `SaleCompletedEvent` handler that logs sales per product

**Recommendation:** Add a simple `StockMovement` entity to track all in/out movements with type (Received, Sold, Shrinkage) — this serves velocity, dashboard, and audit purposes.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Products classified into 4 categories correctly
3. Configurable analysis period
4. Days-of-stock-remaining calculated
5. Dead stock identified (zero sales)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-08-summary.md`
