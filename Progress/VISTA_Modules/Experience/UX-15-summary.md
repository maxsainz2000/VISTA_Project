---
module: MerchSys.App
agent: claude-code
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/15-state-consistency-sweep.md
status: completed
---

# UX-15 — State Consistency Sweep: Error State + Full Loading/Empty Coverage

## Task Summary

Completed the three-state load model (loading / empty / error) across every data view and ViewModel.
Created the `ErrorStatePanel` control, added `IsError`/`ErrorMessage`/`IsEmpty` to 25 data VMs,
normalized all load paths to catch into `IsError`, and wired all 24 data views with `ErrorStatePanel`.

**Plan:** `[[15-state-consistency-sweep]]`

## State Precedence Rule (as implemented)

`IsBusy` (loading) **>** `IsError` (load threw) **>** `IsEmpty` (loaded, count=0) **>** content.

Encoded as:
- `IsBusy` setter fires `OnPropertyChanged(NameOf(IsEmpty))`
- `IsError` setter fires `OnPropertyChanged(NameOf(IsEmpty))`
- `IsEmpty = collection.Count = 0 AndAlso Not IsBusy AndAlso Not IsError`

A load failure sets `IsError = True` which makes `IsEmpty = False` — the error panel shows, not the
empty panel. On retry (Retry button → RefreshCommand), `IsError = False` is cleared at load start.

## What Was Done

### New Control
- **Created** `Views/Shell/ErrorStatePanel.xaml` — warning icon (DangerBrush), headline, message, Retry button. Mirrors `EmptyStatePanel` structure. DependencyProperties: `Title` (string), `Message` (string), `RetryCommand` (ICommand).
- **Created** `Views/Shell/ErrorStatePanel.xaml.vb` — code-behind with 3 DependencyProperty registrations.

### ViewModels Updated (25 VMs)

All data VMs received `IsError As Boolean`, `ErrorMessage As String`, `IsEmpty As Boolean` (computed).
The `IsBusy` (or `IsLoading`) setter now fires `OnPropertyChanged(NameOf(IsEmpty))`.

**Load path normalization (no Catch → added):**
- `Inventory/ProductManagementViewModel.LoadDataAsync` — was Try/Finally only (crash on error)
- `Inventory/ShrinkageViewModel.LoadDataAsync` — was Try/Finally only
- `Inventory/ExpiryMonitorViewModel.LoadDataAsync` — was Try/Finally only
- `Purchasing/VendorListViewModel.LoadDataAsync` — was Try/Finally only
- `Purchasing/GoodsReceivingViewModel.LoadSubmittedPOsAsync` — was Try/Finally only
- `Accounting/SalesSummaryViewModel.LoadDataAsync` — was Try/Finally only
- `Accounting/IncomeStatementViewModel.LoadDataAsync` — was Try/Finally only
- `Accounting/VatReliefReportViewModel.LoadAsync` — was Try/Finally only
- `Accounting/FinancialOverviewViewModel.LoadDataAsync` — was Try/Finally only

**Load path normalization (StatusMessage catch → IsError):**
- `Purchasing/APLedgerViewModel.LoadDataAsync`
- `Purchasing/PurchaseOrderListViewModel.LoadDataAsync`
- `Purchasing/ReorderSuggestionsViewModel.LoadDataAsync`
- `Purchasing/VendorCatalogViewModel.LoadVendorsAsync`
- `POS/TransactionHistoryViewModel.SearchAsync`
- `POS/DailySummaryViewModel.LoadAsync`
- `POS/CreditManagementViewModel.LoadDataAsync`
- `POS/VatSettingsViewModel.ReloadAsync`
- `Accounting/VatReturnViewModel.GenerateAsync`

**Load path normalization (swallow catch → IsError):**
- `Inventory/StockDashboardViewModel.LoadDataAsync`
- `Inventory/ProductPriceHistoryViewModel.LoadHistoryAsync`

**Load path normalization (post-Finally variable → IsError):**
- `Purchasing/PurchasingDashboardViewModel.LoadDataAsync`
- `App/OwnerDashboardViewModel.RefreshAsync`

**Fully unguarded → Try/Catch/Finally added:**
- `Accounting/TamperAuditReportViewModel.LoadAsync` — no Try block at all (IsLoading left True on crash)

**SalesCartViewModel** — added IsError/ErrorMessage; SearchProductsAsync catches into IsError.

**Filter method notifications** — `OnPropertyChanged(NameOf(IsEmpty))` added to `ApplyFilters()`/`ApplyFilter()` in 8 filter-driven VMs so IsEmpty updates when client-side filter changes collection count without changing IsBusy:
ProductManagementVM, StockDashboardVM, ShrinkageVM, APLedgerVM, PurchaseOrderListVM,
VendorListVM, ReorderSuggestionsVM, CreditManagementVM.

### Views Updated (24 views)

All views received `<views:ErrorStatePanel>` as a peer of `BusyOverlay` in the root Grid.
`EmptyStatePanel` bindings updated from raw `Items.Count = 0` DataTrigger to:
- **Simple case (load-only collections):** `Visibility="{Binding IsEmpty, Converter={StaticResource BoolToVis}}"`
- **Filter-driven or multi-section:** kept raw Count DataTrigger + added IsError/IsBusy collapse DataTriggers

`OwnerDashboardView` — the UX-06 gap: added `ErrorStatePanel` (uses existing manual loading Border for busy state; no EmptyStatePanel — dashboard shows KPIs, not a collection).

## Per-View Coverage Table

