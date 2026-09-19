---
module: MerchSys.POS
plan-id: POS-01
title: "POS Domain Models"
depends-on: [INFRA-02]
estimated-files: 6
---

# POS Domain Models

## Context

Defines the core entities for the POS module — sales transactions, line items, receipts, customer credit accounts, credit payments, and sales return records.

## Prerequisites

- **INFRA-02** (Shared Kernel) — `SoftDeletableEntity`, `PaymentMethod` enum

## Wiki References

- `entities/module-pos.md` — S1–S5 problems, payment methods, data flow
- `concepts/utang-credit-system.md` — credit accounts, hard blocking rule
- `concepts/bir-compliance.md` — OR-YYYY-XXXX receipt format
- `concepts/vat-ready.md` — VAT/non-VAT configuration
- `sources/pos-module-paper.md` — 10 core features list

## Deliverables

```
MerchSys.POS/Entities/
├── SalesTransaction.vb
├── SalesTransactionLine.vb
├── OfficialReceipt.vb
├── CreditAccount.vb
├── CreditPayment.vb
└── SalesReturn.vb
```

## Specification

### SalesTransaction
```
Inherits SoftDeletableEntity

Property TransactionNumber As String        ' TX-YYYY-XXXX auto-sequential
Property TransactionDate As DateTime
Property CustomerId As Integer?             ' Nullable — cash sales have no customer
Property CustomerName As String             ' Nullable, denormalized
Property PaymentMethod As PaymentMethod     ' Cash, GCash, BankTransfer, Credit
Property SubTotal As Decimal                ' Sum of line totals before discount
Property DiscountAmount As Decimal          ' Total discount applied
Property VatAmount As Decimal               ' VAT if applicable (12% or 0)
Property TotalAmount As Decimal             ' SubTotal - Discount + VAT
Property AmountTendered As Decimal          ' Cash given by customer
Property ChangeAmount As Decimal            ' AmountTendered - TotalAmount
Property IsVoided As Boolean
Property VoidReason As String

' Navigation
Property Lines As ICollection(Of SalesTransactionLine)
Property Receipt As OfficialReceipt
Property CreditAccount As CreditAccount
```

### SalesTransactionLine
```
Inherits AuditableEntity

Property TransactionId As Integer           ' FK
Property ProductId As Integer               ' Cross-module reference
Property ProductName As String              ' Denormalized
Property Quantity As Integer
Property UnitPrice As Decimal               ' Retail price at time of sale
Property DiscountAmount As Decimal          ' Per-line discount
Property LineTotal As Decimal               ' (Qty × UnitPrice) - Discount

' Navigation
Property Transaction As SalesTransaction
```

### OfficialReceipt
```
Inherits AuditableEntity

Property TransactionId As Integer           ' FK — one receipt per transaction
Property ReceiptNumber As String            ' OR-YYYY-XXXX (BIR format)
Property BusinessName As String             ' "Villon Farm Supply"
Property BusinessAddress As String
Property BusinessTIN As String              ' Tax ID Number
Property IssueDate As DateTime
Property Items As String                    ' Serialized line items for receipt (JSON or formatted text)
Property TotalAmount As Decimal
Property VatAmount As Decimal
Property IsVatRegistered As Boolean         ' From system config

' Navigation
Property Transaction As SalesTransaction
```

### CreditAccount
```
Inherits SoftDeletableEntity

Property CustomerName As String             ' Full name
Property Phone As String
Property Address As String                  ' Nullable
Property CurrentBalance As Decimal          ' Outstanding amount owed
Property TotalCreditExtended As Decimal     ' Lifetime credit total
Property TotalPaymentsReceived As Decimal   ' Lifetime payments
Property IsBlocked As Boolean               ' True when CurrentBalance > 0
Property LastTransactionDate As DateTime?
Property Notes As String

' Navigation
Property Transactions As ICollection(Of SalesTransaction)
Property Payments As ICollection(Of CreditPayment)
```

### CreditPayment
```
Inherits AuditableEntity

Property CreditAccountId As Integer         ' FK
Property PaymentAmount As Decimal
Property PaymentDate As DateTime
Property PaymentMethod As PaymentMethod     ' Cash, GCash, or BankTransfer (NOT Credit)
Property Notes As String
Property ReceivedBy As String               ' Who recorded the payment

' Navigation
Property CreditAccount As CreditAccount
```

### SalesReturn
```
Inherits AuditableEntity

Property OriginalTransactionId As Integer   ' FK to original sale
Property ReturnDate As DateTime
Property ProductId As Integer
Property ProductName As String
Property QuantityReturned As Integer
Property UnitPrice As Decimal
Property RefundAmount As Decimal
Property Reason As String                   ' Required — why returning
Property IsRestocked As Boolean             ' True if returned to inventory

' Navigation
Property OriginalTransaction As SalesTransaction
```

## Implementation Notes

- `CreditAccount.IsBlocked` is a **computed/maintained** field: `CurrentBalance > 0`; this is the hard blocking rule — **non-negotiable**
- `OfficialReceipt.ReceiptNumber` uses OR-YYYY-XXXX format per BIR requirements, auto-sequential, never gaps
- `PaymentMethod` on `CreditPayment` excludes `Credit` — you can't pay credit with credit
- All monetary fields use `Decimal` with precision(18,2)

## Acceptance Criteria

1. `dotnet build` succeeds
2. All 6 entity files exist with correct namespaces
3. Credit blocking logic represented in `IsBlocked` field
4. BIR receipt number format documented
5. No cross-module entity references
6. XML doc comments on all public members

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-01-summary.md`
