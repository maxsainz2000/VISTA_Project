---
type: layer-manifest
module: MerchSys.Inventory
layer: Data Access
last-updated: 2026-05-28
---

# MerchSys.Inventory — Data Access

This page details the Data Access configurations for the **MerchSys.Inventory** module. Excludes SQLite migrations; schema bootstrapping is done centrally via `MariaDbSchemaInitializer`.

## Context

Configures the `InventoryDbContext` with EF Core entity configurations for all Inventory entities. Exposes DbSets.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.Inventory/Data/InventoryDbContext.vb` | `InventoryDbContext` | Contains DbSets for all Inventory entities. |
| `src/MerchSys.Inventory/Data/InventoryDbContextFactory.vb` | `InventoryDbContextFactory` | `IDesignTimeDbContextFactory(Of InventoryDbContext)` implementation for EF CLI. |
| `src/MerchSys.Inventory/Data/Configurations/ProductConfiguration.vb` | `ProductConfiguration` | Table: `Inv_Products`. Name max 200, Sku max 50 unique index, RetailPrice precision(18,2). Includes `RowVersion` concurrency token. |
| `src/MerchSys.Inventory/Data/Configurations/StockBatchConfiguration.vb` | `StockBatchConfiguration` | Table: `Inv_StockBatches`. UnitCost precision(18,4). Index on ProductId + ReceiptDate for FIFO. Includes `RowVersion` concurrency token. |
| `src/MerchSys.Inventory/Data/Configurations/ShrinkageRecordConfiguration.vb` | `ShrinkageRecordConfiguration` | Table: `Inv_ShrinkageRecords`. UnitCost precision(18,4), TotalValue precision(18,2), Reason max 50. |
| `src/MerchSys.Inventory/Data/Configurations/StockAlertConfigConfiguration.vb` | `StockAlertConfigConfiguration` | Table: `Inv_StockAlertConfigs`. |
| `src/MerchSys.Inventory/Data/Configurations/ProductCategoryConfiguration.vb` | `ProductCategoryConfiguration` | Table: `Inv_ProductCategories`. Name max 100, unique index. Includes `RowVersion` concurrency token. |
| `src/MerchSys.Inventory/Data/Configurations/StockMovementConfiguration.vb` | `StockMovementConfiguration` | Table: `Inv_StockMovements`. MovementType as string. Composite index: (ProductId, OccurredAt). |
| `src/MerchSys.Inventory/Data/Configurations/StockAuditRecordConfiguration.vb` | `StockAuditRecordConfiguration` | Table: `Inv_StockAuditRecords`. Variance precision(18,2), Reason max 100, Notes max 500. |
| `src/MerchSys.Inventory/Data/Configurations/ProductPriceHistoryConfiguration.vb` | `ProductPriceHistoryConfiguration` | Table: `Inv_ProductPriceHistory`. OldPrice and NewPrice precision(18,2). Composite index on `(ProductId, ChangedAt DESC)`. Cascade `Restrict` to Product. |
| `src/MerchSys.Inventory/Data/Configurations/SaleCogsRecordConfiguration.vb` | `SaleCogsRecordConfiguration` | Table: `Inv_SaleCogs`. Composite index on `(TransactionId, ProductId)` and secondary index on `(BatchId)`. UnitCost and Cogs precision `(18,4)`. Cascade `Restrict` to Batch. |
| `src/MerchSys.Inventory/Data/SeedData/InventorySeedData.vb` | `InventorySeedData` | Seeds 4 product categories (Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds) and sample products. |

