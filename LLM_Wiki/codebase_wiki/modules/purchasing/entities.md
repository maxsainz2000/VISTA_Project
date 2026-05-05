---
type: layer-manifest
module: MerchSys.Purchasing
layer: Entities
last-updated: 2026-05-05
---

# MerchSys.Purchasing — Entities

This page details the Entities for the **MerchSys.Purchasing** module.

## Files and Classes

| File Path | Class / Interface | Base / Implements | Key Members / Responsibilities |
|---|---|---|---|
| `src/MerchSys.Purchasing/Entities/Vendor.vb` | `Vendor` | `SoftDeletableEntity` | Business name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes. |
| `src/MerchSys.Purchasing/Entities/PurchaseOrder.vb` | `PurchaseOrder` | `SoftDeletableEntity` | OrderNumber, VendorId, Status, OrderDate, ExpectedDeliveryDate, TotalAmount, Notes. |
| `src/MerchSys.Purchasing/Entities/PurchaseOrderLine.vb` | `PurchaseOrderLine` | `AuditableEntity` | PurchaseOrderId, ProductId, ProductName, QuantityOrdered, UnitCost, LineTotal. |
| `src/MerchSys.Purchasing/Entities/GoodsReceipt.vb` | `GoodsReceipt` | `AuditableEntity` | PurchaseOrderId, ReceiptNumber, ReceivedDate, ReceivedBy, Notes. |
| `src/MerchSys.Purchasing/Entities/GoodsReceiptLine.vb` | `GoodsReceiptLine` | `AuditableEntity` | GoodsReceiptId, ProductId, ProductName, QuantityOrdered, QuantityReceived, UnitCost, ExpiryDate, HasDiscrepancy, DiscrepancyNotes. |
| `src/MerchSys.Purchasing/Entities/AccountsPayableEntry.vb` | `AccountsPayableEntry` | `AuditableEntity` | PurchaseOrderId, VendorId, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, AmountPaid, Balance, IsPaid, Notes. |
| `src/MerchSys.Purchasing/Entities/ReorderConfig.vb` | `ReorderConfig` | `AuditableEntity` | ProductId, ProductName, PreferredVendorId (nullable FK → Vendor), MinimumThreshold, SafetyStock, DefaultOrderQuantity, LeadTimeDays, IsSeasonalItem, SeasonalMultiplier, IsActive. |
| `src/MerchSys.Purchasing/Entities/ReorderSuggestion.vb` | `ReorderSuggestion` | `AuditableEntity` | ProductId, ProductName, CurrentStock, ReorderPoint, SuggestedQuantity, PreferredVendorId, PreferredVendorName, EstimatedLeadTimeDays, IsSeasonalAdjusted, Status ("Pending"/"Accepted"/"Dismissed"), ConvertedToPOId (nullable FK). |
| `src/MerchSys.Purchasing/Entities/PriceChangeAlert.vb` | `PriceChangeAlert` | `AuditableEntity` | ProductId, ProductName, VendorId, VendorName, PreviousUnitCost, NewUnitCost, ChangePercent (rounded to 4 dp), ChangeDirection ("Increase"/"Decrease"), GoodsReceiptId (FK to triggering receipt), IsAcknowledged, AcknowledgedAt (nullable). |
