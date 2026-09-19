---
module: MerchSys.POS
plan-id: POS-05
title: "Credit (Utang) System"
depends-on: [POS-02, INFRA-04]
estimated-files: 2
---

# Credit (Utang) System

## Context

Implements the Philippine informal credit system (utang) — digital credit accounts, balance tracking, payment recording, and the **non-negotiable hard blocking rule**: if balance > 0, new credit sales are blocked. Publishes `CreditPaymentEvent` to Accounting. Addresses Problems S2 and S4.

## Prerequisites

- **POS-02** (Data Access) — `CreditAccount`, `CreditPayment` DbSets
- **INFRA-04** (MediatR) — `CreditPaymentEvent` contract

## Wiki References

- `concepts/utang-credit-system.md` — **critical: zero-tolerance blocking rule**
- `sources/pos-module-paper.md` — S2, S4, credit blocking, AR management

## Deliverables

```
MerchSys.POS/Services/
├── ICreditService.vb
└── CreditService.vb
```

## Specification

### ICreditService
```
CreateAccountAsync(customerName As String, phone As String, Optional address As String = Nothing) As Task(Of CreditAccount)
GetAccountAsync(id As Integer) As Task(Of CreditAccount)
GetAllAccountsAsync() As Task(Of List(Of CreditAccount))
SearchAccountsAsync(searchTerm As String) As Task(Of List(Of CreditAccount))
CanExtendCreditAsync(customerId As Integer) As Task(Of Boolean)
ChargeCreditAsync(customerId As Integer, amount As Decimal, transactionId As Integer) As Task
RecordPaymentAsync(customerId As Integer, amount As Decimal, paymentMethod As PaymentMethod, receivedBy As String) As Task(Of CreditPayment)
GetPaymentHistoryAsync(customerId As Integer) As Task(Of List(Of CreditPayment))
GetTotalOutstandingAsync() As Task(Of Decimal)
GetOverdueAccountsAsync() As Task(Of List(Of CreditAccount))
```

### Hard Blocking Rule (NON-NEGOTIABLE)

```
Function CanExtendCreditAsync(customerId):
    Dim account = Await GetAccountAsync(customerId)
    Return account.CurrentBalance = 0D    ' ZERO tolerance — any balance > 0 blocks
End Function
```

- **This rule CANNOT be overridden** by any user role
- The UI must **disable** the sale completion button when Credit is selected and customer is blocked
- This is enforced at the service layer AND the UI layer

### Charge Credit Flow
1. Validate `CanExtendCreditAsync` — throw `CreditBlockedException` if blocked
2. Add amount to `CurrentBalance`
3. Add amount to `TotalCreditExtended`
4. Set `IsBlocked = True`
5. Set `LastTransactionDate = Now`

### Record Payment Flow
1. Validate amount > 0 and amount ≤ CurrentBalance
2. Subtract from `CurrentBalance`
3. Add to `TotalPaymentsReceived`
4. If `CurrentBalance = 0`, set `IsBlocked = False`
5. Create `CreditPayment` record
6. **Publish `CreditPaymentEvent`** via MediatR

## Acceptance Criteria

1. `dotnet build` succeeds
2. **Hard blocking enforced** — cannot charge credit when balance > 0
3. Payments reduce balance; full payment unblocks
4. `CreditPaymentEvent` published
5. Overpayment rejected
6. Search works on customer name/phone

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-05-summary.md`
