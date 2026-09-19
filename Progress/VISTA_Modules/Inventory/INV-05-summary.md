---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/05-stock-dashboard-service.md
status: completed
---

## Task Summary

Implemented the Stock Dashboard Service (INV-05): a read-only aggregation layer that provides the real-time stock dashboard with per-product summaries, FIFO valuation, low-stock/expiry counts, and product-level batch detail.

**Plan:** `[[05-stock-dashboard-service]]`

## What Was Done

- Created `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/IStockDashboardService.vb` — interface + all DTOs (`StockDashboardDto`, `ProductSummaryDto`, `ProductDetailDto`, `StockBatchSummaryDto`, `ShrinkageSummaryDto`, `StockMovementDto`)
- Created `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockDashboardService.vb` — concrete implementation with two methods

### `GetDashboardDataAsync`
- Single EF query loading all active, non-deleted products with Category and StockBatches via `Include`
- Per-product: computes `CurrentStock` (non-expired batch sum), `StockValue` (FIFO: Σ QtyRemaining × UnitCost), `StockStatus` (Out / Low / Normal), `ExpiryStatus` (HasExpired / NearExpiry / OK)
- Aggregates: `TotalStockValue`, `LowStockCount` (products at or below threshold), `NearExpiryCount` / `ExpiredCount` (batch-level counts)
- Near-expiry window: 30 days (constant `NearExpiryDays`, matches `StockAlertConfig.ExpiryAlertDays` default)

### `GetProductDetailAsync`
- Single EF query loading the product with Category, StockBatches, and ShrinkageRecords
- Returns batch list ordered by `ReceiptDate` ascending (FIFO order)
- Returns shrinkage history ordered by `RecordedDate` descending
- Returns `RecentMovements` (last 30 days): merges batch receipts (type "Inflow") and shrinkage events (type "Shrinkage"), sorted by date descending

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `activeBatches.Count(Function(b) ...)` caused BC32016 — VB.NET resolved `.Count` as the `List(Of T)` integer property, then treated the lambda as an indexer on that integer.
  - **Resolution:** Replaced with `Enumerable.Count(activeBatches, Function(b) ...)` to bypass member resolution.
  - **Agent Wiki entry:** `[[vbnet-list-count-property-shadows-linq-extension]]`

## What's Next

- INV-06 (Inventory ViewModel) will consume `IStockDashboardService` to drive the dashboard UI
- DI registration of `IStockDashboardService` / `StockDashboardService` will be wired in `MerchSys.App` at the DI setup phase

## Cross-References

- Domain Wiki pages consulted: `[[fifo-costing]]`
- Agent Wiki entries consulted: `[[vbnet-leading-dot-fluent-chains]]`, `[[vbnet-rootnamespace-relative-declarations]]`
- Depends on: INV-03 (StockService, InventoryDbContext), INV-04 (ExpiryTrackingService)
