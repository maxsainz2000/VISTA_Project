---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/08-velocity-classification.md
status: completed
---

## Task Summary

Implements product velocity classification for the Inventory module. Products are categorised as Fast, Moderate, Slow, or Dead based on average daily sales derived from StockBatch deduction history.

**Plan:** `[[08-velocity-classification]]`

## What Was Done

- Created `src/MerchSys.Inventory/Services/IVelocityService.vb` — `IVelocityService` interface and `ProductVelocityDto`
- Created `src/MerchSys.Inventory/Services/VelocityService.vb` — `VelocityService` implementation

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Design Notes

**Data source approximation:** `StockBatch` records do not carry per-deduction timestamps (no `StockMovement` log exists yet). Velocity is therefore computed from lifetime totals:

- `TotalDeducted` = Σ(`QuantityReceived − QuantityRemaining`) across all batches
- `TotalShrinkage` = Σ(`QuantityLost`) from `ShrinkageRecord`
- `TotalUnitsSold` = `max(0, TotalDeducted − TotalShrinkage)`
- `AvgDailySales` = `TotalUnitsSold / daysToAnalyze`

`daysToAnalyze` acts as the denominator to yield a daily rate; the analysis window does not filter batch-level records (no timestamps available). A future `StockMovement` log (recommended in the plan) would enable proper time-windowed queries.

**Classification thresholds:**

| Label | Condition |
|---|---|
| Fast | AvgDailySales ≥ 2.0 |
| Moderate | 0.5 ≤ AvgDailySales < 2.0 |
| Slow | 0 < AvgDailySales < 0.5 |
| Dead | AvgDailySales = 0 |

`DaysOfStockRemaining` is `Nothing` (infinite) for Dead stock; otherwise `CurrentStock / AvgDailySales`.

**DI:** `IVelocityService` / `VelocityService` must be registered as `Scoped` in `MerchSys.App` when the composition root is wired up (INFRA-02+).

## Issues Encountered

None.

## What's Next

- [x] Register `IVelocityService` → `VelocityService` (Scoped) in the App composition root *(completed — registered in INT-01)*
- [x] Add a `StockMovement` log entity to enable precise time-windowed velocity queries *(completed — created in INT-05)*

## Cross-References

- Agent Wiki entries consulted: `[[vbnet-list-count-property-shadows-linq-extension]]`
