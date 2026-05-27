---
type: layer-manifest
module: MerchSys.Inventory
layer: Data Access
last-updated: 2026-05-27
---

# MerchSys.Inventory — Data Access

This page details the Data Access configurations for the **MerchSys.Inventory** module.

## Context

Configures the `InventoryDbContext` with EF Core entity configurations for all Inventory entities, and provides seed data for product categories and sample products. Provides manual migration files for VB.NET support.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.Inventory/Data/InventoryDbContext.vb` | `InventoryDbContext` | Contains DbSets for all Inventory entities. |
| `src/MerchSys.Inventory/Data/InventorySyncableRepository.vb` | `InventorySyncableRepository` | `ISyncableRepository` implementation that journals changes to `Sync_Journal` on save. |
| `src/MerchSys.Inventory/Data/InventoryDbContextFactory.vb` | `InventoryDbContextFactory` | `IDesignTimeDbContextFactory(Of InventoryDbContext)` implementation for EF CLI. |
| `src/MerchSys.Inventory/Data/Configurations/ProductConfiguration.vb` | `ProductConfiguration` | Table: `Inv_Products`. Name max 200, Sku max 50 unique index, RetailPrice precision(18,2). |
| `src/MerchSys.Inventory/Data/Configurations/StockBatchConfiguration.vb` | `StockBatchConfiguration` | Table: `Inv_StockBatches`. UnitCost precision(18,4). Index on ProductId + ReceiptDate for FIFO. |
| `src/MerchSys.Inventory/Data/Configurations/ShrinkageRecordConfiguration.vb` | `ShrinkageRecordConfiguration` | Table: `Inv_ShrinkageRecords`. UnitCost precision(18,4), TotalValue precision(18,2), Reason max 50. |
| `src/MerchSys.Inventory/Data/Configurations/StockAlertConfigConfiguration.vb` | `StockAlertConfigConfiguration` | Table: `Inv_StockAlertConfigs`. |
| `src/MerchSys.Inventory/Data/Configurations/ProductCategoryConfiguration.vb` | `ProductCategoryConfiguration` | Table: `Inv_ProductCategories`. Name max 100, unique index. |
| `src/MerchSys.Inventory/Data/Configurations/StockMovementConfiguration.vb` | `StockMovementConfiguration` | Table: `Inv_StockMovements`. MovementType as string. Composite index: (ProductId, OccurredAt). |
| `src/MerchSys.Inventory/Data/Configurations/StockAuditRecordConfiguration.vb` | `StockAuditRecordConfiguration` | Table: `Inv_StockAuditRecords`. Variance precision(18,2), Reason max 100, Notes max 500. |
| `src/MerchSys.Inventory/Data/Configurations/ProductPriceHistoryConfiguration.vb` | `ProductPriceHistoryConfiguration` | Table: `Inv_ProductPriceHistory`. OldPrice and NewPrice precision(18,2). Composite index on `(ProductId, ChangedAt DESC)`. Cascade `Restrict` to Product. |
| `src/MerchSys.Inventory/Data/SeedData/InventorySeedData.vb` | `InventorySeedData` | Seeds 4 product categories (Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds) and sample products. |
| `src/MerchSys.Inventory/Migrations/20260507100002_InitialInventory.vb` | `InitialInventory` | Manual EF Core migration (Sqlite) for 5 Inventory tables and seed data. |
| `src/MerchSys.Inventory/Migrations/20260509100003_AddStockMovement.vb` | `AddStockMovement` | Manual EF Core migration creating `Inv_StockMovements` table. |
| `src/MerchSys.Inventory/Migrations/20260527100000_AddProductPriceHistory.vb` | `AddProductPriceHistory` | Manual EF Core migration establishing the `Inv_ProductPriceHistory` table and index on application startup. |
| `src/MerchSys.Inventory/Migrations/InventoryDbContextModelSnapshot.vb` | `InventoryDbContextModelSnapshot` | EF Core model snapshot for the Inventory module. |

