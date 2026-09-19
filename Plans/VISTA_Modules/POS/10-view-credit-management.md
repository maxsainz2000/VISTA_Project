---
module: MerchSys.POS
plan-id: POS-10
title: "View — Credit Management"
depends-on: [POS-05]
estimated-files: 3
---

# View — Credit Management

## Context

Dedicated screen for managing customer credit accounts — view all accounts, record payments, see payment history, and identify overdue accounts.

## Prerequisites

- **POS-05** (Credit System Service)

## Deliverables

```
MerchSys.App/Views/POS/
├── CreditManagementView.xaml
└── CreditManagementView.xaml.vb

MerchSys.POS/ViewModels/
└── CreditManagementViewModel.vb
```

## Specification

### Screen Layout

1. **Summary Cards:** Total Outstanding AR, Blocked Accounts Count, Overdue Count
2. **Account List DataGrid:** CustomerName, Phone, CurrentBalance, IsBlocked (icon), TotalCredited, TotalPaid, LastTransaction
3. **Filters:** All, Blocked, With Balance, Cleared
4. **Search:** by customer name or phone
5. **Actions:** Add Account, Record Payment, View History

### Record Payment Dialog
- Customer info (read-only), Current Balance
- Payment amount input (validated ≤ balance)
- Payment method dropdown (Cash, GCash, Bank — NOT Credit)
- Confirm button → records payment, updates balance, shows success toast
- If payment clears balance fully: "✅ Account cleared — credit re-enabled"

### Payment History Panel (on account select)
- List of all payments: Date, Amount, Method, ReceivedBy
- List of all credit transactions: Date, TX#, Amount

## Acceptance Criteria

1. `dotnet build` succeeds
2. All credit accounts listed with correct balances
3. Payment recording works; balance updates
4. Blocked status visible and accurate
5. Full payment clears blocked status

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-10-summary.md`
