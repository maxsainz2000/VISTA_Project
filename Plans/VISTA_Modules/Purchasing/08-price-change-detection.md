---
module: MerchSys.Purchasing
plan-id: PUR-08
title: "Price Change Detection"
depends-on: [PUR-04]
estimated-files: 2
---

# Price Change Detection

## Context

Detects when supplier prices change between the PO agreed cost and the actual cost at goods receipt. Alerts the manager to review retail prices to maintain margins. Addresses Problem P3 (manual retail price updates).

## Prerequisites

- **PUR-04** (Goods Receiving) — `GoodsReceiptLine.UnitCost` vs `PurchaseOrderLine.UnitCost`

## Wiki References

- `entities/module-purchasing.md` — "P3 — Manual retail price updates → Price Change Detection & Alerts"
- `sources/purchasing-module-paper.md` — "alerts manager to review retail price before selling"

## Deliverables

```
MerchSys.Purchasing/
├── Entities/
│   └── PriceChangeAlert.vb
├── Services/
│   ├── IPriceChangeService.vb
│   └── PriceChangeService.vb
```

## Specification

### PriceChangeAlert Entity
```
Inherits AuditableEntity

Property ProductId As Integer
Property ProductName As String
Property VendorId As Integer
Property VendorName As String
Property PreviousUnitCost As Decimal        ' PO agreed cost
Property NewUnitCost As Decimal             ' Actual receipt cost
Property ChangePercent As Decimal           ' ((New - Previous) / Previous) × 100
Property ChangeDirection As String          ' "Increase" or "Decrease"
Property GoodsReceiptId As Integer          ' FK to receipt that triggered this
Property IsAcknowledged As Boolean          ' Manager has seen and acted on it
Property AcknowledgedAt As DateTime?
```

### IPriceChangeService
```
DetectChangesAsync(goodsReceiptId As Integer) As Task(Of List(Of PriceChangeAlert))
GetUnacknowledgedAsync() As Task(Of List(Of PriceChangeAlert))
AcknowledgeAsync(alertId As Integer) As Task
GetHistoryForProductAsync(productId As Integer) As Task(Of List(Of PriceChangeAlert))
```

### Detection Logic

Called automatically after goods receiving completes:
1. For each `GoodsReceiptLine`, compare `UnitCost` to the corresponding `PurchaseOrderLine.UnitCost`
2. If costs differ by any amount, create a `PriceChangeAlert`
3. Calculate `ChangePercent = ((NewCost - PreviousCost) / PreviousCost) × 100`
4. Alert remains unacknowledged until manager reviews it

### Integration Point

Call `DetectChangesAsync` at the end of `GoodsReceivingService.ReceiveGoodsAsync` (update PUR-04 service to call this after saving the receipt).

## Acceptance Criteria

1. `dotnet build` succeeds
2. Alerts generated when receipt cost ≠ PO cost
3. Change percentage calculated correctly
4. Unacknowledged alerts queryable
5. History per product available

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-08-summary.md`
