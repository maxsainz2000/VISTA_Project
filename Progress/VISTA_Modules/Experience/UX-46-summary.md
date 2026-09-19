---
module: MerchSys.App
agent: antigravity
date: 2026-06-10
plan-ref: Plans/VISTA_Modules/Experience/46-comparative-income-statement.md
status: completed
---

## Task Summary

Implemented a comparative layout for the Income Statement (Profit & Loss) view and fixed several readability defects on the same screen:
1. **Prior-period comparison column + Δ**: Added an extra column in the UI for the prior period's numbers, which are already computed by the view model, and wired standard delta indicators (percent change + direction) for each line.
2. **COGS legibility**: Un-muted the Cost of Goods Sold line to make its magnitude clear.
3. **Disambiguated OpEx roll-up**: Structured operating expenses by showing the components first (indented "Other Operating Expenses" and "Shrinkage Loss"), followed by the subtotal "Total Operating Expenses".
4. **Document-width layout**: Centered and constrained the P&L grid to a beautiful paper layout (`MaxWidth="800" HorizontalAlignment="Center"`), preserving responsiveness.
5. **Friendly month labels**: Exposed a `{Number, Name}` list (`MonthOption`) to combo-box bindings, displaying month names (e.g., "June") instead of raw numbers without breaking the integer-value round-trip.
6. **Export parity**: Extended both CSV and PDF exports to output the comparative prior-period values and percentage changes.

**Plan:** `[[46-comparative-income-statement.md]]`
**Branch:** N/A (Directly on workspace)

## What Was Done

- Modified [IncomeStatementViewModel.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/ViewModels/IncomeStatementViewModel.vb):
  - Defined nested `MonthOption` class.
  - Updated `AvailableMonths` to return list of `MonthOption` options.
  - Added new read-only properties for prior values: `PrevPeriodLabel`, `PrevNetSalesDisplay`, `PrevCOGSDisplay`, `PrevGrossProfitDisplay`, `PrevOtherOperatingExpensesDisplay`, `PrevShrinkageLossDisplay`, `PrevOperatingExpensesDisplay`, `PrevNetIncomeDisplay`, `OtherOperatingExpensesDisplay`, `PrevGrossMarginPercentDisplay`, `PrevNetMarginPercentDisplay`.
  - Added delta values and visibility properties: `NetSalesDelta`, `ShowNetSalesDelta`, `COGSDelta`, `ShowCOGSDelta`, `GrossProfitDelta`, `ShowGrossProfitDelta`, `OtherOperatingExpensesDelta`, `ShowOtherOperatingExpensesDelta`, `ShrinkageLossDelta`, `ShowShrinkageLossDelta`, `OperatingExpensesDelta`, `ShowOperatingExpensesDelta`, `NetIncomeDelta`, `ShowNetIncomeDelta`.
  - Implemented `CalculateDelta` helper function to compute percentage changes relative to the baseline, defaulting to hidden delta when the prior baseline is zero/missing.
  - Updated `LoadDataAsync` to extract values from `prevResult` and populate the new properties.
- Modified [IncomeStatementView.xaml](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml):
  - Replaced the month selector combo box with one utilizing `SelectedValuePath="Number"` and `DisplayMemberPath="Name"`.
  - Redesigned the main P&L grid to center and constrain to a paper width (`MaxWidth="800"` inside a centered parent container).
  - Re-mapped the grid columns to four columns (**Line Item | Current | Prior | Change**).
  - Un-muted the COGS row styling by switching from `Muted` styles to regular `LineLabel`/`LineAmount` styles.
  - Re-structured the Operating Expenses section to show "Other Operating Expenses" and "Shrinkage Loss" first, followed by the "Total Operating Expenses" subtotal.
  - Wired `views:DeltaIndicator` controls for Net Sales, COGS, Gross Profit, Other Operating Expenses, Shrinkage Loss, Total Operating Expenses, and Net Income, mapping `InvertSemantics` correctly for expense lines.
- Modified [IncomeStatementView.xaml.vb](file:///C:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml.vb):
  - Updated the CSV builder `BuildIncomeStatementCsv` to include the prior-period amounts and delta percentages.
  - Redesigned the plain-text PDF report builder `BuildIncomeStatementReport` to support a wider comparative layout (72-column grid) with dynamic headers, prior columns, and aligned delta values.
  - Added `FormatDeltaPercent` and `FormatReportLine` formatting helpers.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ |

All code successfully compiles with **0 warnings and 0 errors**.

## Issues Encountered

None. The design system's tokens and dynamic resources integrated seamlessly.

## What's Next

No pending items remain for this plan.

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`, `[[wpf-vista-formatting]]`
- Agent Wiki entries consulted: `[[wpf-vista-trend-indicators]]`, `[[wpf-vista-print-export]]`
