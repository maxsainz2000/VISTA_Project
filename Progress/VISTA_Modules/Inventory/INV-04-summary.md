---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/04-expiry-tracking.md
status: completed
---

## Task Summary

Implemented batch-level expiry date tracking for perishable products (pesticides, seeds, animal feeds). Introduces `IExpiryTrackingService` and `ExpiryTrackingService` with near-expiry alerts, expired batch identification, product-level expiry status, and write-off to shrinkage.

**Plan:** `[[04-expiry-tracking]]`

## What Was Done

- Created `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/IExpiryTrackingService.vb` — interface `IExpiryTrackingService` plus `ExpiryAlertDto` and `ProductExpiryStatusDto`
- Created `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ExpiryTrackingService.vb` — concrete implementation

## New Types

### IExpiryTrackingService (interface)
| Method | Description |
|---|---|
| `GetNearExpiryBatchesAsync(daysThreshold)` | Returns `ExpiryAlertDto` list for batches where `0 < DaysUntilExpiry ≤ threshold` |
| `GetExpiredBatchesAsync()` | Returns `ExpiryAlertDto` list for batches where `ExpiryDate < Today` |
| `GetExpiryStatusForProductAsync(productId)` | Returns `ProductExpiryStatusDto` aggregating near-expiry and expired alerts for one product |
| `WriteOffExpiredBatchAsync(batchId, reason)` | Zeroes `QuantityRemaining`, creates `ShrinkageRecord` with `Reason = "Expiry"`, returns the record |

### ExpiryAlertDto
`BatchId`, `ProductId`, `ProductName`, `QtyRemaining`, `UnitCost`, `TotalValue`, `ExpiryDate`, `DaysUntilExpiry`, `Status` ("NearExpiry" or "Expired")

### ProductExpiryStatusDto
`ProductId`, `ProductName`, `NearExpiryBatchCount`, `ExpiredBatchCount`, `TotalNearExpiryQty`, `TotalExpiredQty`, `NearExpiryValue`, `ExpiredValue`, `NearExpiryAlerts`, `ExpiredAlerts`

## Alert Logic

- `DaysUntilExpiry = (ExpiryDate.Date − DateTime.UtcNow.Date).TotalDays`
- **Near-expiry:** `ExpiryDate >= today AND ExpiryDate <= today + threshold`
- **Expired:** `ExpiryDate < today`
- Only products with `HasExpiry = True` are monitored
- `WriteOffExpiredBatchAsync` guards against writing off non-expired batches

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- INV-05 (Low Stock Alerts) — next plan in Inventory module
- DI registration of `IExpiryTrackingService` / `ExpiryTrackingService` in `MerchSys.App` (done in APP-layer wiring plan)

## Cross-References

- Domain Wiki pages consulted: `[[expiry-date-tracking]]`
- Agent Wiki entries consulted: `[[vbnet-leading-dot-fluent-chains]]`, `[[vbnet-rootnamespace-relative-declarations]]`
