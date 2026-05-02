---
module: MerchSys.Inventory
plan-id: INV-01
title: "Inventory Domain Models"
depends-on: [INFRA-02]
estimated-files: 5
---

# Inventory Domain Models

## Context

Defines the core entities for the Inventory module — the **central hub** of VISTA. Manages product catalog, stock batches (FIFO), expiry tracking, shrinkage records, and stock alert configurations. Receives stock from Purchasing (goods received) and sends stock deductions to POS (sale completed).

## Prerequisites

- **INFRA-02** (Shared Kernel) — `SoftDeletableEntity`, `AuditableEntity`

## Wiki References

- `entities/module-inventory.md` — I1–I7 problems, central hub role, data flow
- `concepts/fifo-costing.md` — batch-level: Batch ID, Product ID, Qty Received/Remaining, Unit Cost, Receipt Date, Expiry Date
- `concepts/expiry-date-tracking.md` — batch-level expiry for pesticides, seeds, feeds
- `sources/inventory-module-paper.md` — features list, shrinkage recording, velocity classification

## Deliverables

```
MerchSys.Inventory/Entities/
├── Product.vb
├── StockBatch.vb
├── ShrinkageRecord.vb
├── StockAlertConfig.vb
└── ProductCategory.vb
```

## Specification

### Product
```
Inherits SoftDeletableEntity

Property Name As String                     ' Product name
Property Sku As String                      ' Stock Keeping Unit code
Property CategoryId As Integer              ' FK to ProductCategory
Property Description As String              ' Nullable
Property RetailPrice As Decimal             ' Current selling price
Property Unit As String                     ' "kg", "bag", "bottle", "pack", etc.
Property HasExpiry As Boolean               ' True for perishable products
Property MinimumThreshold As Integer        ' Low-stock alert threshold
Property IsActive As Boolean                ' Active in catalog

' Computed/aggregated (not stored, calculated via service)
' CurrentStock — sum of StockBatch.QtyRemaining where not expired
' TotalValue — sum of StockBatch.QtyRemaining × UnitCost (FIFO valuation)

' Navigation
Property Category As ProductCategory
Property StockBatches As ICollection(Of StockBatch)
Property ShrinkageRecords As ICollection(Of ShrinkageRecord)
```

### StockBatch (FIFO core)
```
Inherits AuditableEntity

Property ProductId As Integer               ' FK to Product
Property QuantityReceived As Integer        ' Original qty in this batch
Property QuantityRemaining As Integer       ' Units not yet sold/consumed/shrunk
Property UnitCost As Decimal                ' Purchase price at receipt
Property ReceiptDate As DateTime            ' When batch was received
Property ExpiryDate As DateTime?            ' Nullable — only for HasExpiry products
Property SourcePurchaseOrderId As Integer?  ' Cross-module: PO ID from Purchasing
Property IsExpired As Boolean               ' Computed: ExpiryDate < Today
Property IsFullyConsumed As Boolean         ' QuantityRemaining = 0

' Navigation
Property Product As Product
```

### ShrinkageRecord
```
Inherits AuditableEntity

Property ProductId As Integer               ' FK to Product
Property StockBatchId As Integer?           ' Optional: specific batch affected
Property QuantityLost As Integer
Property UnitCost As Decimal                ' Cost of lost units (from batch)
Property TotalValue As Decimal              ' Qty × UnitCost
Property Reason As String                   ' "Damage", "Spoilage", "Expiry", "Admin Error"
Property Notes As String                    ' Additional details
Property RecordedDate As DateTime

' Navigation
Property Product As Product
Property StockBatch As StockBatch
```

### StockAlertConfig
```
Inherits AuditableEntity

Property ProductId As Integer               ' FK to Product
Property MinimumThreshold As Integer        ' Alert when stock ≤ this
Property ExpiryAlertDays As Integer         ' Alert when expiry within N days (default 30)
Property IsAlertEnabled As Boolean

' Navigation
Property Product As Product
```

### ProductCategory
```
Inherits SoftDeletableEntity

Property Name As String                     ' "Fertilizers", "Pesticides", "Seeds", "Animal Feeds"
Property Description As String

' Navigation
Property Products As ICollection(Of Product)
```

## Implementation Notes

- `StockBatch` is the heart of FIFO — deductions always consume from the oldest batch first (by `ReceiptDate`)
- `IsExpired` is a computed property: `ExpiryDate.HasValue AndAlso ExpiryDate.Value < DateTime.UtcNow`
- `ProductCategory` has 4 default categories matching Villon Farm Supply's product lines
- `SourcePurchaseOrderId` is an integer reference only — no EF navigation to Purchasing entities

## Acceptance Criteria

1. `dotnet build` succeeds
2. All 5 entity files exist with correct namespaces
3. `StockBatch` has all FIFO-required fields
4. `ShrinkageRecord` captures financial impact
5. No cross-module entity references
6. XML doc comments on all public members

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-01-summary.md`
