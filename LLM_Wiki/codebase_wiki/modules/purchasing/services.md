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
| `src/MerchSys.Purchasing/Services/IAccountsPayableService.vb`<br>`src/MerchSys.Purchasing/Services/AccountsPayableService.vb` | `IAccountsPayableService`<br>`AccountsPayableService` | AP Tracking. Creates `AccountsPayableEntry` records from a verified `PurchaseOrder` (total derived from GR line costs). Supports partial payments via `RecordPaymentAsync` — accumulates `AmountPaid`, recalculates `Balance`, and sets `IsPaid = True` when `Balance = 0`. Rejects overpayments. Provides queries for all outstanding, by-vendor, overdue (DueDate < Today AND NOT IsPaid), and total outstanding balance. |
| `src/MerchSys.Purchasing/Services/IReorderService.vb`<br>`src/MerchSys.Purchasing/Services/ReorderService.vb` | `IReorderService`<br>`ReorderService` | Reorder Suggestion Engine. Queries stock levels via `GetCurrentStockQuery` (MediatR), computes effective reorder point with optional seasonal multiplier, deduplicates against existing Pending suggestions, and saves `ReorderSuggestion` records. `AcceptSuggestionAsync` creates a draft `PurchaseOrder` via `SequentialNumberGenerator`. |

## Helpers

| File Path | Class | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb` | `SequentialNumberGenerator` | Static helper with `Generate(prefix, year, existingNumbers)` to produce sequence strings like `PO-YYYY-XXXX` or `GR-YYYY-XXXX`. |
