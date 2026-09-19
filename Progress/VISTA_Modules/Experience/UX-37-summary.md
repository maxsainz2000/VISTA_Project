---
module: MerchSys.App
agent: claude-code
date: 2026-06-06
plan-ref: Plans/VISTA_Modules/Experience/37-performance-perceived-performance.md
status: completed
---

## Task Summary

UX-37 — Performance & Perceived Performance: Virtualization, Async Coverage & Optimistic UI.

Three levers were applied: (A) container-recycling virtualization on all unbounded DataGrids,
(B) async-coverage audit of every growable view against the UX-15 standard, and (C) an
optimistic-UI pilot on the VAT settings write path.

**Plan:** `[[37-performance-perceived-performance]]`

## What Was Done

### A. Virtualization — `VirtualizingStackPanel.VirtualizationMode="Recycling"` enabled

Added the attribute to all unbounded/growable DataGrids:

| File | DataGrid | Collection |
|---|---|---|
| `Views/POS/TransactionHistoryView.xaml` | `TransactionGrid` | `Transactions` |
| `Views/Accounting/TamperAuditReportView.xaml` | *(unnamed)* | `Entries` |
| `Views/Purchasing/APLedgerView.xaml` | `APGrid` | `Entries` |
| `Views/Inventory/ProductPriceHistoryView.xaml` | `HistoryGrid` | `HistoryItems` |
| `Views/POS/CreditManagementView.xaml` | `AccountsDataGrid` | `Accounts` |
| `Views/Purchasing/PurchaseOrderListView.xaml` | `POGrid` | `Orders` |
| `Views/Purchasing/VendorDirectoryView.xaml` | `VendorGrid` | `Vendors` |
| `Views/Inventory/ShrinkageView.xaml` | *(unnamed)* | `HistoryItems` (shrinkage history) |
| `Views/Accounting/VatReturnView.xaml` | *(unnamed)* | `Lines` (VAT return line items) |

**Virtualization-killer audit (all growable transactional grids):** None of the nine grids above is wrapped in an outer `ScrollViewer`; each has a constrained height (parent Grid row `Height="*"`, DockPanel last-child fill, or fixed-size Window), so virtualization is active. No `Height="Auto"` ancestor defeats it.

**Report/dashboard grids — surveyed, intentionally skipped:** `SalesSummaryView.DailyBreakdown`, `IncomeStatementView.ProductMargins`, `FinancialOverviewView.TopProducts`, and `PurchasingDashboardView.TopVendors`/`.PendingPurchaseOrders` *do* sit inside a page-level `ScrollViewer` (the whole report scrolls as one surface — `CanContentScroll` is the default `False`, so these realize all rows). Recycling was **not** applied because their row counts are bounded by the report period (≤31 days) or a top-N projection, so full realization is acceptable. If any is rebound to an unbounded source, lift it out of the page `ScrollViewer` (or set `CanContentScroll="True"`) and enable recycling.

### B. Async Coverage Sweep

All growable views were audited against the UX-15 standard. All initial loads and refreshes were already routed through an async `LoadDataAsync` / `SearchAsync` / `LoadAsync` with `IsBusy` or `IsLoading`. No synchronous DB calls on the UI thread were found.

**Filter-debounce audit:**
- `TxNumberFilter` in `TransactionHistoryViewModel` → triggers `ApplyFilters()` (client-side in-memory filter, no debounce needed).
- `SearchText` in `VendorListViewModel` → triggers `ApplyFilter()` (client-side, no debounce needed).
- `DateFrom`/`DateTo` in `TransactionHistoryViewModel` → trigger `SearchAsync()` on discrete date-picker changes (not incremental keystroke), no debounce needed.
- No new incremental DB-querying filter was added; the existing 250 ms `CancellationTokenSource` debounce in `CommandPaletteViewModel` (UX-16) remains the reference implementation.

**No code changes required for async coverage** — all views were already compliant.

