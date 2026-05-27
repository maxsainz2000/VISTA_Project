---
type: layer-manifest
module: MerchSys.Inventory
layer: Handlers
last-updated: 2026-05-27
---

# MerchSys.Inventory — Handlers

This page details the MediatR Handlers for the **MerchSys.Inventory** module.

## Event Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.Inventory/Handlers/GoodsReceivedHandler.vb` | `GoodsReceivedHandler` | `GoodsReceivedEvent` | Receives incoming inventory from Purchasing. Ensures Product exists, adds new stock batches via `StockService`. |
| `src/MerchSys.Inventory/Handlers/SaleCompletedHandler.vb` | `SaleCompletedHandler` | `SaleCompletedEvent` | Deducts stock for sales using FIFO rules via `StockService.DeductStockFIFOAsync()`. Persists per-batch FIFO breakdown to `Inv_SaleCogs` with idempotency guards. Triggers low-stock alert generation. |
| `src/MerchSys.Inventory/Handlers/StockReturnedEventHandler.vb` | `StockReturnedEventHandler` | `StockReturnedEvent` | Adds returned items back to FIFO stock pool via `StockService.AddStockBatchAsync()`; logs as `Return` movement type. |
| `src/MerchSys.Inventory/Handlers/ShrinkageRecordedHandler.vb` | `ShrinkageRecordedHandler` | `ShrinkageRecordedEvent` | Triggers low-stock alert generation after stock reduction by shrinkage. |

## Request Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.Inventory/Handlers/GetCurrentStockHandler.vb` | `GetCurrentStockHandler` | `GetCurrentStockQuery` | Retrieves aggregated current stock levels via `StockService.GetCurrentStockAsync()`. |
| `src/MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb` | `GetProductCatalogQueryHandler` | `GetProductCatalogQuery` | Returns active products with stock levels for POS search; filters by SearchTerm/ProductId. |
| `src/MerchSys.Inventory/Handlers/GetProductCostQueryHandler.vb` | `GetProductCostQueryHandler` | `GetProductCostQuery` | Returns current FIFO unit cost (oldest available non-expired batch) for a product. |
| `src/MerchSys.Inventory/Handlers/GetSaleCogsBreakdownQueryHandler.vb` | `GetSaleCogsBreakdownQueryHandler` | `GetSaleCogsBreakdownQuery` | Retrieves per-batch FIFO COGS breakdown from `Inv_SaleCogs` for a specific POS transaction and product; projects to DTO. |
| `src/MerchSys.Inventory/Handlers/GetLowStockAlertCountQueryHandler.vb` | `GetLowStockAlertCountQueryHandler` | `GetLowStockAlertCountQuery` | Returns count of active low-stock alerts via `LowStockAlertService`. |
| `src/MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb` | `GetProductsForCatalogQueryHandler` | `GetProductsForCatalogQuery` | Handles query for populating vendor catalog selection list by querying active products using a raw SQLite connection. |

