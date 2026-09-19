---
module: MerchSys.POS
plan-id: POS-09
title: "View — Sales Cart"
depends-on: [POS-03, POS-04, POS-05, POS-06]
estimated-files: 3
---

# View — Sales Cart

## Context

The main POS transaction screen — the most frequently used view in the entire system. Manager builds a cart, selects payment method, processes payment, and receives a receipt. This is the customer-facing sales workflow.

## Prerequisites

- **POS-03** (Cart), **POS-04** (Payment), **POS-05** (Credit), **POS-06** (Receipt)

## Wiki References

- `entities/module-pos.md` — full transaction workflow, payment methods

## Deliverables

```
MerchSys.App/Views/POS/
├── SalesCartView.xaml
└── SalesCartView.xaml.vb

MerchSys.POS/ViewModels/
└── SalesCartViewModel.vb
```

## Specification

### Screen Layout

1. **Product Search (left panel):**
   - Search box with auto-complete
   - Product list: Name, SKU, Price, Available Stock
   - Click or Enter to add to cart

2. **Cart (center):**
   - DataGrid: Product, Qty (editable), UnitPrice, Discount, LineTotal
   - Remove line button per row
   - Running totals: SubTotal, Discount, VAT, **Grand Total**

3. **Payment Panel (right):**
   - Payment method selector (4 buttons: Cash, GCash, Bank, Credit)
   - If Cash: Amount Tendered input, auto-calculated Change
   - If Credit: Customer search/select dropdown, **blocked indicator** (red badge when IsBlocked)
   - **Pay button** — disabled if validation fails

4. **Receipt Preview:** after payment, show receipt in a print-preview panel

### Critical UI Rules

- When Credit is selected and customer `IsBlocked = True`:
  - Show "⛔ Customer has outstanding balance of ₱{amount}" in red
  - **Disable the Pay button** — non-negotiable
- Product stock validation: warn if cart qty > available stock
- Empty cart: Pay button disabled

## Acceptance Criteria

1. `dotnet build` succeeds
2. Full cart workflow: search → add → pay → receipt
3. **Credit blocking visible in UI** — Pay disabled for blocked customers
4. Cash change calculated in real-time
5. Receipt generated and shown after payment
6. Stock availability shown per product

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-09-summary.md`
