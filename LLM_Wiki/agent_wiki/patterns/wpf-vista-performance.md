---
type: pattern
module: MerchSys.App
agent: claude-code
date: 2026-06-06
tags: [wpf, xaml, vb-net, mvvm, virtualization, async-load, optimistic-ui, performance, debouncing]
---

# Pattern: WPF VISTA Performance & Perceived Performance

## Context

UX-37 swept the codebase for three classes of performance risk as data grows without bound:
- **Virtualization** — long DataGrids realizing every row instead of recycling containers
- **Async coverage** — growable list views blocking the UI thread on initial load or refresh
- **Optimistic UI** — write paths that hold up the UI waiting for a DB round-trip before reflecting the user's change

## The Patterns

---

### 1. DataGrid Virtualization — `VirtualizationMode=Recycling`

**The risk:** WPF DataGrid has row virtualization on by default (`EnableRowVirtualization="True"`), but the default `VirtualizationMode` is `Standard`, which destroys and re-creates containers as rows scroll in and out. For growing lists (transaction history, audit log, AP ledger, price history), this causes jank at scroll time.

**The fix:** Set `VirtualizingStackPanel.VirtualizationMode="Recycling"` on every DataGrid whose bound collection can grow without bound:

```xml
<DataGrid ItemsSource="{Binding Transactions}"
          VirtualizingStackPanel.VirtualizationMode="Recycling"
          ...>
```

**Virtualization killers to watch for (check before adding a list):**

| Killer | Symptom | Fix |
|---|---|---|
| Outer `ScrollViewer` with `CanContentScroll="False"` (default) | DataGrid measures rows against infinite height; all rows realized | Remove outer ScrollViewer, or set `CanContentScroll="True"` |
| `Height="Auto"` on DataGrid or any ancestor Grid row | Unconstrained height → DataGrid measures with infinity | Give the Grid row `Height="*"` instead |
| Grouping with `IsVirtualizingWhenGrouping="False"` (default) | Grouping disables virtualization | Add `VirtualizingPanel.IsVirtualizingWhenGrouping="True"` |

**Audited and enabled (UX-37):**

| View | DataGrid Name | Collection | Verdict |
|---|---|---|---|
| TransactionHistoryView | `TransactionGrid` | `Transactions` (sales history) | UNBOUNDED — recycling enabled |
| TamperAuditReportView | *(unnamed)* | `Entries` (tamper incidents) | UNBOUNDED — recycling enabled |
| APLedgerView | `APGrid` | `Entries` (AP invoices) | GROWS — recycling enabled |
| ProductPriceHistoryView | `HistoryGrid` | `HistoryItems` (price changes) | UNBOUNDED — recycling enabled |
| CreditManagementView | `AccountsDataGrid` | `Accounts` (AR accounts) | GROWS — recycling enabled |
| PurchaseOrderListView | `POGrid` | `Orders` (purchase orders) | GROWS — recycling enabled |
| VendorDirectoryView | `VendorGrid` | `Vendors` (vendor list) | BOUNDED (~50) — recycling enabled for consistency |
| ShrinkageView | *(unnamed)* | `HistoryItems` (shrinkage history) | UNBOUNDED — recycling enabled |
| VatReturnView | *(unnamed)* | `Lines` (VAT return line items) | GROWS per period — recycling enabled |

None of the above grids is wrapped in an outer `ScrollViewer`; each virtualizes against a constrained parent height (`Height="*"` on the containing Grid row, or fixed-size Window with DockPanel last-child fill).

**Bounded report/dashboard grids — intentionally left in their outer `ScrollViewer` (not unbounded, so virtualization is moot):** `SalesSummaryView.DailyBreakdown` (≤31 rows/period), `IncomeStatementView.ProductMargins` and `FinancialOverviewView.TopProducts` (top-N), `PurchasingDashboardView.TopVendors` (top-N) and `.PendingPurchaseOrders` (naturally drained). These sit inside a page-level `ScrollViewer` by design (the whole report scrolls as one surface). Because their row counts are bounded by the report period / top-N projection, full realization is acceptable — recycling was **not** applied. If any of these is ever rebound to an unbounded source, move it out of the page `ScrollViewer` (or set `CanContentScroll="True"`) and enable recycling.

---

### 2. Async-Coverage Standard (UX-15 reuse)

All growable list views follow the UX-15 three-state load model. The canonical async load pattern:

```vb
Private Async Function LoadDataAsync() As Task
    IsError = False
    IsBusy = True        ' ← fires before any Await
    Try
        ' ... raw MySqlConnector reader or service call ...
        IsError = False
        LastLoadedAt = DateTime.Now
    Catch ex As Exception
        ErrorMessage = ex.Message
        IsError = True
    Finally
        IsBusy = False   ' ← always resets, even on error
    End Try
End Function
```

