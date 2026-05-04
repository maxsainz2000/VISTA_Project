---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/10-view-stock-dashboard.md
status: completed
---

## Task Summary

Implemented the Stock Dashboard View — the primary Inventory screen. Provides real-time stock visibility across all products with FIFO-costed values, color-coded status, expiry alerts, and predictive stockout estimates. Driven by the existing `IStockDashboardService` (INV-05) and `IStockoutEstimationService` (INV-09).

**Plan:** `[[10-view-stock-dashboard]]`

## What Was Done

- Created `src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb` — ObservableObject ViewModel combining `StockDashboardDto` and `StockoutEstimateDto` into `ProductRowItem` rows. Implements category/status/text filtering, product detail drill-down, and 60-second auto-refresh via `System.Timers.Timer` + captured `SynchronizationContext`.
- Created `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml` — WPF UserControl with five summary cards, filter toolbar, color-coded DataGrid (DataTrigger row styles), color-coded status/expiry badge columns, and a bottom detail panel for batch list + recent movements.
- Created `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml.vb` — Code-behind with constructor injection, `SelectionChanged` handler that fires `SelectProductCommand`, and Escape key handler to clear the search box.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | N/A (UI testing in separate phase) |

## Issues Encountered

- **Issue:** `Dim _ = LoadDataAsync()` — VB.NET does not support `_` as a discard identifier (C# only).
  - **Resolution:** Renamed to `Dim initTask` / `Dim refreshTask`.

- **Issue:** `DispatcherTimer` (`System.Windows.Threading`) not available in the `net10.0` classlib project.
  - **Resolution:** Replaced with `System.Timers.Timer`. Captured `SynchronizationContext.Current` in the constructor (on the UI thread during DI resolution) and used `_uiContext.Post(...)` in the timer callback to marshal `LoadDataAsync()` back to the dispatcher thread.

- **Issue:** `_allProducts.Count(Function(...))` — VB.NET resolves `.Count` as the `List(Of T).Count` property, blocking the LINQ extension method overload.
  - **Resolution:** Changed to `.Where(Function(...)).Count()`.

- **Issue:** `Timer` ambiguous between `System.Threading.Timer` and `System.Timers.Timer` after adding `Imports System.Threading`.
  - **Resolution:** Used fully qualified `System.Timers.Timer` for the field declaration and constructor.

- **Issue:** XAML `MC3024` — `Style` property set twice on the StockStatus badge `TextBlock` (both as attribute and `<TextBlock.Style>` property element).
  - **Resolution:** Removed the redundant `Style="{StaticResource StatusBadge}"` attribute; retained `<TextBlock.Style BasedOn=...>`.

## What's Next

- INV-11 (or subsequent Inventory plans) — further inventory screens or features
- DI registration of `StockDashboardViewModel` and `StockDashboardView` in `Application.xaml.vb` (deferred to INFRA-02 or shell navigation plan)
- Agent Wiki: document the `System.Timers.Timer` + `SynchronizationContext` pattern for classlib ViewModels needing auto-refresh

## Cross-References

- Domain Wiki pages consulted: `entities/module-inventory.md`
- Services used: `IStockDashboardService`, `IStockoutEstimationService`
- Depends on: INV-05, INV-06, INV-09