| View | Busy | Empty | Error | Notes |
|---|---|---|---|---|
| ProductManagementView | ✅ IsBusy | ✅ IsEmpty | ✅ LoadDataCommand | Categories tab: raw Count + guards |
| StockDashboardView | ✅ IsBusy | ✅ IsEmpty | ✅ RefreshCommand | |
| ShrinkageView | ✅ IsBusy | ✅ IsEmpty | ✅ LoadDataCommand | |
| ExpiryMonitorView | ✅ IsBusy | ✅ per-tab Count+guards | ✅ RefreshCommand | Two tabs |
| ProductPriceHistoryView | ✅ IsBusy | ✅ IsEmpty | ✅ (no Retry — popup) | Window, no load cmd |
| APLedgerView | ✅ IsBusy | ✅ IsEmpty | ✅ RefreshCommand | |
| PurchaseOrderListView | ✅ IsBusy | ✅ IsEmpty | ✅ RefreshCommand | |
| VendorDirectoryView | ✅ IsBusy | ✅ IsEmpty | ✅ RefreshCommand | |
| GoodsReceivingView | ✅ IsBusy | n/a | ✅ LoadPOsCommand | No EmptyStatePanel for PO list |
| ReorderSuggestionsView | ✅ IsBusy | ✅ IsEmpty (Suggestions) | ✅ RefreshCommand | Config tab: raw Count+guards |
| VendorCatalogView | ✅ IsBusy | ✅ MultiDataTrigger+guards | ✅ LoadVendorsCommand | |
| PurchasingDashboardView | ✅ IsBusy | ✅ IsEmpty | ✅ RefreshCommand | |
| TransactionHistoryView | ✅ IsBusy | ✅ IsEmpty (search results) | ✅ SearchCommand | Detail "no selection" panel untouched |
| DailySummaryView | ✅ IsBusy | n/a | ✅ LoadCommand | No list to be empty |
| SalesCartView | ✅ IsBusy | ✅ cart (raw Count, unchanged) | ✅ (search error) | Cart empty ≠ load error |
| CreditManagementView | ✅ IsBusy | n/a | ✅ LoadDataCommand | "Select account" panel untouched |
| VatSettingsView | ✅ IsSaving (save only) | n/a | ✅ ReloadCommand | Load has no BusyOverlay |
| SalesSummaryView | ✅ IsBusy | ✅ DailyBreakdown Count+guards | ✅ LoadCommand | DailyBreakdown ≠ TransactionCount |
| IncomeStatementView | ✅ IsBusy | ✅ IsEmpty | ✅ LoadCommand | |
| VatReturnView | ✅ IsBusy | ✅ IsEmpty | ✅ GenerateCommand | |
| VatReliefReportView | ✅ IsLoading | ✅ IsEmpty | ✅ RefreshCommand | Uses IsLoading |
| TamperAuditReportView | ✅ IsLoading | ✅ HasNoEntries (updated) | ✅ LoadCommand | HasNoEntries now excludes IsError |
| FinancialOverviewView | ✅ IsBusy | n/a | ✅ RefreshCommand | Dashboard, no primary list |
| OwnerDashboardView | ✅ Border (manual) | n/a | ✅ RefreshCommand | UX-06 gap; KPI dashboard |

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | Pending operator test (force DB unreachable to trigger load error) |

## Load Failure Manual Repro

To verify ErrorStatePanel appears:
1. Stop MariaDB service on host laptop
2. Launch the app and navigate to any module view (e.g. AP Ledger)
3. **Expected:** BusyOverlay shows briefly, then ErrorStatePanel appears with "Load Failed" and error message; EmptyStatePanel is NOT visible
4. Restart MariaDB, click "Retry"
5. **Expected:** BusyOverlay shows, data loads, ErrorStatePanel disappears, content or EmptyStatePanel shows

## Issues Encountered

- **BC36943 guard:** Several VMs use the captured-variable pattern (`Dim errMsg As String = Nothing` → set in Catch → check after `End Try`) to avoid `Await` in Catch. `IsError = True` is set after the Try block for these. This is correct and safe.
- **ExpiryMonitor two-tab issue:** `IsEmpty` is the combined condition (both tabs empty). Individual tab panels use raw Count triggers + IsError/IsBusy collapse guards to independently manage their visibility.
- **CollectionChanged vs IsBusy notification:** Filter-driven VMs were updated with explicit `OnPropertyChanged(NameOf(IsEmpty))` calls in their `ApplyFilters()` methods to ensure IsEmpty re-evaluates when client-side filtering changes collection count.
- **TamperAuditReportViewModel:** Had NO Try block at all — `IsLoading` could remain True forever on exception. Added Try/Catch/Finally.

## Codebase Wiki Discrepancies

- New file: `Views/Shell/ErrorStatePanel.xaml` (+ `.vb`) — not in codebase_wiki yet
- All 25 data VMs now have `IsError`, `ErrorMessage`, `IsEmpty` properties — codebase_wiki not yet updated

## What's Next

- [ ] Operator verification: force load failure per module (per the repro above) to confirm ErrorStatePanel shows in both themes
- [ ] codebase_wiki sync (Antigravity) after commit

## Cross-References

- Plan: `Plans/VISTA_Modules/Experience/15-state-consistency-sweep.md`
- Pattern updated: `LLM_Wiki/agent_wiki/patterns/wpf-vista-state-feedback.md`
- Depends on: UX-06 (`BusyOverlay`, `EmptyStatePanel`), UX-11 (layout rules)
