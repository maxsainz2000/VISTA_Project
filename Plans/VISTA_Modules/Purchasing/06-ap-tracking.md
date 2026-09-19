---
module: MerchSys.Purchasing
plan-id: PUR-06
title: "AP Tracking"
depends-on: [PUR-04]
estimated-files: 2
---

# AP Tracking

## Context

Implements Accounts Payable tracking — managing outstanding balances to vendors with due dates, partial payment recording, and overdue detection. AP entries are created when goods are received and verified.

## Prerequisites

- **PUR-04** (Goods Receiving) — AP entries created after goods receipt

## Wiki References

- `entities/module-purchasing.md` — "P4 — No AP ledger → AP Tracking with due dates"
- `sources/purchasing-module-paper.md` — "AP Tracking: credit terms, due dates, outstanding balances"

## Deliverables

```
MerchSys.Purchasing/Services/
├── IAccountsPayableService.vb
└── AccountsPayableService.vb
```

## Specification

### IAccountsPayableService

```
CreateFromPurchaseOrderAsync(purchaseOrderId As Integer, invoiceNumber As String, invoiceDate As DateTime, dueDate As DateTime) As Task(Of AccountsPayableEntry)
RecordPaymentAsync(apEntryId As Integer, amount As Decimal) As Task(Of AccountsPayableEntry)
GetAllOutstandingAsync() As Task(Of List(Of AccountsPayableEntry))
GetByVendorAsync(vendorId As Integer) As Task(Of List(Of AccountsPayableEntry))
GetOverdueAsync() As Task(Of List(Of AccountsPayableEntry))
GetTotalOutstandingAsync() As Task(Of Decimal)
```

### Business Rules

1. AP entry `TotalAmount` = sum of goods receipt line costs
2. `Balance` = `TotalAmount - AmountPaid`; auto-recalculated on payment
3. `IsPaid` = True when `Balance = 0`
4. Partial payments allowed — `AmountPaid` accumulates
5. Payment amount cannot exceed `Balance`
6. Overdue = `DueDate < Today AND NOT IsPaid`

### Payment Flow

1. Validate payment amount ≤ remaining balance
2. Add payment amount to `AmountPaid`
3. Recalculate `Balance`
4. Set `IsPaid = True` if `Balance = 0`

## Acceptance Criteria

1. `dotnet build` succeeds
2. AP entries created with correct total from GR
3. Partial payments work; balance recalculates
4. Overpayment is rejected
5. Overdue query returns correct entries

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-06-summary.md`
