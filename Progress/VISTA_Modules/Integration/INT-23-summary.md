---
module: MerchSys.Integration
agent: antigravity
date: 2026-06-11
plan-ref: Plans/VISTA_Modules/Integration/23-viewmodel-data-access-and-timer-disposal.md
status: completed
---

# Implementation Progress Report: INT-23 ViewModel data-access layering + timer disposal

## Task Summary

Implemented MVVM hygiene improvements for data-access layering and timer disposal across multiple modules. All direct `MySqlConnection` usages inside ViewModels have been removed, delegating to services (with raw ADO.NET for full entities and EF projections for aggregate metrics). Refresh timers in auto-refresh ViewModels are now cleanly stopped and disposed when their corresponding Views are unloaded.

**Plan:** `[[23-viewmodel-data-access-and-timer-disposal.md]]`

## What Was Done

### Part 1 — Data-Access Layering
- **`IStockService.vb` + `StockService.vb`** — Added `GetProductsWithCategoriesAsync()` returning `ProductAndCategoryData` using raw ADO.NET for full entities.
- **`ProductManagementViewModel.vb`** — Injected `IStockService`, removed raw connection/reader code in `LoadDataAsync`, and replaced it with a call to `_stockService.GetProductsWithCategoriesAsync()`. Removed `MySqlConnector` import.
- **`ICreditService.vb` + `CreditService.vb`** — Added `GetCreditTransactionsAsync(accountId)` returning `List(Of CreditTransactionItem)` using raw ADO.NET. Moved `CreditTransactionItem` class to `ICreditService.vb` to make it accessible to the service layer.
- **`CreditManagementViewModel.vb`** — Removed local `CreditTransactionItem` class definition. Removed raw connection/reader code in `LoadHistoryInternalAsync` and replaced it with a call to `_creditService.GetCreditTransactionsAsync(account.Id)`. Removed `MySqlConnector` import.
- **`IPurchasingDashboardService.vb` + `PurchasingDashboardService.vb`** (New) — Created interface and implementation with `GetMonthlyTrendAsync(trendStart)` and `GetTopVendorsAsync()` returning DTOs using EF projections.
- **`PurchasingDashboardViewModel.vb`** — Injected `IPurchasingDashboardService`, removed local `TrendBarItem` and `TopVendorItem` definitions, and delegated trend and vendor queries to the dashboard service. Removed `MySqlConnector`, EF Core, and database context imports.
- **`PurchasingServiceCollectionExtensions.vb`** — Registered `IPurchasingDashboardService` as scoped in DI.
- **`PurchaseOrderListViewModel.vb`** — Cleaned up raw vendor database query and replaced it with `_vendorService.GetAllAsync()`. Removed unused DbContext and `MySqlConnector` imports.
- **`IDailySummaryService.vb` + `DailySummaryService.vb`** (Optional Task) — Added `GetDailySalesTrendAsync(startDate)` returning `Dictionary(Of DateTime, Decimal)` using raw ADO.NET.
- **`OwnerDashboardViewModel.vb`** (Optional Task) — Injected `IDailySummaryService`, removed `MySqlConnector`, `IConfiguration` constructor injection, and the raw SQL connection block in `LoadTrendDataAsync`. Replaced it with a call to `_dailySummary.GetDailySalesTrendAsync(startDate)`.

### Part 2 — Timer Disposal
- **`StockDashboardViewModel.vb`** — Implemented `IDisposable` to stop, detach from event, and dispose `_refreshTimer`.
- **`ExpiryMonitorViewModel.vb`** — Implemented `IDisposable` to stop, detach from event, and dispose `_refreshTimer`.
- **`FinancialOverviewViewModel.vb`** — Implemented `IDisposable` to stop, detach from event, and dispose `_refreshTimer`.
- **`StockDashboardView.xaml.vb`** — Wired up `Unloaded` event to cast `DataContext` to `IDisposable` and dispose it.
- **`ExpiryMonitorView.xaml.vb`** — Wired up `Unloaded` event to cast `DataContext` to `IDisposable` and dispose it.
- **`FinancialOverviewView.xaml.vb`** — Wired up `Unloaded` event to cast `DataContext` to `IDisposable` and dispose it.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ |

*Note: The solution built successfully with 0 errors and 0 warnings.*

## Issues Encountered

None. Refactoring went smoothly and all dependencies compiled correctly on the first try.

## What's Next

No outstanding tasks remain for this plan. All deliverables, including the optional one, have been implemented and validated.

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`
- Agent Wiki entries consulted: `[[efcore-vbnet-tolistasync-entity-empty]]`
