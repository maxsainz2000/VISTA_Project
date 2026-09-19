---
module: MerchSys.Purchasing
plan-id: PUR-01
title: "Purchasing Domain Models"
depends-on: [INFRA-02]
estimated-files: 6
---

# Purchasing Domain Models

## Context

Defines the core entities for the Purchasing module: Vendor, PurchaseOrder, PurchaseOrderLine, GoodsReceipt, GoodsReceiptLine, and AccountsPayableEntry. These entities model the upstream supply chain from vendor management through goods receipt and AP payment.

## Prerequisites

- **INFRA-02** (Shared Kernel) — `SoftDeletableEntity`, `PurchaseOrderStatus` enum

## Wiki References

- `entities/module-purchasing.md` — P1–P6 problems, data flow, user roles
- `sources/purchasing-module-paper.md` — vendor directory, PO lifecycle, goods receiving, AP tracking
- `concepts/fifo-costing.md` — batch-level cost recording at receipt
- `concepts/reorder-suggestion-engine.md` — reorder point formula, inputs

## Deliverables

Create in `src/MerchSys.Purchasing/Entities/`:

```
Entities/
├── Vendor.vb
├── PurchaseOrder.vb
├── PurchaseOrderLine.vb
├── GoodsReceipt.vb
├── GoodsReceiptLine.vb
└── AccountsPayableEntry.vb
```

## Specification

### Vendor
```
Inherits SoftDeletableEntity

Property Name As String                     ' Business name
Property ContactPerson As String            ' Primary contact
Property Phone As String                    ' Contact number
Property Email As String                    ' Nullable
Property Address As String                  ' Full address
Property DefaultLeadTimeDays As Integer     ' Average days from PO to delivery
Property Notes As String                    ' Nullable — free-text notes

' Navigation
Property PurchaseOrders As ICollection(Of PurchaseOrder)
```

### PurchaseOrder
```
Inherits SoftDeletableEntity

Property OrderNumber As String              ' Auto-generated: PO-YYYY-XXXX
Property VendorId As Integer                ' FK to Vendor
Property Status As PurchaseOrderStatus      ' Draft, Submitted, Received, Verified, Closed
Property OrderDate As DateTime              ' When PO was created
Property ExpectedDeliveryDate As DateTime?  ' Nullable — estimated arrival
Property TotalAmount As Decimal             ' Sum of line amounts
Property Notes As String                    ' Nullable

' Navigation
Property Vendor As Vendor
Property Lines As ICollection(Of PurchaseOrderLine)
Property GoodsReceipts As ICollection(Of GoodsReceipt)
```

### PurchaseOrderLine
```
Inherits AuditableEntity

Property PurchaseOrderId As Integer         ' FK to PurchaseOrder
Property ProductId As Integer               ' FK to Inventory product (cross-module ID only)
Property ProductName As String              ' Denormalized — for display without cross-module query
Property QuantityOrdered As Integer
Property UnitCost As Decimal                ' Agreed purchase price
Property LineTotal As Decimal               ' Qty × UnitCost

' Navigation
Property PurchaseOrder As PurchaseOrder
```

### GoodsReceipt
```
Inherits AuditableEntity

Property PurchaseOrderId As Integer         ' FK to PurchaseOrder
Property ReceiptNumber As String            ' Auto-generated: GR-YYYY-XXXX
Property ReceivedDate As DateTime           ' When goods were physically received
Property ReceivedBy As String               ' Who received
Property Notes As String                    ' Nullable — discrepancy notes

' Navigation
Property PurchaseOrder As PurchaseOrder
Property Lines As ICollection(Of GoodsReceiptLine)
```

### GoodsReceiptLine
```
Inherits AuditableEntity

Property GoodsReceiptId As Integer          ' FK to GoodsReceipt
Property ProductId As Integer               ' FK to Inventory product
Property ProductName As String              ' Denormalized
Property QuantityOrdered As Integer         ' From PO line
Property QuantityReceived As Integer        ' Actually received
Property UnitCost As Decimal                ' Actual cost at receipt (may differ from PO)
Property ExpiryDate As DateTime?            ' Nullable — for perishable products only
Property HasDiscrepancy As Boolean          ' True if received ≠ ordered
Property DiscrepancyNotes As String         ' Nullable — explain difference

' Navigation
Property GoodsReceipt As GoodsReceipt
```

### AccountsPayableEntry
```
Inherits AuditableEntity

Property PurchaseOrderId As Integer         ' FK to PurchaseOrder
Property VendorId As Integer                ' FK to Vendor
Property InvoiceNumber As String            ' Supplier invoice reference
Property InvoiceDate As DateTime
Property DueDate As DateTime                ' Payment due date
Property TotalAmount As Decimal             ' Amount owed
Property AmountPaid As Decimal              ' Running total paid
Property Balance As Decimal                 ' TotalAmount - AmountPaid (computed or stored)
Property IsPaid As Boolean                  ' True when Balance = 0
Property Notes As String                    ' Nullable

' Navigation
Property PurchaseOrder As PurchaseOrder
Property Vendor As Vendor
```

## Implementation Notes

- `ProductId` references are **integer IDs only** — no EF navigation to Inventory entities (cross-module boundary)
- `ProductName` is denormalized to avoid cross-module queries for display purposes
- `OrderNumber` and `ReceiptNumber` use a sequential pattern (PO-YYYY-XXXX, GR-YYYY-XXXX) — generation logic will be in the service layer (Plan PUR-03)
- `PurchaseOrderLine` inherits from `AuditableEntity` (not `SoftDeletableEntity`) because lines are deleted when the PO is deleted
- Use `Decimal` for all monetary values
- All `DateTime` values are UTC

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors
2. All 6 entity files exist with correct namespaces (`MerchSys.Purchasing.Entities`)
3. Inheritance hierarchy is correct (each entity inherits from appropriate base)
4. No references to other module's entity types
5. XML doc comments on every public property

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-01-summary.md`

### Documentation
- XML doc comments on every entity and property
