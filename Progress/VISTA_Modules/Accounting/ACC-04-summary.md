---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-06
plan-ref: Plans/VISTA_Modules/Accounting/04-income-statement-service.md
status: completed
---

## Task Summary

Implemented the merchandising-format Income Statement (P&L) service for the Accounting module. Provides monthly, quarterly, annual, and arbitrary date-range views of Net Sales → COGS → Gross Profit → Operating Expenses → Net Income, with FIFO-based COGS and per-product margin analysis.

**Plan:** `[[04-income-statement-service]]`

## What Was Done

- Created `src/MerchSys.Accounting/Services/IIncomeStatementService.vb` — interface with `GenerateAsync`, `GenerateMonthlyAsync`, `GenerateQuarterlyAsync`, `GenerateAnnualAsync`, `GetPerProductMarginsAsync`; also defines `IncomeStatementDto` (merchandising P&L format) and `ProductMarginDto`
- Created `src/MerchSys.Accounting/Services/IncomeStatementService.vb` — full implementation backed by `AccountingDbContext`; private `BuildStatementAsync` helper handles all aggregation; delegates period-label formatting to each public method

## Calculation Notes

| Field | Source |
|---|---|
| `GrossSales` | `Sum(RevenueRecord.GrossAmount)` |
| `SalesDiscounts` | `Sum(RevenueRecord.DiscountAmount)` |
| `SalesReturns` | `Sum(ExpenseRecord.Amount)` where `Category = "Return"` (will be 0 until a return handler posts that category) |
| `NetSales` | `GrossSales - SalesReturns - SalesDiscounts` |
| `CostOfGoodsSold` | `Sum(RevenueRecord.COGS)` — already FIFO-costed at sale time |
| `GrossProfit` | `NetSales - COGS` |
| `ShrinkageLoss` | `Sum(ExpenseRecord.Amount)` where `Category = "Shrinkage"` |
| `OperatingExpenses` | `ShrinkageLoss + Sum(ExpenseRecord.Amount where Category = "Operating")` |
| `NetIncome` | `GrossProfit - OperatingExpenses` |

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- ACC-05 or subsequent Accounting plans
- `IIncomeStatementService` is not yet registered in DI — registration should be added in `MerchSys.App` when the service is wired to a ViewModel

## Cross-References

- Domain Wiki pages consulted: `[[accounting-module-paper]]`, `[[fifo-costing]]`
- Codebase Wiki consulted: `[[accounting/index]]`, `[[accounting/data-access]]`, `[[accounting/entities]]`
