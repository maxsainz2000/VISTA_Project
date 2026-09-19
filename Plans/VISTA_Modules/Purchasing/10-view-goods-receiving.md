---
module: MerchSys.Purchasing
plan-id: PUR-10
title: "View — Goods Receiving"
depends-on: [PUR-04]
estimated-files: 4
---

# View — Goods Receiving

## Context

WPF View and ViewModel for the goods receiving process. Manager selects a submitted PO, enters actual quantities received, captures expiry dates, notes discrepancies, and confirms receipt.

## Prerequisites

- **PUR-04** (Goods Receiving Service) — `IGoodsReceivingService` implemented

## Wiki References

- `sources/purchasing-module-paper.md` — "quantity/condition check, expiry capture, discrepancy flagging"

## Deliverables

```
MerchSys.App/Views/Purchasing/
├── GoodsReceivingView.xaml
└── GoodsReceivingView.xaml.vb

MerchSys.Purchasing/ViewModels/
└── GoodsReceivingViewModel.vb
```

## Specification

### Screen Layout

1. **PO Selector** — dropdown of POs in `Submitted` status showing OrderNumber + Vendor
2. **Receiving Grid** — when PO selected, auto-populate with PO lines:
   - ProductName (read-only), QtyOrdered (read-only), QtyReceived (editable), UnitCost (editable), ExpiryDate (date picker, optional), DiscrepancyNotes (text, shown if qty differs)
3. **Discrepancy highlight** — row background changes if QtyReceived ≠ QtyOrdered
4. **Confirm Receipt button** — triggers `ReceiveGoodsAsync`, shows success/failure toast

### ViewModel

- Loads submitted POs on initialization
- On PO selection, loads PO lines and pre-fills QtyReceived = QtyOrdered
- Validates: all QtyReceived ≥ 0, at least one line received
- Calls `GoodsReceivingService.ReceiveGoodsAsync` on confirm
- Shows toast notification on success

## Acceptance Criteria

1. `dotnet build` succeeds
2. Only Submitted POs appear in selector
3. Discrepancies are visually highlighted
4. Receipt confirmation triggers backend service
5. Expiry date picker available per line

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-10-summary.md`
