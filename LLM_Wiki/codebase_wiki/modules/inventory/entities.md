---
type: layer-manifest
module: MerchSys.Inventory
layer: Entities
last-updated: 2026-05-09
---

# MerchSys.Inventory — Entities

This page details the Entities for the **MerchSys.Inventory** module.

## Files and Classes

| File Path | Class / Interface | Base / Implements | Key Members / Responsibilities |
|---|---|---|---|
| `src/MerchSys.Inventory/Entities/MovementType.vb` | `MovementType` | `Enum` | Types: `Sale`, `Receipt`, `Shrinkage`, `[Return]`. |
| `src/MerchSys.Inventory/Entities/Product.vb` | `Product` | `SoftDeletableEntity` | Catalog entry: `Sku`, `RetailPrice`, `Unit`, `HasExpiry`, `MinimumThreshold`. Computed: `CurrentStock`, `TotalValue`. |
| `src/MerchSys.Inventory/Entities/ProductCategory.vb` | `ProductCategory` | `SoftDeletableEntity` | Groups products: `Name`, `Description`, `Products`. |
| `src/MerchSys.Inventory/Entities/ShrinkageRecord.vb` | `ShrinkageRecord` | `AuditableEntity` | Financial impact: `Reason`, `QuantityLost`, `UnitCost`, `TotalValue`. |
| `src/MerchSys.Inventory/Entities/StockAlertConfig.vb` | `StockAlertConfig` | `AuditableEntity` | Alert config: `MinimumThreshold`, `ExpiryAlertDays`, `IsAlertEnabled`. |
| `src/MerchSys.Inventory/Entities/StockBatch.vb` | `StockBatch` | `AuditableEntity` | FIFO core: `QuantityReceived`, `QuantityRemaining`, `UnitCost`, `ReceiptDate`, `ExpiryDate`, `IsExpired`, `IsFullyConsumed`. |
| `src/MerchSys.Inventory/Entities/StockMovement.vb` | `StockMovement` | `AuditableEntity` | Change log: `ProductId`, `MovementType`, `Quantity`, `OccurredAt`. Enables windowed velocity queries. |
