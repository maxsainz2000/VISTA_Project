---
module: MerchSys.Purchasing
plan-id: PUR-12
title: "View — AP Ledger"
depends-on: [PUR-06]
estimated-files: 3
---

# View — AP Ledger

## Context

WPF View and ViewModel for accounts payable management — outstanding balances, payment recording, overdue tracking.

## Prerequisites

- **PUR-06** (AP Tracking Service) — `IAccountsPayableService` implemented

## Deliverables

```
MerchSys.App/Views/Purchasing/
├── APLedgerView.xaml
└── APLedgerView.xaml.vb

MerchSys.Purchasing/ViewModels/
└── APLedgerViewModel.vb
```

## Specification

### AP Ledger Screen

- **Summary header:** Total Outstanding balance
- **DataGrid:** VendorName, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, AmountPaid, Balance, IsPaid, IsOverdue
- **Filters:** All, Outstanding Only, Overdue Only, Paid
- **Row styling:** Overdue rows highlighted in red/orange
- **Record Payment button** — opens dialog: enter amount, validates ≤ balance
- **Vendor filter dropdown** — filter by specific vendor

### Payment Dialog

- Shows: Vendor, Invoice, Balance remaining
- Input: Payment amount (decimal, validated)
- Confirm / Cancel buttons

## Acceptance Criteria

1. `dotnet build` succeeds
2. AP ledger displays with correct balances
3. Overdue entries visually highlighted
4. Payments recorded; balance recalculates
5. Overpayment rejected with error message

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-12-summary.md`
