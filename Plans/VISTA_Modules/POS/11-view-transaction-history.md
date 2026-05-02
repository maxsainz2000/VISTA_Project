---
module: MerchSys.POS
plan-id: POS-11
title: "View — Transaction History"
depends-on: [POS-03, POS-07]
estimated-files: 3
---

# View — Transaction History

## Context

WPF View for searching and viewing past transactions, viewing receipts, and processing returns.

## Prerequisites

- **POS-03** (Cart & Transaction) — transaction history query
- **POS-07** (Sales Returns) — return processing service

## Deliverables

```
MerchSys.App/Views/POS/
├── TransactionHistoryView.xaml
└── TransactionHistoryView.xaml.vb

MerchSys.POS/ViewModels/
└── TransactionHistoryViewModel.vb
```

## Specification

### Screen Layout

1. **Search/Filter Bar:** Date range picker, TX number search, Payment method filter
2. **Transaction DataGrid:** TxNumber, Date, CustomerName, PaymentMethod, TotalAmount, ItemCount, HasReturns
3. **Transaction Detail Panel (on select):**
   - Line items: Product, Qty, Price, LineTotal
   - Receipt info: ReceiptNumber, business details
   - Returns: any returns against this transaction
4. **Actions:** View Receipt (print preview), Process Return (opens return dialog)

### Return Dialog
- Select product from original TX lines
- Enter return quantity (≤ original)
- Enter reason (required)
- Restock checkbox
- Confirm → processes return

## Acceptance Criteria

1. `dotnet build` succeeds
2. Transaction history searchable by date, TX#, payment method
3. Detail panel shows line items and receipt
4. Returns processable from history screen
5. Return validation prevents over-return

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-11-summary.md`
