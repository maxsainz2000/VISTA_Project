---
type: layer-manifest
module: MerchSys.Purchasing
layer: Data Access
last-updated: 2026-05-29
---

# MerchSys.Purchasing — Data Access

This page details the Data Access configurations for the **MerchSys.Purchasing** module. Excludes SQLite migrations; schema bootstrapping is done centrally via `MariaDbSchemaInitializer`.

## Context

Configures the `PurchasingDbContext` with EF Core entity configurations for all Purchasing entities. Exposes DbSets.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.Purchasing/Data/PurchasingDbContext.vb` | `PurchasingDbContext` | Contains DbSets for all Purchasing entities. |
| `src/MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb` | `PurchasingDbContextFactory` | `IDesignTimeDbContextFactory(Of PurchasingDbContext)` implementation for EF CLI. |
| `src/MerchSys.Purchasing/Data/Configurations/VendorConfiguration.vb` | `VendorConfiguration` | Table: `Pur_Vendors`. Unique index on Name, cascade Restrict to PurchaseOrders. Includes `RowVersion` concurrency token. |
| `src/MerchSys.Purchasing/Data/Configurations/PurchaseOrderConfiguration.vb` | `PurchaseOrderConfiguration` | Table: `Pur_PurchaseOrders`. Unique index on OrderNumber, cascade delete to Lines and GoodsReceipts. Includes `RowVersion` concurrency token. |
| `src/MerchSys.Purchasing/Data/Configurations/PurchaseOrderLineConfiguration.vb` | `PurchaseOrderLineConfiguration` | Table: `Pur_PurchaseOrderLines`. UnitCost precision(18,4), LineTotal precision(18,2). |
| `src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptConfiguration.vb` | `GoodsReceiptConfiguration` | Table: `Pur_GoodsReceipts`. Unique index on ReceiptNumber, cascade delete to Lines. |
| `src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptLineConfiguration.vb` | `GoodsReceiptLineConfiguration` | Table: `Pur_GoodsReceiptLines`. UnitCost precision(18,4). |
| `src/MerchSys.Purchasing/Data/Configurations/AccountsPayableConfiguration.vb` | `AccountsPayableConfiguration` | Table: `Pur_AccountsPayable`. Monetary columns precision(18,2), composite index on VendorId+IsPaid. Includes `RowVersion` concurrency token. |
| `src/MerchSys.Purchasing/Data/Configurations/ReorderConfigConfiguration.vb` | `ReorderConfigConfiguration` | Table: `Pur_ReorderConfigs`. Unique index on ProductId, nullable FK PreferredVendorId → Pur_Vendors (SetNull on delete). |
| `src/MerchSys.Purchasing/Data/Configurations/ReorderSuggestionConfiguration.vb` | `ReorderSuggestionConfiguration` | Table: `Pur_ReorderSuggestions`. Composite index on (ProductId, Status). |
| `src/MerchSys.Purchasing/Data/Configurations/PriceChangeAlertConfiguration.vb` | `PriceChangeAlertConfiguration` | Table: `Pur_PriceChangeAlerts`. precision(18,4) on PreviousUnitCost, NewUnitCost, ChangePercent. Indexes on IsAcknowledged and ProductId. |
| `src/MerchSys.Purchasing/Data/Configurations/VendorProductConfiguration.vb` | `VendorProductConfiguration` | Table: `Pur_VendorProducts`. Unique composite index on `(VendorId, ProductId)` filtered to `IsDeleted = 0`. Cascade `Restrict` to Vendor. |
| `src/MerchSys.Purchasing/Data/SeedData/PurchasingSeedData.vb` | `PurchasingSeedData` | Seeds 3 sample vendors: AgriChem Supplies, FarmFresh Seeds Corp., Golden Feeds Trading. |
| `src/MerchSys.Purchasing/Data/Migrations/AddGoodsReceiptLineVatColumns.vb` | `AddGoodsReceiptLineVatColumns` | EF Core Migration adding input VAT breakdown columns to Goods Receipt lines for BIR VAT compliance. |

