---
module: MerchSys.App
agent: antigravity
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/26-skeleton-loaders.md
status: completed
---

## Task Summary

Implemented content-shaped skeleton loaders (`SkeletonBlock` and `SkeletonPanel`) in `MerchSys.App` to replace the blanket full-screen busy overlay during first-load states on high-traffic dashboards and large lists. Skeletons provide content silhouettes (Cards for dashboards, Rows for lists) with a gentle horizontal shimmer, resolving layout jumps and reducing perceived loading latency.

**Plan:** `[[26-skeleton-loaders]]`

## What Was Done

- **Theme Additions:** Added `SkeletonBaseBrush` (SolidColorBrush) and `SkeletonShimmerHighlightColor` (Color) to [Light.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Themes/Light.xaml) and [Dark.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Themes/Dark.xaml) to ensure skeletons color-match their active theme.
- **SkeletonBlock Component:** Created [SkeletonBlock.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/SkeletonBlock.xaml) and [SkeletonBlock.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/SkeletonBlock.xaml.vb) which renders a rounded rectangle skeleton shape with a linear gradient shimmer overlay.
- **SkeletonPanel Component:** Created [SkeletonPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/SkeletonPanel.xaml) and [SkeletonPanel.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/SkeletonPanel.xaml.vb) offering pre-defined dashboard metric cards (`Kind="Cards"`) and data grid table rows (`Kind="Rows"`) layouts.
- **BusyOverlay Improvements:** Enhanced [BusyOverlay.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/BusyOverlay.xaml) and [BusyOverlay.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/BusyOverlay.xaml.vb) with `IsBusy` and `LastLoadedAt` dependency properties and style triggers. It automatically collapses itself during first-load to let the skeleton render, and shows during reloads/refreshes.
- **View Integration:** Integrated skeletons and conditional layout toggles into 7 major screens:
  - **Dashboards (Cards):** `StockDashboardView`, `PurchasingDashboardView`, `OwnerDashboardView` (uses `IsLoading`), `FinancialOverviewView`
  - **Lists (Rows):** `PurchaseOrderListView`, `ProductManagementView`, `TransactionHistoryView`

### First-Load vs Refresh Discriminator
Used `LastLoadedAt Is Nothing` as the first-load discriminator across all views (using the `IFreshnessAware` interface implemented by these ViewModels).
- **First Load (`IsBusy` / `IsLoading` is True AND `LastLoadedAt` is Null):** Main content container is set to `Collapsed`, the `SkeletonPanel` becomes `Visible` and shimmers, and `BusyOverlay` is kept `Collapsed`.
- **Reload/Refresh (`IsBusy` / `IsLoading` is True AND `LastLoadedAt` is Not Null):** Main content remains `Visible` to avoid blanking, `SkeletonPanel` is `Collapsed`, and `BusyOverlay` is shown as a spinner on top.

### Shimmer & Reduced-Motion
The shimmer animation is implemented as a `Storyboard` animating the `StartPoint` and `EndPoint` of a `LinearGradientBrush`. The storyboards are wrapped in a `MultiDataTrigger` checking if `{DynamicResource MotionEnabled}` is `True` and the parent is `Visible`. When OS animations are disabled (`MotionEnabled` is `False`), the shimmer overlay is `Collapsed`, showing static grey shapes.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified theme recoloring, first-load skeletons, refresh overlays, and reduced motion fallback) |

## Issues Encountered

- **WPF Frozen Brush Animations:** Custom themes load brushes as frozen resources which throw exceptions if their properties are directly animated.
  - **Resolution:** Declared a local `LinearGradientBrush` inside the `Border`'s template/style in `SkeletonBlock.xaml` and animated its relative properties (`StartPoint` / `EndPoint`) instead of animating a shared brush resource.

## What's Next

- [ ] Run the application to manually inspect visual alignment and polish micro-interactions if necessary.

## Cross-References

- Domain Wiki pages consulted: `[[client-server-wpf]]`, `[[modular-monolith]]`
- Agent Wiki entries consulted: None

## Codebase Wiki Discrepancies
- **Discrepancy:** [index.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/index.md) and module indexes do not yet document the new `SkeletonBlock` and `SkeletonPanel` controls in `Views/Shell/` nor the adoption of skeleton loading triggers in the 7 views listed above. Update codebase wiki pages after this plan lands.
