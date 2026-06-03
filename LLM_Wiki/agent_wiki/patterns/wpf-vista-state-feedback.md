---
type: pattern
module: MerchSys.App
agent: claude-code
date: 2026-06-03
updated: 2026-06-03
tags: [wpf, mvvm, vb-net, state, feedback, busy, empty, concurrency, validation, notifications]
---

# WPF VISTA State & Feedback Conventions

## Context

UX-06 made VISTA tell the operator *what is happening*: loading vs empty vs error are now
visually distinct, write failures are surfaced (not silent), input is validated inline, and the
**optimistic-concurrency conflict UX mandated by `CLAUDE.md`** is implemented. VISTA runs up to 4
client laptops against one centralized MariaDB instance, so write conflicts are a *normal* runtime
condition — not an edge case — and must never silently overwrite another client's change.

This entry documents the four conventions established by UX-06 so later work reuses them instead of
re-inventing per-view feedback. (Foundation for UX-14 concurrency consolidation and UX-15 state sweep.)

## The Pattern

### 1. Busy / Empty state (`IsBusy` + `IsEmpty`)

Two reusable shell controls in `Views/Shell/`, overlaid on a view's root `Grid`:

- **`BusyOverlay`** — tokenized dimmed spinner, bound to a VM `IsBusy`/`IsLoading` flag. Set
  `IsBusy = True` before the first `Await` of a load/save, reset it in a `Finally`.
- **`EmptyStatePanel`** — icon + headline + hint, shown when a *loaded* collection is empty so
  "no data" is never confused with "still loading". Drive its visibility from
  `IsEmpty = (Count = 0) AndAlso Not IsBusy`.

> A view with a `DockPanel` root must wrap that `DockPanel` in a parent `Grid` so the overlay can
> sit on top — overlays are the last child of a `Grid`, not docked.

### 2. Concurrency-conflict UX (`IConflictPresenter` — never overwrite)

On `DbUpdateConcurrencyException` (an optimistic-concurrency token —
`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`,
`Pos_CreditAccounts.OutstandingBalance`, … — was changed by another client):

- `IConflictPresenter.PromptAsync()` shows the modal `ConcurrencyConflictPrompt`
  ("Data changed elsewhere — refresh and retry") and returns `True` for **Refresh**, `False` for
  **Cancel**. There is **no force-overwrite** path.
- On Refresh, **re-query live values** (the load command) and re-bind. Never call `SaveChanges`
  again with the stale original values.

The canonical primitive is `SharedKernel.Persistence.ConcurrencyHelper.ExecuteWithConflictPromptAsync(work, onRefresh, presenter)`.
UX-14 consolidated all write-path VMs onto this primitive (the previous per-VM inline pattern
has been eliminated). The adopted shape (Option A — VM outer Try, primitive for conflict only):

```vb
IsBusy = True
Dim domainError As Boolean = False
Dim domainErrorMessage As String = String.Empty

Try
    Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
        Async Function() Await _service.MutateAsync(...),  ' mutation only — no success path
        Async Function()                                    ' on conflict → Refresh
            Await LoadDataAsync()
        End Function,
        _conflictPresenter)

    If saved Then
        Await LoadDataAsync()                              ' success refresh
        _notifications.ShowSuccess("...")
    End If
Catch ex As InvalidOperationException                      ' domain errors propagate out of work
    domainError = True
    domainErrorMessage = ex.Message
Finally
    IsBusy = False
End Try

If domainError Then ...
```

**Why Option A:** The `work` lambda contains only the mutation; the success path runs in `If saved Then`
inside the outer `Try`. Non-concurrency exceptions from `work` propagate to the outer `Catch`, so
each VM keeps its own domain-error UX. The primitive never swallows non-concurrency exceptions.

> **BC36943 — `Await` is illegal inside a `Catch`/`Finally`.** The primitive already captures a
> flag internally and awaits the prompt after its own Try block. The VM outer Try is safe (no
> Await in Catch/Finally). See `[[feedback-vbnet-await-catch]]`.

### 3. Toast feedback (`INotificationService`)

`INotificationService` (`SharedKernel/Interfaces`) wraps `Notification.Wpf`:
`ShowSuccess` / `ShowError` / `ShowInfo` / `ShowWarning`. Route every major write outcome (sale,
payment, receive, save settings, delete) through it instead of ad-hoc message boxes. Registered as a
singleton alongside `IConflictPresenter` in `Application.xaml.vb`.

### 4. Inline validation (`ObservableValidator`)

Input-heavy VMs inherit `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator`, decorate
properties with data-annotation attributes (`Required`, `Range`, `MinLength`), and report errors via
`INotifyDataErrorInfo`. The primary action's `CanExecute` gates on `Not HasErrors`, and the save body
guards with `ValidateAllProperties()` + `If HasErrors Then Return`. The tokenized
`Validation.ErrorTemplate` renders a red adorner + tooltip — no per-view error `TextBlock`.

## Why It Works

State, conflict, feedback, and validation are each one shared mechanism wired the same way
everywhere, so the safety-critical rule (never silently overwrite) is enforced in one place and the
three "what's happening?" states stay visually unambiguous across every view.

## Rules

- **Never silently overwrite.** A `DbUpdateConcurrencyException` is always surfaced + prompted; the
  stale write is dropped, never retried with old original values.
- **Never `Await` in `Catch`/`Finally`** (BC36943) — the primitive handles this; the VM outer Try
  has no `Catch` for `DbUpdateConcurrencyException` at all.
- **`IsEmpty` excludes `IsBusy`** — empty must mean "loaded and empty", never "still loading".
- **Owner is read-only.** Owner-facing views get loading/empty only — no conflict/validation/save UI
  (there are no writes); enforced at the data layer (OWASP-DA5), not just hidden in the UI.
- **Tokens only** — all state UI uses UX-00 tokens / UX-05 components; no inline hex; honors the live
  light/dark swap.
- **Use the canonical primitive** — every write path routes through
  `ConcurrencyHelper.ExecuteWithConflictPromptAsync`; do not re-grow per-VM variants.
  Add `Imports MerchSys.SharedKernel.Persistence` and inject `IConflictPresenter` via constructor.

## Wired surface (post UX-14)

**Concurrency (via primitive — all write paths on RowVersion tables):**
- POS: `SalesCartViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`, `TransactionHistoryViewModel`
- Purchasing: `APLedgerViewModel`, `GoodsReceivingViewModel`, `PurchaseOrderListViewModel`, `ReorderSuggestionsViewModel`, `VendorListViewModel`
- Inventory: `ProductManagementViewModel`, `ShrinkageViewModel`

**Not guarded (no concurrency token on the target table):**
- `ReorderSuggestionsViewModel.SaveConfigAsync/DismissAsync` — `Pur_ReorderConfigs`/`Pur_ReorderSuggestions` (no RowVersion)
- `VendorCatalogViewModel` — `Pur_VendorProducts` (no RowVersion)

**Validation:** `ProductManagementViewModel`, `ShrinkageViewModel`, `GoodsReceivingViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`.

**Busy/Empty:** ~23 list/grid/dashboard views across all four modules.

## Related

- Patterns: `[[wpf-vista-theming-conventions]]`, `[[mariadb-pure-client-server-architecture]]`
- Antipatterns: `[[feedback-vbnet-await-catch]]`