### C. Optimistic UI Pilot — `VatSettingsViewModel.SaveAsync`

**Pilot path chosen:** VAT settings update (`Pos_VatConfiguration`). Rationale: non-financial (configuration value for future transactions, not a ledger posting), has a `RowVersion` concurrency token, and is reversible. FIFO/financial paths were excluded per plan.

**Change:** `MerchSys.POS/ViewModels/VatSettingsViewModel.vb`

**Before:** `SaveAsync` called `ConcurrencyHelper.ExecuteWithConflictPromptAsync` with `AddressOf ReloadAsync` as the `onRefresh` callback. The form showed "Saving…" only after the await yielded, and on conflict the form fields were only reset by the DB reload (not explicitly).

**After:**
1. **Snapshot** — backing fields (`_isVatRegistered`, `_tin`, `_vatRatePercent`, `_percentageTaxRatePercent`, `_registeredBusinessName`, `_registeredAddress`) are captured before `IsSaving = True`.
2. **Optimistic status** — `StatusMessage = "Saving…"` is set immediately, before any `Await`, so the user sees feedback at once.
3. **Inline rollback lambda** — the `onRefresh` callback is now an `Async Function()` that:
   a. Restores snapshot values into VM backing fields + fires `OnPropertyChanged` for each
   b. Then calls `Await ReloadAsync()` to load the authoritative DB state
4. **Reconcile on success** — `StatusMessage = "VAT settings saved."` confirms the optimistic status; on validation rejection the status is cleared.
5. **Conflict-Cancel reconcile** — a `didRefresh` flag (set inside the rollback lambda) distinguishes the two conflict outcomes. On **Refresh**, the lambda runs and `ReloadAsync` sets "Settings loaded."; on **Cancel** the lambda never fires, so after the helper returns the optimistic "Saving…" is replaced with `"Not saved — data changed elsewhere."` (the form keeps the edits to retry). This closes a stuck-status gap where Cancel-at-conflict previously left "Saving…" on screen permanently.

**BC36943 safety:** The rollback lambda is a `Func(Of Task)` parameter called by `ConcurrencyHelper.ExecuteWithConflictPromptAsync` *after* its own `Try` block; no `Await` appears inside a `Catch`/`Finally`.

**Conflict proof:** On `DbUpdateConcurrencyException`, the primitive:
1. Sets `isConflict = True` inside `Catch` (no Await)
2. After the Try block: awaits `conflictPresenter.PromptAsync()` → shows "Data changed elsewhere — refresh and retry"
3. If operator chooses Refresh: calls the rollback lambda → form reverts to snapshot → `ReloadAsync` loads DB values
4. Returns `False` so the VM `If saved Then` branch is not entered — no success notification, no stale write kept.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | Pending (testing phase) |

## Issues Encountered

None. The `VirtualizingStackPanel.VirtualizationMode` attached property is a valid XAML attribute on `DataGrid`; all seven edits compiled without warnings. The `Async Function()` inline lambda in `VatSettingsViewModel` matches the existing pattern already used in `APLedgerViewModel`, `VendorListViewModel`, and others.

## What's Next

- [ ] Operator verification — scroll a long transaction history / AP ledger and confirm smooth scrolling without row-realization stutter.
- [ ] Force a concurrency conflict on VAT settings (two clients edit simultaneously) and verify the rollback lambda fires: form reverts to pre-save values, conflict prompt appears, Refresh loads DB values.
- [ ] If `ProductManagementViewModel` or the AP ledger payment path are identified as high-value optimistic UI targets in a future plan, apply the same snapshot/rollback recipe.

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`, `[[client-server-wpf]]`
- Agent Wiki entries consulted: `[[wpf-vista-state-feedback]]` (UX-15 async standard), `[[wpf-vista-command-palette]]` (UX-16 debounce reference)
- New pattern: `[[wpf-vista-performance]]`

## Codebase Wiki Discrepancies

None noted.
