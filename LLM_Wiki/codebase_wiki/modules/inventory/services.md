---
type: layer-manifest
module: MerchSys.Inventory
layer: Services
last-updated: 2026-05-04
---

# MerchSys.Inventory — Services

This page details the Service implementations for the **MerchSys.Inventory** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Inventory/Services/IStockService.vb`<br>`src/MerchSys.Inventory/Services/StockService.vb` | `IStockService`<br>`StockService` | Core FIFO deduction engine. `AddStockBatchAsync()`, `DeductStockFIFOAsync()`, `GetCurrentStockAsync()`, `GetStockBatchesAsync()`, `GetTotalValuationAsync()`. |
| `src/MerchSys.Inventory/Services/IExpiryTrackingService.vb`<br>`src/MerchSys.Inventory/Services/ExpiryTrackingService.vb` | `IExpiryTrackingService`<br>`ExpiryTrackingService` | Batch-level expiry monitoring and alerting. `GetNearExpiryBatchesAsync()`, `GetExpiredBatchesAsync()`, `GetExpiryStatusForProductAsync()`, `WriteOffExpiredBatchAsync()`. |
| `src/MerchSys.Inventory/Services/IStockDashboardService.vb`<br>`src/MerchSys.Inventory/Services/StockDashboardService.vb` | `IStockDashboardService`<br>`StockDashboardService` | Real-time stock dashboard data aggregation. `GetDashboardDataAsync()`, `GetProductDetailAsync()`. Computes FIFO valuation, low-stock, and expiry counts. |
