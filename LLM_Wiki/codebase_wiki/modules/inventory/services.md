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
