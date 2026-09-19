---
module: MerchSys.POS
plan-id: POS-04
title: "Payment Processing"
depends-on: [POS-03, INFRA-04]
estimated-files: 2
---

# Payment Processing

## Context

Handles all four payment methods (Cash, GCash, Bank Transfer, Credit) and publishes `SaleCompletedEvent` after successful payment. GCash and Bank Transfer are recorded only — no actual electronic transfers.

## Prerequisites

- **POS-03** (Cart & Transaction) — `ICartService.FinalizeAsync`
- **INFRA-04** (MediatR) — `SaleCompletedEvent` contract

## Wiki References

- `entities/module-pos.md` — payment methods table
- `analysis/cross-module-data-flow.md` — `SaleCompletedEvent`: POS → Inventory, Accounting

## Deliverables

```
MerchSys.POS/Services/
├── IPaymentService.vb
└── PaymentService.vb
```

## Specification

### IPaymentService
```
ProcessPaymentAsync(transactionId As Integer, paymentMethod As PaymentMethod, amountTendered As Decimal, Optional customerId As Integer? = Nothing) As Task(Of PaymentResultDto)
```

### PaymentResultDto
```
Success, TransactionId, ChangeAmount, ReceiptNumber, ErrorMessage
```

### Payment Method Behavior

| Method | Validation | Action |
|---|---|---|
| Cash | AmountTendered ≥ Total | Calculate change |
| GCash | AmountTendered = Total | Record as e-wallet (no transfer) |
| BankTransfer | AmountTendered = Total | Record as bank (no transfer) |
| Credit | Customer exists, not blocked | Charge to credit account |

### Post-Payment

After successful payment:
1. Generate official receipt (delegate to POS-06)
2. **Publish `SaleCompletedEvent`** via MediatR — triggers Inventory stock deduction and Accounting revenue recording
3. If Credit payment, update `CreditAccount.CurrentBalance` and set `IsBlocked = True`

## Acceptance Criteria

1. `dotnet build` succeeds
2. All 4 payment methods work per spec
3. `SaleCompletedEvent` published after payment
4. Credit payment updates customer balance and blocking status
5. Cash change calculated correctly

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-04-summary.md`
