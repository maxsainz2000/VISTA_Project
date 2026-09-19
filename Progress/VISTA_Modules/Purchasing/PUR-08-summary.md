---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-05
plan-ref: Plans/VISTA_Modules/Purchasing/08-price-change-detection.md
status: completed
---

## Task Summary

Implemented Price Change Detection (PUR-08). When goods are received, the service compares each receipt line's unit cost against the originating PO line and creates a `PriceChangeAlert` for every discrepancy, leaving it unacknowledged until a manager reviews it.

**Plan:** `[[08-price-change-detection]]`

## What Was Done

- Created `src/MerchSys.Purchasing/Entities/PriceChangeAlert.vb` — new `AuditableEntity` with ProductId, VendorId, PreviousUnitCost, NewUnitCost, ChangePercent, ChangeDirection, GoodsReceiptId, IsAcknowledged, AcknowledgedAt
- Created `src/MerchSys.Purchasing/Services/IPriceChangeService.vb` — interface with `DetectChangesAsync`, `GetUnacknowledgedAsync`, `AcknowledgeAsync`, `GetHistoryForProductAsync`
- Created `src/MerchSys.Purchasing/Services/PriceChangeService.vb` — implementation; loads the receipt and PO with their lines, skips lines with matching cost, calculates `ChangePercent = ((New - Previous) / Previous) × 100` rounded to 4 dp, sets `ChangeDirection` to "Increase" or "Decrease", bulk-inserts alerts in a single `SaveChangesAsync` call
- Created `src/MerchSys.Purchasing/Data/Configurations/PriceChangeAlertConfiguration.vb` — table `Pur_PriceChangeAlerts`, precision(18,4) on all cost/percent columns, indexes on `IsAcknowledged` and `ProductId`
- Modified `src/MerchSys.Purchasing/Data/PurchasingDbContext.vb` — added `PriceChangeAlerts As DbSet(Of PriceChangeAlert)`
- Modified `src/MerchSys.Purchasing/Services/GoodsReceivingService.vb` — injected `IPriceChangeService`; calls `DetectChangesAsync(receipt.Id)` immediately after publishing `GoodsReceivedEvent`
- Modified `src/MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` — registered `IPriceChangeService → PriceChangeService` as scoped; registered before `IGoodsReceivingService` so the dependency chain resolves

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- `Vendor.BusinessName` does not exist — the property is `Vendor.Name`. Caught by inspection before build.

## What's Next

- PUR-09 or subsequent Purchasing plans
- EF Core migration for `Pur_PriceChangeAlerts` table (migration phase)

## Cross-References

- Domain Wiki pages consulted: `entities/module-purchasing.md`, `sources/purchasing-module-paper.md`
- Agent Wiki entries consulted: `vbnet-leading-dot-fluent-chains`, `vbnet-lambda-param-shadows-local-variable`
