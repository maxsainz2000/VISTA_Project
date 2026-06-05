---
type: layer-manifest
module: MerchSys.Purchasing
layer: Services
last-updated: 2026-05-27
---

# MerchSys.Purchasing — Services

This page details the Service implementations for the **MerchSys.Purchasing** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Services/IPurchaseOrderService.vb`<br>`src/MerchSys.Purchasing/Services/PurchaseOrderService.vb` | `IPurchaseOrderService`<br>`PurchaseOrderService` | PO Lifecycle Service. Enforces the Draft → Submitted → Received → Verified → Closed state machine. `CloseAsync` creates an `AccountsPayableEntry`; `RecalculateTotal` updates `TotalAmount` on line changes. Invalid transitions throw `InvalidOperationException`. Also defines `CreatePOLineDto`. Draft operations persist `Notes` and `ExpectedDeliveryDate`. |
| `src/MerchSys.Purchasing/Services/IGoodsReceivingService.vb`<br>`src/MerchSys.Purchasing/Services/GoodsReceivingService.vb`<br>`src/MerchSys.Purchasing/Dtos/ReceiveGoodsDto.vb` | `IGoodsReceivingService`<br>`GoodsReceivingService`<br>`ReceiveGoodsLineDto` | Records receipt of goods against a PO, handling partial quantities, expiry dates, and discrepancy notes. Creates `GoodsReceipt` records and publishes `GoodsReceivedEvent`. As of **PUR-14**, also publishes `GoodsReceivedWithVatEvent` for input-VAT accounting. |
| `src/MerchSys.Purchasing/Services/Vat/GoodsReceiptVatCalculator.vb` | `GoodsReceiptVatCalculator` | VAT breakdown calculator for goods receipts. Produces `GoodsReceiptVatBreakdown` (VatableInputs, VatExemptInputs, ZeroRatedInputs, InputVat). Currently implements "Option-2" simplification (all items treated as 12% vatable). |
| `src/MerchSys.Purchasing/Services/IVendorService.vb`<br>`src/MerchSys.Purchasing/Services/VendorService.vb` | `IVendorService`<br>`VendorService` | Vendor management implementation providing CRUD, soft delete, undo restore (UX-29), and case-insensitive search. Defines `CreateVendorDto`, `UpdateVendorDto`, and `VendorDetailDto` which aggregates purchase history. Requires `PurchasingDbContext`. |
| `src/MerchSys.Purchasing/Services/IAccountsPayableService.vb`<br>`src/MerchSys.Purchasing/Services/AccountsPayableService.vb` | `IAccountsPayableService`<br>`AccountsPayableService` | AP Tracking. Creates `AccountsPayableEntry` records from a verified `PurchaseOrder` (total derived from GR line costs). Supports partial payments via `RecordPaymentAsync` — accumulates `AmountPaid`, recalculates `Balance`, and sets `IsPaid = True` when `Balance = 0`. Rejects overpayments. Provides queries for all entries, outstanding only, by-vendor, overdue (DueDate < Today AND NOT IsPaid), and total outstanding balance. |
| `src/MerchSys.Purchasing/Services/IReorderService.vb`<br>`src/MerchSys.Purchasing/Services/ReorderService.vb` | `IReorderService`<br>`ReorderService` | Reorder Suggestion Engine. `GenerateSuggestionsAsync` queries stock levels via `GetCurrentStockQuery` (MediatR), applies optional seasonal multiplier to the reorder point, deduplicates against existing Pending suggestions, and saves `ReorderSuggestion` records. `AcceptSuggestionAsync` creates a draft `PurchaseOrder` via `SequentialNumberGenerator` and marks the suggestion Accepted. `DismissSuggestionAsync` marks a suggestion Dismissed. `UpdateConfigAsync` upserts a `ReorderConfig` by Id. `GetAllConfigsAsync` returns all configs including the `PreferredVendor` navigation. |
| `src/MerchSys.Purchasing/Services/IPriceChangeService.vb`<br>`src/MerchSys.Purchasing/Services/PriceChangeService.vb` | `IPriceChangeService`<br>`PriceChangeService` | Price Change Detection. Compares each `GoodsReceiptLine.UnitCost` against the originating `PurchaseOrderLine.UnitCost`; creates a `PriceChangeAlert` for every discrepancy with `ChangePercent` (rounded to 4 dp) and `ChangeDirection`. Called automatically at the end of `GoodsReceivingService.ReceiveGoodsAsync`. Supports querying unacknowledged alerts and per-product history. `AcknowledgeAsync` marks an alert as reviewed by the manager. |
| `src/MerchSys.Purchasing/Services/IVendorProductService.vb`<br>`src/MerchSys.Purchasing/Services/VendorProductService.vb`<br>`src/MerchSys.Purchasing/Dtos/VendorProductDto.vb` | `IVendorProductService`<br>`VendorProductService`<br>`VendorProductDto` | Vendor product catalog service providing catalog lookup and mutations. Supports checking catalog for vendor, adding catalog entries, updating catalog entries (unit cost and notes), removing catalog entries (soft-delete), undo restoring deleted entries (UX-29), and updating the last unit cost upon purchase order saves. Restricts mutations to Manager-only roles using `ISessionService`. |

## Helpers

| File Path | Class | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb` | `SequentialNumberGenerator` | Static helper with `Generate(prefix, year, existingNumbers)` to produce sequence strings like `PO-YYYY-XXXX` or `GR-YYYY-XXXX`. |

## Dependency Injection

| File Path | Class | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` | `PurchasingServiceCollectionExtensions` | Configures dependency injection bindings for Purchasing services and view models. |
