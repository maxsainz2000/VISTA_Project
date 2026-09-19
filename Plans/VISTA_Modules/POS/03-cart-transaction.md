---
module: MerchSys.POS
plan-id: POS-03
title: "Cart & Transaction Service"
depends-on: [POS-02]
estimated-files: 2
---

# Cart & Transaction Service

## Context

Implements the cart-based transaction interface — building a cart with line items, calculating totals with discounts, and finalizing the sale. This is the core POS operation. Addresses Problem S1 (no digital sales record).

## Prerequisites

- **POS-02** (Data Access) — `POSDbContext` with DbSets

## Wiki References

- `sources/pos-module-paper.md` — "Cart-Based Transaction Interface: product, qty, price, discount, payment method, customer"
- `entities/module-pos.md` — "S1 — No digital sales record"

## Deliverables

```
MerchSys.POS/Services/
├── ICartService.vb
└── CartService.vb
```

## Specification

### ICartService
```
CreateCartAsync() As Task(Of CartDto)
AddLineAsync(cartId As Guid, productId As Integer, productName As String, quantity As Integer, unitPrice As Decimal) As Task(Of CartDto)
UpdateLineQuantityAsync(cartId As Guid, lineIndex As Integer, newQuantity As Integer) As Task(Of CartDto)
RemoveLineAsync(cartId As Guid, lineIndex As Integer) As Task(Of CartDto)
ApplyLineDiscountAsync(cartId As Guid, lineIndex As Integer, discountAmount As Decimal) As Task(Of CartDto)
FinalizeAsync(cartId As Guid, paymentMethod As PaymentMethod, amountTendered As Decimal, Optional customerId As Integer? = Nothing) As Task(Of SalesTransaction)
VoidTransactionAsync(transactionId As Integer, reason As String) As Task
GetTransactionHistoryAsync(Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of SalesTransaction))
```

### CartDto (in-memory, not persisted until finalized)
```
CartId As Guid, Lines As List(Of CartLineDto), SubTotal, DiscountTotal, VatAmount, GrandTotal
```

### Finalization Flow

1. Validate cart has ≥1 line
2. Validate payment: if Cash, `AmountTendered ≥ GrandTotal`; if Credit, validate customer not blocked (delegate to credit service)
3. Create `SalesTransaction` with auto-generated `TX-YYYY-XXXX` number
4. Create `SalesTransactionLine` for each cart line
5. Calculate `ChangeAmount = AmountTendered - GrandTotal` (Cash only)
6. Save to database
7. Return the finalized `SalesTransaction` — receipt and event publishing handled by other plans

### VAT Calculation
- If system is VAT-registered: `VatAmount = SubTotal × 0.12`
- If non-VAT: `VatAmount = 0`
- Config flag read from app settings

## Acceptance Criteria

1. `dotnet build` succeeds
2. Cart operations (add/update/remove/discount) work correctly
3. Totals recalculate on every change
4. TX number auto-generated
5. Cash change calculated correctly
6. Empty cart cannot be finalized

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-03-summary.md`
