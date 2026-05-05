---
type: layer-manifest
module: MerchSys.Purchasing
layer: Services
last-updated: 2026-05-05
---

# MerchSys.Purchasing — Services

This page details the Service implementations for the **MerchSys.Purchasing** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Services/IPurchaseOrderService.vb`<br>`src/MerchSys.Purchasing/Services/PurchaseOrderService.vb` | `IPurchaseOrderService`<br>`PurchaseOrderService` | PO Lifecycle Service. Enforces the Draft → Submitted → Received → Verified → Closed state machine. `CloseAsync` creates an `AccountsPayableEntry`; `RecalculateTotal` updates `TotalAmount` on line changes. Invalid transitions throw `InvalidOperationException`. Also defines `CreatePOLineDto`. |
| `src/MerchSys.Purchasing/Services/IGoodsReceivingService.vb`<br>`src/MerchSys.Purchasing/Services/GoodsReceivingService.vb` | `IGoodsReceivingService`<br>`GoodsReceivingService` | Records receipt of goods against a PO, handling partial quantities, expiry dates, and discrepancy notes. Creates `GoodsReceipt` records and publishes `GoodsReceivedEvent`. Defines `ReceiveGoodsLineDto`. |
| `src/MerchSys.Purchasing/Services/IVendorService.vb`<br>`src/MerchSys.Purchasing/Services/VendorService.vb` | `IVendorService`<br>`VendorService` | Vendor management implementation providing CRUD, soft delete, and case-insensitive search. Defines `CreateVendorDto`, `UpdateVendorDto`, and `VendorDetailDto` which aggregates purchase history. Requires `PurchasingDbContext`. |

## Helpers

| File Path | Class | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb` | `SequentialNumberGenerator` | Static helper with `Generate(prefix, year, existingNumbers)` to produce sequence strings like `PO-YYYY-XXXX` or `GR-YYYY-XXXX`. |
