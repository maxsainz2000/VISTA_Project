---
module: MerchSys.POS
plan-id: POS-07
title: "Sales Returns & Exchanges"
depends-on: [POS-03]
estimated-files: 2
---

# Sales Returns & Exchanges

## Context

Implements sales return and exchange processing — captures return reason, links to original transaction, optionally restocks inventory.

## Prerequisites

- **POS-03** (Cart & Transaction) — `SalesTransaction` queryable

## Wiki References

- `sources/pos-module-paper.md` — "Sales Return & Exchange: reason captured, inventory restocked, original TX linked"

## Deliverables

```
MerchSys.POS/Services/
├── ISalesReturnService.vb
└── SalesReturnService.vb
```

## Specification

### ISalesReturnService
```
ProcessReturnAsync(originalTransactionId As Integer, productId As Integer, quantity As Integer, reason As String, shouldRestock As Boolean) As Task(Of SalesReturn)
GetReturnsForTransactionAsync(transactionId As Integer) As Task(Of List(Of SalesReturn))
GetReturnHistoryAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of SalesReturn))
```

### Return Flow

1. Validate original transaction exists and is not voided
2. Validate return quantity ≤ original quantity sold for that product
3. Calculate refund amount based on original unit price
4. Create `SalesReturn` record linked to original transaction
5. If `shouldRestock = True`, publish event or call service to add stock back (reverse of sale)
6. If original payment was Credit, reduce customer's balance

### Business Rules
- Reason is required — cannot process return without explanation
- Partial returns allowed (return 2 of 5 items)
- Multiple returns against same transaction allowed (different products)
- Cannot return more than originally purchased

## Acceptance Criteria

1. `dotnet build` succeeds
2. Returns linked to original transaction
3. Reason captured and required
4. Quantity validation prevents over-return
5. Restock option works

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-07-summary.md`
