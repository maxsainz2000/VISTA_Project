---
type: layer-manifest
module: MerchSys.Accounting
layer: ViewModels
last-updated: 2026-05-06
---

# MerchSys.Accounting — ViewModels

This page details the ViewModel implementations for the **MerchSys.Accounting** module.

## Core ViewModels

| File Path | Class | Key Responsibilities | Dependencies (DI) |
|---|---|---|---|
| `src/MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb` | `FinancialOverviewViewModel` | ViewModel for the Financial Overview Dashboard. Aggregates KPIs, 6-month trends, top products, and alerts. Includes a 5-minute auto-refresh timer and a `RefreshCommand`. | `IFinancialOverviewService`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/IncomeStatementViewModel.vb` | `IncomeStatementViewModel` | ViewModel for the Income Statement (P&L) view. Supports Monthly, Quarterly, and Annual periods. Provides formatted display strings for accounting lines, "What This Means" interpretation, and per-product margin breakdown. | `IIncomeStatementService`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/SalesSummaryViewModel.vb` | `SalesSummaryViewModel` | ViewModel for the Sales Summary View. Provides Daily/Weekly/Monthly breakdowns by payment method, KPI summary cards, and mandatory interpretation logic. | `ISalesSummaryService`, `IWhatThisMeansService` |

## Support Classes

| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb` | `TrendBarItem` | Flat item for the 6-month trend bar chart. Contains pre-computed bar heights for UI normalization. |
| `src/MerchSys.Accounting/ViewModels/IncomeStatementViewModel.vb` | `IncomeStatementPeriodType` | Enum: `Monthly`, `Quarterly`, `Annual`. |
| `src/MerchSys.Accounting/ViewModels/SalesSummaryViewModel.vb` | `SalesSummaryPeriodType` | Enum: `Daily`, `Weekly`, `Monthly`. |
