---
module: MerchSys.Inventory
plan-id: INV-09
title: "Stockout Estimation"
depends-on: [INV-08]
estimated-files: 2
---

# Stockout Estimation

## Context

Implements predictive stockout estimation — calculates days-until-stockout for each product based on average daily velocity. Addresses Problem I6 (demand-spike stockouts).

## Prerequisites

- **INV-08** (Velocity Classification) — average daily sales data available

## Wiki References

- `sources/inventory-module-paper.md` — "Predictive Stockout Estimation: days-until-stockout based on avg daily velocity"
- `entities/module-inventory.md` — "I6 — Demand-spike stockouts"

## Deliverables

```
MerchSys.Inventory/Services/
├── IStockoutEstimationService.vb
└── StockoutEstimationService.vb
```

## Specification

### IStockoutEstimationService
```
EstimateAllAsync() As Task(Of List(Of StockoutEstimateDto))
EstimateForProductAsync(productId As Integer) As Task(Of StockoutEstimateDto)
GetCriticalProductsAsync(daysThreshold As Integer) As Task(Of List(Of StockoutEstimateDto))
```

### StockoutEstimateDto
```
ProductId, ProductName, CurrentStock, AvgDailySales, EstimatedDaysUntilStockout, RiskLevel ("Critical", "Warning", "OK"), EstimatedStockoutDate
```

### Formula
```
DaysUntilStockout = CurrentStock / AvgDailySales
EstimatedStockoutDate = Today + DaysUntilStockout
```

### Risk Levels
- **Critical:** ≤ 7 days (or AvgDailySales = 0 and CurrentStock = 0)
- **Warning:** 8–14 days
- **OK:** > 14 days
- Products with zero sales and stock > 0 → "OK" (dead stock, won't stockout from sales)

### Critical Products Query
Returns all products where `DaysUntilStockout ≤ threshold`, sorted by urgency.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Days-until-stockout calculated correctly
3. Risk levels assigned appropriately
4. Critical products query returns correct subset
5. Handles edge cases: zero sales, zero stock

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-09-summary.md`
