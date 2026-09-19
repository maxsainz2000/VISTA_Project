---
module: MerchSys.Accounting
plan-id: ACC-03
title: "Financial Overview Service"
depends-on: [ACC-02]
estimated-files: 2
---

# Financial Overview Service

## Context

Implements the Financial Overview Dashboard data service — KPI aggregation, 6-month trend, top-selling products, and AR/AP balances. This is the primary view for the Owner role. Addresses Problems A2 and A7.

## Prerequisites

- **ACC-02** (Data Access) — revenue/expense records populated by event handlers

## Wiki References

- `sources/accounting-module-paper.md` — "Financial Overview Dashboard: KPIs, 6-month trend chart, top-selling products"
- `entities/module-accounting.md` — "A7 — Real-time Financial Overview dashboard"

## Deliverables

```
MerchSys.Accounting/Services/
├── IFinancialOverviewService.vb
└── FinancialOverviewService.vb
```

## Specification

### IFinancialOverviewService
```
GetOverviewAsync() As Task(Of FinancialOverviewDto)
RefreshSnapshotAsync() As Task(Of FinancialSnapshot)
```

### FinancialOverviewDto
```
' KPIs
TodayRevenue, MonthToDateRevenue, YearToDateRevenue As Decimal
TodayTransactions, MonthTransactions As Integer
CurrentGrossMargin As Decimal                        ' This month's %
TotalAR As Decimal                                   ' Outstanding credit balances
TotalAP As Decimal                                   ' Outstanding payables
InventoryValue As Decimal                            ' FIFO valuation

' Trends
MonthlyTrend As List(Of MonthlyTrendDto)             ' Last 6 months: revenue, COGS, gross profit

' Top Products
TopProducts As List(Of TopProductDto)                ' Top 10 by revenue this month

' Alerts
OverdueARCount As Integer
OverdueAPCount As Integer
LowStockAlertCount As Integer
```

### MonthlyTrendDto
```
Month (String, e.g., "Jan 2026"), Revenue, COGS, GrossProfit, GrossMarginPercent
```

### Data Sources
- Revenue/COGS: from `RevenueRecord` table
- AR: query total outstanding credit from POS (via `CreditService` or stored snapshot)
- AP: query total outstanding AP from Purchasing (via MediatR or stored snapshot)
- Inventory Value: query via MediatR `GetInventoryValuationQuery`

## Acceptance Criteria

1. `dotnet build` succeeds
2. All KPIs calculated correctly
3. 6-month trend data with correct aggregations
4. Top 10 products by revenue
5. AR/AP/Inventory values accurate

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-03-summary.md`
