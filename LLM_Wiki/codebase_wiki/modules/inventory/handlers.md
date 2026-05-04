---
type: layer-manifest
module: MerchSys.Inventory
layer: Handlers
last-updated: 2026-05-04
---

# MerchSys.Inventory — Handlers

This page details the MediatR Handlers for the **MerchSys.Inventory** module.

## Event Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.Inventory/Handlers/GoodsReceivedHandler.vb` | `GoodsReceivedHandler` | `GoodsReceivedEvent` | Receives incoming inventory from Purchasing. Ensures Product exists, adds new stock batches via `StockService`. |
| `src/MerchSys.Inventory/Handlers/SaleCompletedHandler.vb` | `SaleCompletedHandler` | `SaleCompletedEvent` | Deducts stock for sales using FIFO rules via `StockService.DeductStockFIFOAsync()`. |

## Request Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.Inventory/Handlers/GetCurrentStockHandler.vb` | `GetCurrentStockHandler` | `GetCurrentStockQuery` | Retrieves aggregated current stock levels via `StockService.GetCurrentStockAsync()`. |
