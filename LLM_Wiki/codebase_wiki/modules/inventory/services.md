---
type: layer-manifest
module: MerchSys.Inventory
layer: Services
last-updated: 2026-05-09
---

# MerchSys.Inventory — Services

This page details the Service implementations for the **MerchSys.Inventory** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Inventory/Services/IStockService.vb`<br>`src/MerchSys.Inventory/Services/StockService.vb` | `IStockService`<br>`StockService` | Core FIFO deduction engine. `AddStockBatchAsync()`, `DeductStockFIFOAsync()`. Now writes `StockMovement` records (Receipt, Sale) for all stock changes. |
| `src/MerchSys.Inventory/Services/IExpiryTrackingService.vb`<br>`src/MerchSys.Inventory/Services/ExpiryTrackingService.vb` | `IExpiryTrackingService`<br>`ExpiryTrackingService` | Batch-level expiry monitoring and alerting. `GetNearExpiryBatchesAsync()`, `GetExpiredBatchesAsync()`, `GetExpiryStatusForProductAsync()`, `WriteOffExpiredBatchAsync()`. |
| `src/MerchSys.Inventory/Services/IStockDashboardService.vb`<br>`src/MerchSys.Inventory/Services/StockDashboardService.vb` | `IStockDashboardService`<br>`StockDashboardService` | Real-time stock dashboard data aggregation. `GetDashboardDataAsync()`, `GetProductDetailAsync()`. Computes FIFO valuation, low-stock, and expiry counts. |
| `src/MerchSys.Inventory/Services/ILowStockAlertService.vb`<br>`src/MerchSys.Inventory/Services/LowStockAlertService.vb` | `ILowStockAlertService`<br>`LowStockAlertService` | Threshold-based low-stock alerting and WPF toast notifications via `ILowStockNotifier`. `CheckAndGenerateAlertsAsync()`, `BuildAlertsAsync()`, `UpdateThresholdAsync()`. |
| `src/MerchSys.Inventory/Services/ILowStockNotifier.vb` | `ILowStockNotifier` | Interface for low-stock notifications. Implemented in the App layer (Composition Root) to handle UI-specific alerts (e.g., WPF toast). |
| `src/MerchSys.Inventory/Services/IShrinkageService.vb`<br>`src/MerchSys.Inventory/Services/ShrinkageService.vb` | `IShrinkageService`<br>`ShrinkageService` | Inventory shrinkage recording (damage, spoilage). `RecordShrinkageAsync()`. Now writes `StockMovement` records (Shrinkage) for all events. |
| `src/MerchSys.Inventory/Services/IVelocityService.vb`<br>`src/MerchSys.Inventory/Services/VelocityService.vb` | `IVelocityService`<br>`VelocityService` | Product velocity classification (fast, slow, dead). Now utilizes `StockMovement` logging (implemented in INT-08) for accurate classification. |
| `src/MerchSys.Inventory/Services/IStockoutEstimationService.vb`<br>`src/MerchSys.Inventory/Services/StockoutEstimationService.vb` | `IStockoutEstimationService`<br>`StockoutEstimationService` | Predictive stockout estimation. Now utilizes `StockMovement` logging (implemented in INT-08) for accurate estimation. |
