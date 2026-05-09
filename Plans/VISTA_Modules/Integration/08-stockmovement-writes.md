---
module: Integration
plan-id: INT-08
title: "StockMovement Log Writes"
depends-on: [INT-07]
estimated-files: 2
---

# StockMovement Log Writes

## Context

INT-05 created the `StockMovement` entity and the `Inv_StockMovements` database table (migration `20260509100003_AddStockMovement`). However, `StockService.AddStockBatchAsync` and `DeductStockFIFOAsync` do **not** write `StockMovement` records. The table is schema-only at this time — no runtime logging of stock changes occurs.

This gap was confirmed during the INT-06 QA pass (Deliverable 6.3, status ⚠️ INCOMPLETE). Without movement records, `VelocityService` and `StockoutEstimationService` cannot perform accurate time-windowed analysis and fall back to lifetime approximations based on batch totals.

**Audit sources:** `Pending_Tasks/Inventory-audit-2026-05-09.md`, `Pending_Tasks/Integration-audit-2026-05-09.md`, `Progress/VISTA_Modules/Integration/INT-06-summary.md` (Deliverable 6.3)

## Prerequisites

- INT-07 (DI Registration Gaps) — `IStockService` must be registered in DI before `StockService` can be tested at runtime.
- INT-05 (Phase 2 Enhancements) — `StockMovement` entity and migration already exist.

## Deliverables

### 1. StockMovement Writes in `AddStockBatchAsync`

In `MerchSys.Inventory/Services/StockService.vb`, modify `AddStockBatchAsync` to create a `StockMovement` record after successfully adding a stock batch:

- `MovementType` = `Receipt`
- `ProductId` = the product receiving stock
- `Quantity` = the quantity added
- `OccurredAt` = `DateTime.UtcNow`
- Populate audit columns (`CreatedBy`, `CreatedAt`)

### 2. StockMovement Writes in `DeductStockFIFOAsync`

In `MerchSys.Inventory/Services/StockService.vb`, modify `DeductStockFIFOAsync` to create a `StockMovement` record after successfully deducting stock:

- `MovementType` = `Sale`
- `ProductId` = the product being deducted
- `Quantity` = the total quantity deducted (sum across FIFO batches)
- `OccurredAt` = `DateTime.UtcNow`
- Populate audit columns

### 3. StockMovement Writes for Shrinkage and Returns

Review and implement `StockMovement` creation in:

- `ShrinkageService.RecordShrinkageAsync` → `MovementType` = `Shrinkage`
- `StockReturnedEventHandler` → `MovementType` = `Return` (or ensure `AddStockBatchAsync` handles this when called from the handler)

### 4. VelocityService Time-Windowed Query Verification

Verify that `VelocityService` can query `StockMovement` records for time-windowed velocity classification:

- Confirm the service queries `Inv_StockMovements` filtered by `MovementType = Sale` and `OccurredAt` within the analysis window
- If the service currently uses only batch totals, update it to prefer `StockMovement` data when available

### 5. StockoutEstimationService Accuracy Verification

Verify that `StockoutEstimationService` benefits from the new movement data:

- Confirm the estimation logic uses recent movement records for calculating depletion rates
- If it falls back to batch-based approximation, update to use movement-based calculation

## Implementation Notes

- `StockMovement.MovementType` is an enum: `Sale`, `Receipt`, `Shrinkage`, `Return` (defined in INT-05).
- Write movement records within the same `SaveChangesAsync` transaction as the stock change to maintain consistency.
- Consider whether `VelocityService` should gracefully degrade when no `StockMovement` records exist (for backwards compatibility with existing stock batches that predate this change).

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — **0 errors, 0 warnings**
2. `AddStockBatchAsync` creates a `StockMovement` record with `MovementType = Receipt`
3. `DeductStockFIFOAsync` creates a `StockMovement` record with `MovementType = Sale`
4. Shrinkage and return operations create corresponding movement records
5. `VelocityService` can use time-windowed `StockMovement` data for classification
6. No regression in existing stock operations

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-08-summary.md`.
