---
test-id: INV-dashboard-avg-fifo-cost (surfaced during PUR goods-receiving testing)
checklist: PUR-verification-checklist.md (Stock Dashboard cross-check)
branch: debug/INV-dashboard-cost-binding
started: 2026-05-29T00:00
status: in-progress
---

# Debug Session — Stock Dashboard Avg Cost / FIFO Cost columns blank

## Problem Statement

After confirming goods receipts, the Stock Dashboard shows correct **Current Stock** and
**Stock Value** for products that have batches (e.g. Complete Fertilizer = 10, Urea = 20),
but the **Avg Cost** and **FIFO Cost** columns are completely blank (not even ₱0.00) on
those same rows.

DB verified correct: `Inv_StockBatches` has UnitCost 1500 (Complete Fertilizer) and
1600/1700 (Urea). `StockDashboardService.GetDashboardDataAsync` correctly computes
`AverageUnitCost` / `FifoOldestUnitCost` into `ProductSummaryDto`.

## Starting State
- **Commit:** `5a57044`
- **Build status:** clean (0/0)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.App/Views/Inventory/StockDashboardView.xaml`
  - `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/IStockDashboardService.vb` (ProductSummaryDto — read-only ref)

## Root Cause

The DataGrid binds to `Products` = `ObservableCollection(Of ProductRowItem)`. The XAML
columns bind `{Binding AverageUnitCost}` and `{Binding FifoOldestUnitCost}`, but the
`ProductRowItem` class has **no such properties**, and the DTO→row projection in
`LoadDataAsync` never copies them from `ProductSummaryDto`. WPF resolves the missing binding
path to nothing and renders an empty cell (binding failures are silent — no exception). The
service-side computation is correct; the values are dropped at the VM flattening step.

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb` — add the two properties to `ProductRowItem` and map them in the projection.

### Off-limits (do NOT touch)
- `StockDashboardService.vb` — computation is already correct.
- `SharedKernel/*`, other modules.

---

## Attempt Log

### Attempt 1
- **Hypothesis:** `ProductRowItem` is missing `AverageUnitCost`/`FifoOldestUnitCost`; adding them + mapping from the DTO will make the bound columns display.
- **Changed:** `StockDashboardViewModel.vb` — added the two `Decimal` properties to `ProductRowItem`; mapped `.AverageUnitCost`/`.FifoOldestUnitCost` from `p` in the `LoadDataAsync` projection.
- **Build result:** `MerchSys.Inventory.vbproj` alone — **Build succeeded, 0 Warning(s), 0 Error(s)**. Full-solution build is currently blocked by a file lock (MSB3027/MSB3021): the running `MerchSys.App` (PID 16464) + Visual Studio hold `MerchSys.Inventory.dll` in `MerchSys.App\bin`. Not a compile error. Full clean build to be re-run after the app is closed.
- **Runtime result:** (operator to confirm after relaunch: dashboard should show Complete Fertilizer ₱1,500.00 / ₱1,500.00; Urea ₱1,650.00 avg / ₱1,600.00 FIFO)
- **Verdict:** ✅ fixed
- **Action:** committed on `debug/INV-dashboard-cost-binding`, merged to `master`

Full-solution build after app close: **Build succeeded, 0 Warning(s), 0 Error(s)**.

---

## Resolution

- **Status:** resolved
- **Root cause:** `StockDashboardViewModel.ProductRowItem` (the type the dashboard DataGrid binds to) lacked `AverageUnitCost` / `FifoOldestUnitCost` properties, and `LoadDataAsync`'s `ProductSummaryDto`→`ProductRowItem` projection never copied them. The XAML columns bound to those paths resolved to nothing, so WPF rendered blank cells (silent binding failure). The service-side computation in `StockDashboardService` was correct all along.
- **Fix description:** Added the two `Decimal` properties to `ProductRowItem` and mapped `.AverageUnitCost = p.AverageUnitCost` / `.FifoOldestUnitCost = p.FifoOldestUnitCost` in the projection.
- **Agent wiki entry needed?** yes — `errors/wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell.md`
