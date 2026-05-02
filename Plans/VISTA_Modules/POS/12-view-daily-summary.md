---
module: MerchSys.POS
plan-id: POS-12
title: "View — Daily Summary"
depends-on: [POS-08]
estimated-files: 3
---

# View — Daily Summary

## Context

WPF View for the daily, weekly, and monthly sales summary reports.

## Prerequisites

- **POS-08** (Daily Summary Service)

## Deliverables

```
MerchSys.App/Views/POS/
├── DailySummaryView.xaml
└── DailySummaryView.xaml.vb

MerchSys.POS/ViewModels/
└── DailySummaryViewModel.vb
```

## Specification

### Screen Layout

1. **Period Selector:** Date picker (daily), Week picker, Month picker
2. **Summary Cards:** Total Sales, TX Count, Avg TX Value, Returns
3. **Payment Method Breakdown:** Bar chart or table — Cash, GCash, Bank, Credit with counts and totals
4. **Top Products:** Top 5 selling products for the period
5. **Trend (weekly/monthly view):** Simple bar chart showing daily totals

### Owner View
Same screen, same data — Owner is the primary consumer of summary reports.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Daily summary shows correct totals
3. Payment breakdown adds up to total
4. Top products identified correctly
5. Weekly/monthly aggregations work

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-12-summary.md`
