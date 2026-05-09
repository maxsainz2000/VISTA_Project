---
module: Integration
agent: claude-code
date: 2026-05-09
plan-ref: Plans/VISTA_Modules/Integration/08-stockmovement-writes.md
status: completed
---

## Task Summary

Implemented `StockMovement` record writes across all stock-mutating operations in the Inventory module. The `Inv_StockMovements` table (created in INT-05) was schema-only prior to this plan. After INT-08, every stock addition, deduction, shrinkage event, and return now logs a timestamped movement record within the same `SaveChangesAsync` transaction as the underlying stock change.

**Plan:** `[[08-stockmovement-writes]]`

## What Was Done

- Modified `MerchSys.Inventory/Services/IStockService.vb` — added optional `movementType As MovementType = MovementType.Receipt` parameter to `AddStockBatchAsync` interface signature
- Modified `MerchSys.Inventory/Services/StockService.vb` — `AddStockBatchAsync` now adds a `StockMovement` (positive quantity, configurable type, defaults to `Receipt`) in the same transaction as the batch; `DeductStockFIFOAsync` now adds a `StockMovement` (`Sale`, negative quantity = total deducted) before `SaveChangesAsync`
- Modified `MerchSys.Inventory/Services/ShrinkageService.vb` — `RecordShrinkageAsync` now adds a `StockMovement` (`Shrinkage`, negative quantity = total shrinkage across all affected batches) before `SaveChangesAsync`
- Modified `MerchSys.Inventory/Handlers/StockReturnedEventHandler.vb` — added `Imports MerchSys.Inventory.Entities`; passes `movementType:=MovementType.[Return]` when calling `AddStockBatchAsync`, so customer returns are logged as `Return` rather than `Receipt`

## Deliverables Status

| Deliverable | Status | Notes |
|---|---|---|
| 1. AddStockBatchAsync writes Receipt movement | ✅ Done | Positive qty, configurable type via optional param |
| 2. DeductStockFIFOAsync writes Sale movement | ✅ Done | Negative qty, logged after FIFO loop succeeds |
| 3. Shrinkage and Return movements | ✅ Done | ShrinkageService → Shrinkage; handler → Return via optional param |
| 4. VelocityService time-windowed query verification | ✅ Already implemented | VelocityService.ClassifyAllProductsAsync and GetVelocityForProductAsync already query `StockMovements` filtered by `MovementType.Sale` and `OccurredAt >= windowStart`; falls back to batch-total approximation when no movement records exist |
| 5. StockoutEstimationService accuracy verification | ✅ Already implemented | Fully delegates to VelocityService; benefits automatically from movement data |

## Quantity Sign Convention

Per `StockMovement.Quantity` documentation:
- Positive: inbound movements (`Receipt`, `Return`)
- Negative: outbound movements (`Sale`, `Shrinkage`)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings (verified via VS build) |
| Unit tests | N/A |
| Manual verification | N/A — deferred to testing phase |

## Issues Encountered

None. All changes were additive within existing service methods.

## What's Next

- [ ] Runtime verification that movement records appear in `Inv_StockMovements` after each operation type
- [ ] Verify VelocityService velocity classifications shift correctly once real movement data accumulates (vs. batch-total fallback)

## Cross-References

- Domain Wiki pages consulted: none required
- Agent Wiki entries consulted: none required
- Prior plans: INT-05 (StockMovement entity + migration), INT-07 (DI registration, prerequisite)
