---
module: MerchSys.Accounting
plan-id: ACC-09
title: "View — Sales Summary"
depends-on: [ACC-05, ACC-06]
estimated-files: 3
---

# View — Sales Summary

## Context

WPF View for the accounting sales summary — daily/weekly/monthly breakdown by payment method with the mandatory "What This Means" interpretation.

## Prerequisites

- **ACC-05** (Sales Summary Service), **ACC-06** (What This Means Engine)

## Deliverables

```
MerchSys.App/Views/Accounting/
├── SalesSummaryView.xaml
└── SalesSummaryView.xaml.vb

MerchSys.Accounting/ViewModels/
└── SalesSummaryViewModel.vb
```

## Specification

### Screen Layout

1. **Period Selector:** Daily / Weekly / Monthly toggle + date picker
2. **Summary Cards:** Total Net Sales, TX Count, Avg TX Value, Returns
3. **Payment Method Breakdown Table:**

   | Payment Method | Transactions | Gross Amount | Net Amount | % of Total |
   |---|---|---|---|---|
   | Cash | XX | ₱XX,XXX | ₱XX,XXX | XX% |
   | GCash | XX | ₱XX,XXX | ₱XX,XXX | XX% |
   | Bank Transfer | XX | ₱XX,XXX | ₱XX,XXX | XX% |
   | Credit (Utang) | XX | ₱XX,XXX | ₱XX,XXX | XX% |
   | **Total** | **XX** | **₱XX,XXX** | **₱XX,XXX** | **100%** |

4. **"What This Means" Box (mandatory):**
   - Interpretation of sales mix, credit percentage warning if high

5. **Daily Breakdown (for weekly/monthly views):**
   - Per-day totals in a mini-table or simple bar chart

### Credit Warning Logic
If credit sales > 30% of total: highlight in the "What This Means" box with a warning about AR exposure.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Payment breakdown adds up to totals
3. Daily/weekly/monthly switching works
4. **"What This Means" box always visible**
5. Credit warning triggered when appropriate
6. ₱ currency formatting correct

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-09-summary.md`
