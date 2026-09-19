---
module: MerchSys.Accounting
plan-id: ACC-04
title: "Income Statement Service"
depends-on: [ACC-02]
estimated-files: 2
---

# Income Statement Service

## Context

Implements the merchandising-format income statement (P&L) — Net Sales → COGS → Gross Profit → Operating Expenses → Net Income. Supports monthly, quarterly, and annual views. Uses FIFO-based COGS. Addresses Problem A1 and A3.

## Prerequisites

- **ACC-02** (Data Access) — revenue and expense records available

## Wiki References

- `sources/accounting-module-paper.md` — "Merchandising-Format Income Statement: Net Sales, COGS (FIFO), Gross Profit, OpEx, Net Income"
- `concepts/fifo-costing.md` — "Accounting calculates COGS for Income Statement"

## Deliverables

```
MerchSys.Accounting/Services/
├── IIncomeStatementService.vb
└── IncomeStatementService.vb
```

## Specification

### IIncomeStatementService
```
GenerateAsync(startDate As DateTime, endDate As DateTime) As Task(Of IncomeStatementDto)
GenerateMonthlyAsync(year As Integer, month As Integer) As Task(Of IncomeStatementDto)
GenerateQuarterlyAsync(year As Integer, quarter As Integer) As Task(Of IncomeStatementDto)
GenerateAnnualAsync(year As Integer) As Task(Of IncomeStatementDto)
GetPerProductMarginsAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of ProductMarginDto))
```

### IncomeStatementDto (Merchandising Format)
```
PeriodDescription As String                   ' "January 2026", "Q1 2026", "FY 2026"
StartDate, EndDate As DateTime

' Revenue Section
GrossSales As Decimal
SalesReturns As Decimal
SalesDiscounts As Decimal
NetSales As Decimal                           ' Gross - Returns - Discounts

' Cost Section
CostOfGoodsSold As Decimal                    ' FIFO-based COGS
GrossProfit As Decimal                        ' Net Sales - COGS
GrossMarginPercent As Decimal                 ' (GrossProfit / NetSales) × 100

' Expense Section
OperatingExpenses As Decimal                  ' Shrinkage + other
ShrinkageLoss As Decimal                      ' From shrinkage events

' Bottom Line
NetIncome As Decimal                          ' GrossProfit - OpEx
NetMarginPercent As Decimal                   ' (NetIncome / NetSales) × 100
```

### ProductMarginDto
```
ProductId, ProductName, Revenue, COGS, GrossProfit, GrossMarginPercent, UnitsSold
```

### Calculation Logic
- `COGS` = sum of `RevenueRecord.COGS` for the period (already FIFO-based from the sale event)
- `SalesReturns` = sum of refund amounts from return records
- `ShrinkageLoss` = sum of `ExpenseRecord` where Category = "Shrinkage"
- Per-product margins from `RevenueRecord` grouped by ProductId

## Acceptance Criteria

1. `dotnet build` succeeds
2. Merchandising P&L format correct: Sales → COGS → GP → OpEx → NI
3. FIFO-based COGS used
4. Per-product margin analysis available
5. Monthly/quarterly/annual views work
6. All percentages calculated correctly

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-04-summary.md`
