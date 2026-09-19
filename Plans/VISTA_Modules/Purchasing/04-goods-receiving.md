---
module: MerchSys.Purchasing
plan-id: PUR-04
title: "Goods Receiving"
depends-on: [PUR-03, INFRA-04]
estimated-files: 3
---

# Goods Receiving

## Context

Implements goods receiving verification against a Purchase Order. When goods arrive, the manager records quantities received, checks for discrepancies, captures expiry dates for perishable products, and publishes a `GoodsReceivedEvent` via MediatR so Inventory can update stock and Accounting can create AP entries.

## Prerequisites

- **PUR-03** (PO Lifecycle) — PO must be in `Submitted` status to receive goods
- **INFRA-04** (MediatR Event Bus) — `GoodsReceivedEvent` contract exists

## Wiki References

- `sources/purchasing-module-paper.md` — "Goods Receiving Verification: quantity/condition check, expiry capture, discrepancy flagging"
- `concepts/fifo-costing.md` — "Each purchase batch records: Batch ID, Product ID, Qty Received, Unit Cost, Receipt Date, Expiry Date"
- `analysis/cross-module-data-flow.md` — `GoodsReceivedEvent`: Purchasing → Inventory, Accounting

## Deliverables

```
MerchSys.Purchasing/
├── Services/
│   ├── IGoodsReceivingService.vb
│   └── GoodsReceivingService.vb
└── Dtos/
    └── ReceiveGoodsDto.vb
```

## Specification

### IGoodsReceivingService
```
Public Interface IGoodsReceivingService
    Function ReceiveGoodsAsync(purchaseOrderId As Integer, lines As List(Of ReceiveGoodsLineDto)) As Task(Of GoodsReceipt)
    Function GetReceiptByIdAsync(id As Integer) As Task(Of GoodsReceipt)
    Function GetReceiptsForPOAsync(purchaseOrderId As Integer) As Task(Of List(Of GoodsReceipt))
End Interface
```

### ReceiveGoodsLineDto
```
Public Class ReceiveGoodsLineDto
    Public Property ProductId As Integer
    Public Property ProductName As String
    Public Property QuantityOrdered As Integer
    Public Property QuantityReceived As Integer
    Public Property UnitCost As Decimal              ' Actual cost at receipt
    Public Property ExpiryDate As DateTime?           ' Nullable
    Public Property DiscrepancyNotes As String        ' Nullable
End Class
```

### Receiving Flow

1. Validate PO is in `Submitted` status
2. Create `GoodsReceipt` with auto-generated `GR-YYYY-XXXX` number
3. Create `GoodsReceiptLine` for each item:
   - Set `HasDiscrepancy = True` if `QuantityReceived ≠ QuantityOrdered`
   - Capture `ExpiryDate` if provided
   - Record actual `UnitCost` (may differ from PO line's agreed cost)
4. Transition PO status to `Received`
5. **Publish `GoodsReceivedEvent`** via MediatR with all received items
   - Event payload includes: PO ID, received date, and for each item: ProductId, ProductName, QuantityReceived, UnitCost, ExpiryDate
6. Return the created `GoodsReceipt`

### Discrepancy Handling

- If quantity received differs from ordered, flag `HasDiscrepancy = True`
- Require `DiscrepancyNotes` when there's a discrepancy
- The system records what was actually received (not what was ordered)
- Partial receiving is allowed (e.g., ordered 100, received 80)

## Implementation Notes

- Use `IMediator.Publish()` for the `GoodsReceivedEvent` — it's a notification (multiple consumers)
- The event is published **after** the receiving data is saved to the database
- Expiry dates are only required for products flagged as perishable (but since we don't know product categories in Purchasing, accept it as nullable and let the user decide)
- Register service in DI

## Acceptance Criteria

1. `dotnet build` succeeds
2. Only `Submitted` POs can receive goods
3. Discrepancies are flagged and require notes
4. `GR-YYYY-XXXX` receipt numbers are generated
5. `GoodsReceivedEvent` is published after save
6. PO status transitions to `Received`

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-04-summary.md`
