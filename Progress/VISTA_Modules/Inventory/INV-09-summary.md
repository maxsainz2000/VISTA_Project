---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/09-stockout-estimation.md
status: completed
---

## Task Summary

Implemented predictive stockout estimation for the Inventory module. Calculates days-until-stockout per product using average daily velocity, assigns risk levels, and exposes a critical-products query.

**Plan:** `[[09-stockout-estimation]]`

## What Was Done

- Created `src/MerchSys.Inventory/Services/IStockoutEstimationService.vb` — interface (`EstimateAllAsync`, `EstimateForProductAsync`, `GetCriticalProductsAsync`) and `StockoutEstimateDto` (ProductId, ProductName, CurrentStock, AvgDailySales, EstimatedDaysUntilStockout, RiskLevel, EstimatedStockoutDate)
- Created `src/MerchSys.Inventory/Services/StockoutEstimationService.vb` — implementation delegating velocity calculation to `IVelocityService` with a 30-day default analysis window; maps `ProductVelocityDto` to `StockoutEstimateDto` with risk classification and stockout date projection

## Design Notes

- **Reuses `IVelocityService`** (same module) rather than duplicating the stock/sales calculation; avoids redundancy and keeps AvgDailySales consistent between the two services.
- **`EstimatedDaysUntilStockout` is `Decimal?`** — `Nothing` for dead-stock products (AvgDailySales=0, CurrentStock>0), indicating infinite runway; `0` for zero-stock/zero-sales (already out).
- **`GetCriticalProductsAsync`** filters on `EstimatedDaysUntilStockout.HasValue AndAlso days ≤ threshold`, so dead stock (Nothing) is excluded. Results sorted ascending by days (most urgent first).

## Edge Cases Handled

| Scenario | DaysUntilStockout | RiskLevel | StockoutDate |
|---|---|---|---|
| AvgDailySales = 0, CurrentStock = 0 | 0 | Critical | Today |
| AvgDailySales = 0, CurrentStock > 0 | Nothing | OK | Nothing |
| AvgDailySales > 0, days ≤ 7 | Computed | Critical | Today + days |
| AvgDailySales > 0, days 8–14 | Computed | Warning | Today + days |
| AvgDailySales > 0, days > 14 | Computed | OK | Today + days |

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- INV-10 (next Inventory plan per dependency order)
- DI registration for `IStockoutEstimationService` / `StockoutEstimationService` in `MerchSys.App` (deferred to INFRA-02 pattern)

## Cross-References

- Domain Wiki pages consulted: `[[inventory-module-paper]]`, `[[module-inventory]]`
- Codebase Wiki consulted: `[[inventory/services]]`, `[[inventory/index]]`
- Depends on: INV-08 (`IVelocityService`, `ProductVelocityDto`)
