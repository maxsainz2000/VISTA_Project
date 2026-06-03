---
module: MerchSys.Purchasing
agent: antigravity
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/13-purchasing-dashboard.md
status: completed
---

# Implementation Progress Report — Purchasing Dashboard (UX-13)

This report documents the completion of the **Purchasing Dashboard (UX-13)** implementation to resolve the **module asymmetry** defect #7 identified in the UX-08 audit.

**Plan:** `Plans/VISTA_Modules/Experience/13-purchasing-dashboard.md`

## Task Summary

Implemented a new read-only **`PurchasingDashboardView`** and **`PurchasingDashboardViewModel`** to serve as the default landing view for the Purchasing module. The dashboard aggregates existing data from multiple entities/services within the Purchasing module (Outstanding AP, Overdue AP, Pending Deliveries, Reorder Suggestions, Active Vendors, and Average Lead Time) and renders a 6-month spend trend chart, a top 5 vendors by spend table, and a detailed pending deliveries grid.

## What Was Done

- Created `WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchasingDashboardViewModel.vb` — ViewModel aggregating KPIs, status counts, spend trend, top vendors, and pending PO detail rows. Includes raw MySQL ADO.NET reader loop logic to fetch monthly trends and top vendors safely without triggering the EF Core 10 VB.NET `ToListAsync` empty-list bug.
- Modified `WPF_Applications/MerchSys/src/MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` — Registered the new ViewModel in DI as transient.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Views/Purchasing/PurchasingDashboardView.xaml` — XAML view using themed design tokens, responsive FilterBar container, scroll rules, and layout overflow safety ScrollViewers.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Views/Purchasing/PurchasingDashboardView.xaml.vb` — Code-behind establishing DataContext and wiring the ViewModel's abstract string-based `NavigateToViewRequested` event to the shell navigation system to prevent circular reference compilation errors.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb` — Registered the new View in DI as transient.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/MainWindowViewModel.vb` — Added `PurchasingDashboardView` as the first/default navigation item in the Purchasing module for both Owner and Manager roles.
- Created Agent Wiki pattern documentation at `LLM_Wiki/agent_wiki/patterns/wpf-vista-purchasing-dashboard.md` and updated `index.md` + `log.md`.

## Final Metric Set & Data Sources

| Metric | Source |
|---|---|
| **Total Outstanding AP (Hero)** | Sourced via `IAccountsPayableService.GetTotalOutstandingAsync()` |
| **Overdue AP Amount & Count** | Sourced via `IAccountsPayableService.GetOverdueAsync()` |
| **Pending Deliveries Count** | Sourced via `IPurchaseOrderService.GetAllAsync(PurchaseOrderStatus.Submitted)` count |
| **Pending Reorders Count** | Sourced via `IReorderService.GetPendingSuggestionsAsync()` count |
| **Active Vendors Count** | Sourced via `IVendorService.GetAllAsync()` count |
| **Average Lead Time Days** | Calculated as the average of `DefaultLeadTimeDays` across all active vendors |
| **6-Month Spend Trend** | Raw SQL query summing `TotalAmount` from `Pur_PurchaseOrders` grouped by Year/Month |
| **Top 5 Vendors by Spend** | Raw SQL query joining `Pur_Vendors` and `Pur_PurchaseOrders` ordered by spend |
| **Pending & Overdue Deliveries Details** | Filtered active PO list from `IPurchaseOrderService.GetAllAsync()` (Submitted, Received, Verified) |

### AP Delta Omission
No delta is rendered for Outstanding AP because the database only tracks running balances on `AccountsPayableEntry` without individual payment transaction dates. This matches the behavior of the Accounting dashboard's AP Outstanding tile.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 warnings, 0 errors) |
| Unit tests pass | N/A (No test projects exist for this module) |
| Manual verification | ✅ (Verified namespace, DI registrations, layout, and compilation safety) |

## Issues Encountered

- **Issue:** Border and TextBlock duplicate style compilation errors (`MC3024`).
  - **Resolution:** Removed the redundant inline `Style` attribute on tags that already define nested `.Style` tags.
  - **Agent Wiki entry:** N/A (Standard WPF markup bug, resolved directly).

## Codebase Wiki Discrepancies
The following new classes will be indexed by the codebase wiki on the next sync:
- `Views.Purchasing.PurchasingDashboardView` (WPF UserControl View)
- `ViewModels.PurchasingDashboardViewModel` (Wvvm ViewModel)

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`, `[[client-server-wpf]]`
- Agent Wiki entries consulted: `[[wpf-vista-trend-indicators]]`, `[[wpf-vista-dashboard-layout]]`, `[[efcore-vbnet-tolistasync-entity-empty]]`
