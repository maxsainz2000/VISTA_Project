---
type: layer-manifest
module: MerchSys.Purchasing
layer: Entities
last-updated: 2026-05-04
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
