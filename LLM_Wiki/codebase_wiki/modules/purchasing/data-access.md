---
type: layer-manifest
module: MerchSys.Purchasing
layer: Data Access
last-updated: 2026-05-09
---

# MerchSys.Purchasing — Data Access

This page details the Data Access configurations for the **MerchSys.Purchasing** module.

## Context

Configures the `PurchasingDbContext` with EF Core entity configurations for all Purchasing entities. Creates seed data for development and defines repository-style data access methods. Provides manual migration files for VB.NET support.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.Purchasing/Data/PurchasingDbContext.vb` | `PurchasingDbContext` | Contains DbSets for all Purchasing entities. |
| `src/MerchSys.Purchasing/Data/PurchasingSyncableRepository.vb` | `PurchasingSyncableRepository` | `ISyncableRepository` implementation that journals changes to `Sync_Journal` on save. |
| `src/MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb` | `PurchasingDbContextFactory` | `IDesignTimeDbContextFactory(Of PurchasingDbContext)` implementation for EF CLI. |
| `src/MerchSys.Purchasing/Data/Configurations/VendorConfiguration.vb` | `VendorConfiguration` | Table: `Pur_Vendors`. Unique index on Name, cascade Restrict to PurchaseOrders. |
| `src/MerchSys.Purchasing/Data/Configurations/PurchaseOrderConfiguration.vb` | `PurchaseOrderConfiguration` | Table: `Pur_PurchaseOrders`. Unique index on OrderNumber, cascade delete to Lines and GoodsReceipts. |
| `src/MerchSys.Purchasing/Data/Configurations/PurchaseOrderLineConfiguration.vb` | `PurchaseOrderLineConfiguration` | Table: `Pur_PurchaseOrderLines`. UnitCost precision(18,4), LineTotal precision(18,2). |
| `src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptConfiguration.vb` | `GoodsReceiptConfiguration` | Table: `Pur_GoodsReceipts`. Unique index on ReceiptNumber, cascade delete to Lines. |
| `src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptLineConfiguration.vb` | `GoodsReceiptLineConfiguration` | Table: `Pur_GoodsReceiptLines`. UnitCost precision(18,4). |
| `src/MerchSys.Purchasing/Data/Configurations/AccountsPayableConfiguration.vb` | `AccountsPayableConfiguration` | Table: `Pur_AccountsPayable`. Monetary columns precision(18,2), composite index on VendorId+IsPaid. |
| `src/MerchSys.Purchasing/Data/Configurations/ReorderConfigConfiguration.vb` | `ReorderConfigConfiguration` | Table: `Pur_ReorderConfigs`. Unique index on ProductId, nullable FK PreferredVendorId → Pur_Vendors (SetNull on delete). |
| `src/MerchSys.Purchasing/Data/Configurations/ReorderSuggestionConfiguration.vb` | `ReorderSuggestionConfiguration` | Table: `Pur_ReorderSuggestions`. Composite index on (ProductId, Status). |
| `src/MerchSys.Purchasing/Data/Configurations/PriceChangeAlertConfiguration.vb` | `PriceChangeAlertConfiguration` | Table: `Pur_PriceChangeAlerts`. precision(18,4) on PreviousUnitCost, NewUnitCost, ChangePercent. Indexes on IsAcknowledged and ProductId. |
| `src/MerchSys.Purchasing/Data/SeedData/PurchasingSeedData.vb` | `PurchasingSeedData` | Seeds 3 sample vendors: AgriChem Supplies, FarmFresh Seeds Corp., Golden Feeds Trading. |
| `src/MerchSys.Purchasing/Migrations/20260507100001_InitialPurchasing.vb` | `InitialPurchasing` | Manual EF Core migration (Sqlite) for 9 Purchasing tables and seed data. |
| `src/MerchSys.Purchasing/Migrations/PurchasingDbContextModelSnapshot.vb` | `PurchasingDbContextModelSnapshot` | EF Core model snapshot for the Purchasing module. |