**Audit findings (UX-37):** All growable views were already compliant with UX-15. No synchronous DB calls on the UI thread were found. Key observations:

- **Client-side filters** (`ApplyFilters()` / `ApplyFilter()`) run synchronously but operate only on in-memory `List(Of T)` — no DB round-trip, so no debounce is needed.
- **DB-querying filters** (date range in `TransactionHistoryViewModel`) are triggered by discrete date-picker `PropertyChanged` events, not incremental keystroke events — no debounce needed.
- The **CommandPalette entity search** (UX-16) remains the only filter that hits the DB on each keystroke; its 250 ms debounce via `CancellationTokenSource` is the reference implementation for any future incremental DB filter.

**Debounce pattern (for future DB-triggered incremental filters):**

```vb
Private _searchCts As CancellationTokenSource

Private Async Sub OnQueryChanged(newQuery As String)
    If _searchCts IsNot Nothing Then
        _searchCts.Cancel()
        _searchCts.Dispose()
    End If
    _searchCts = New CancellationTokenSource()
    Dim token = _searchCts.Token
    Try
        Await Task.Delay(250, token)
        Await RunSearchAsync(newQuery, token)
    Catch ex As TaskCanceledException
        ' intentional cancellation — ignore
    End Try
End Sub
```

---

### 3. Optimistic UI with Rollback (UX-14 compatibility)

**The pattern:** Reflect a safe write immediately in the VM before the async DB call returns. If the DB call raises a `DbUpdateConcurrencyException`, roll back the optimistic state and surface the existing UX-14 "data changed elsewhere" prompt. **Never silently overwrite.**

**Pilot: `VatSettingsViewModel.SaveAsync` (VAT settings — non-financial, reversible)**

```vb
Public Async Function SaveAsync() As Task
    ValidateAllProperties()
    If HasErrors Then Return

    ' Snapshot current values for conflict rollback.
    Dim snapVatRegistered = IsVatRegistered
    Dim snapTin = Tin
    Dim snapVatRate = VatRatePercent
    ' ... (remaining fields)

    IsSaving = True
    StatusMessage = "Saving…"   ' ← optimistic: show immediately, before the await

    Dim request = ...  ' build from current VM props

    Try
        Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
            Async Function()
                result = Await _writer.UpdateAsync(request, CancellationToken.None)
            End Function,
            Async Function()
                ' Conflict rollback: restore snapshot into VM backing fields,
                ' fire notifications, then reload authoritative DB values.
                _isVatRegistered = snapVatRegistered
                _tin = snapTin
                ' ... (remaining fields)
                OnPropertyChanged(NameOf(IsVatRegistered))
                ' ... (remaining notifications)
                Await ReloadAsync()
            End Function,
            _conflictPresenter)

        If saved AndAlso result IsNot Nothing Then
            If result.Persisted Then
                StatusMessage = "VAT settings saved."
                _notifications.ShowSuccess("...")
            Else
                StatusMessage = String.Empty
                For Each errMsg In result.ValidationErrors
                    _validationErrors.Add(errMsg)
                Next
            End If
        End If
    Catch ex As Exception
        saveError = ex.Message
    Finally
        IsSaving = False
    End Try
End Function
```

**Key rules:**
1. Always snapshot backing fields (not computed properties) before modifying anything.
2. The rollback `Async Function()` sets backing fields directly then calls `OnPropertyChanged` — it does NOT call the public setters (which would call `SetProperty` and re-trigger validation / CanExecuteChanged).
3. `Await ReloadAsync()` after the field restore ensures the form shows the true DB state after rollback.
4. **BC36943 is NOT a risk here** — the `Async Function()` lambdas are `Func(Of Task)` parameters called by `ConcurrencyHelper` *after* its own Try block; no `Await` appears inside a `Catch`/`Finally`.
5. Do not apply this pattern to financial/inventory-decrement write paths (the FIFO `FOR UPDATE` path stays authoritative).

## Rules

- Enable `VirtualizingStackPanel.VirtualizationMode="Recycling"` on every DataGrid backed by a collection that can grow over time.
- Check for outer `ScrollViewer` / `Height="Auto"` / unconstrained ancestor before adding a new list — these silently kill virtualization.
- Client-side in-memory filters need no debounce. Debounce (250 ms, `CancellationTokenSource`) only when a filter keystroke triggers a DB call.
- Optimistic UI pilot is VAT settings only. Financial/FIFO write paths stay sequential (DB round-trip before display update).
- Roll back using backing fields + `OnPropertyChanged`, not public setters (avoids re-triggering validation).

## Related

- UX-15 (async load standard): `[[wpf-vista-state-feedback]]`
- UX-14 (concurrency conflict flow): `[[wpf-vista-state-feedback]]`
- UX-16 (debounce reference): `[[wpf-vista-command-palette]]`
