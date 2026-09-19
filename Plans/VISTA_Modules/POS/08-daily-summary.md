---
module: MerchSys.POS
plan-id: POS-08
title: "Daily Sales Summary"
depends-on: [POS-03]
estimated-files: 2
---

# Daily Sales Summary

## Context

Implements automated daily sales summary — total sales, transaction count, breakdown by payment method. Addresses Problem S5 (no daily summary).

## Prerequisites

- **POS-03** (Cart & Transaction) — `SalesTransaction` data available

## Wiki References

- `sources/pos-module-paper.md` — "Daily Sales Summary: total sales, TX count, payment method breakdown"
- `entities/module-pos.md` — "S5 — No daily summary"

## Deliverables

```
MerchSys.POS/Services/
├── IDailySummaryService.vb
└── DailySummaryService.vb
```

## Specification

### IDailySummaryService
```
GetDailySummaryAsync(date As DateTime) As Task(Of DailySummaryDto)
GetWeeklySummaryAsync(weekStartDate As DateTime) As Task(Of PeriodSummaryDto)
GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of PeriodSummaryDto)
```

### DailySummaryDto
```
Date, TotalSales, TransactionCount, AverageTransactionValue, 
PaymentBreakdown As List(Of PaymentMethodBreakdownDto),
TopSellingProducts As List(Of TopProductDto) (top 5),
ReturnCount, ReturnValue
```

### PaymentMethodBreakdownDto
```
PaymentMethod, Count, Total, Percentage
```

### Period Summary (weekly/monthly)
Same fields as daily + daily breakdown list for trend analysis.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Daily totals calculated correctly
3. Payment method breakdown accurate
4. Top-selling products identified
5. Returns counted separately

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-08-summary.md`
