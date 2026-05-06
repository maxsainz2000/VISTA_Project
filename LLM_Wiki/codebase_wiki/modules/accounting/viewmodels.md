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

## Support Classes

| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb` | `TrendBarItem` | Flat item for the 6-month trend bar chart. Contains pre-computed bar heights for UI normalization. |
