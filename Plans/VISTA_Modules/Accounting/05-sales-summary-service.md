---
module: MerchSys.Accounting
plan-id: ACC-05
title: "Sales Summary Service"
depends-on: [ACC-02]
estimated-files: 2
---

# Sales Summary Service

## Context

Implements the sales summary report — daily/weekly/monthly breakdown by payment method for the Accounting perspective. Distinct from POS daily summary: this is the formal accounting view.

## Prerequisites

- **ACC-02** (Data Access) — `RevenueRecord` data available

## Wiki References

- `sources/accounting-module-paper.md` — "Sales Summary: daily/weekly/monthly breakdown by payment method"

## Deliverables

```
MerchSys.Accounting/Services/
├── ISalesSummaryService.vb
└── SalesSummaryService.vb
```

## Specification

### ISalesSummaryService
```
GetDailySummaryAsync(date As DateTime) As Task(Of AccountingSalesSummaryDto)
GetWeeklySummaryAsync(weekStart As DateTime) As Task(Of AccountingSalesSummaryDto)
GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of AccountingSalesSummaryDto)
GetPaymentMethodTrendAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of PaymentTrendDto))
```

### AccountingSalesSummaryDto
```
PeriodDescription, StartDate, EndDate
TotalGrossSales, TotalDiscounts, TotalReturns, TotalNetSales
TransactionCount
PaymentBreakdown As List(Of PaymentBreakdownDto)      ' Per payment method
DailyBreakdown As List(Of DailySalesDto)              ' Per-day totals (for weekly/monthly)
```

### PaymentBreakdownDto
```
PaymentMethod, TransactionCount, GrossAmount, NetAmount, Percentage
```

### PaymentTrendDto
```
Date, CashTotal, GCashTotal, BankTransferTotal, CreditTotal
```

## Acceptance Criteria

1. `dotnet build` succeeds
2. Daily/weekly/monthly aggregations correct
3. Payment method breakdown accurate (totals = sum of methods)
4. Trend data for payment method analysis

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-05-summary.md`
