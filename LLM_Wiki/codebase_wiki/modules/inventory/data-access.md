---
type: layer-manifest
module: MerchSys.Inventory
layer: Data Access
last-updated: 2026-05-09
---

# MerchSys.Inventory — Data Access

This page details the Data Access configurations for the **MerchSys.Inventory** module.

## Context

Configures the `InventoryDbContext` with EF Core entity configurations for all Inventory entities, and provides seed data for product categories and sample products. Provides manual migration files for VB.NET support.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.Inventory/Data/InventoryDbContext.vb` | `InventoryDbContext` | Contains DbSets: `Products`, `StockBatches`, `ShrinkageRecords`, `StockAlertConfigs`, `ProductCategories`, `StockMovements`. |
| `src/MerchSys.Inventory/Data/InventoryDbContextFactory.vb` | `InventoryDbContextFactory` | `IDesignTimeDbContextFactory(Of InventoryDbContext)` implementation for EF CLI design-time support. |
| `src/MerchSys.Inventory/Data/Configurations/ProductConfiguration.vb` | `ProductConfiguration` | Table: `Inv_Products`. Name max 200, Sku max 50 unique index, RetailPrice precision(18,2). |
| `src/MerchSys.Inventory/Data/Configurations/StockBatchConfiguration.vb` | `StockBatchConfiguration` | Table: `Inv_StockBatches`. UnitCost precision(18,4). Index on ProductId + ReceiptDate for FIFO. |
| `src/MerchSys.Inventory/Data/Configurations/ShrinkageRecordConfiguration.vb` | `ShrinkageRecordConfiguration` | Table: `Inv_ShrinkageRecords`. UnitCost precision(18,4), TotalValue precision(18,2), Reason max 50. |
| `src/MerchSys.Inventory/Data/Configurations/StockAlertConfigConfiguration.vb` | `StockAlertConfigConfiguration` | Table: `Inv_StockAlertConfigs`. |
| `src/MerchSys.Inventory/Data/Configurations/ProductCategoryConfiguration.vb` | `ProductCategoryConfiguration` | Table: `Inv_ProductCategories`. Name max 100, unique index. |
| `src/MerchSys.Inventory/Data/SeedData/InventorySeedData.vb` | `InventorySeedData` | Seeds 4 product categories (Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds) and sample products. |
| `src/MerchSys.Inventory/Migrations/20260507100002_InitialInventory.vb` | `InitialInventory` | Manual EF Core migration (Sqlite) for 5 Inventory tables and seed data. |
| `src/MerchSys.Inventory/Migrations/InventoryDbContextModelSnapshot.vb` | `InventoryDbContextModelSnapshot` | EF Core model snapshot for the Inventory module. |
