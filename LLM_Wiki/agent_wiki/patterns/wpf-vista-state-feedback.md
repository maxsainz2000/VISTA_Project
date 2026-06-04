---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-03
updated: 2026-06-04 (UX-18)
tags: [wpf, mvvm, vb-net, state, feedback, busy, empty, error, concurrency, validation, notifications]
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

### 1. Three-state load model (`IsBusy` / `IsError` / `IsEmpty`) — UX-15

Three reusable shell controls in `Views/Shell/`, overlaid on a view's root `Grid`:

- **`BusyOverlay`** — tokenized dimmed spinner, bound to `IsBusy`/`IsLoading`. Set True before the
  first `Await` of a load, reset in `Finally`.
- **`ErrorStatePanel`** — warning icon + headline + message + **Retry** button (added UX-15). Shown
  on load failure. Binds `RetryCommand` to the view's load/refresh command (read-only, idempotent).
  Title=`"Load Failed"`, Message=`{Binding ErrorMessage}`, Retry=`{Binding RefreshCommand}` (or
  `LoadCommand`, `LoadDataCommand`, etc.), Visibility=`{Binding IsError, Converter=BoolToVis}`.
- **`EmptyStatePanel`** — icon + headline + hint, shown when loaded and empty. Drive from
  `IsEmpty` (computed — excludes IsBusy **and** IsError so "error" is never shown as "no data").

**State precedence (mutually exclusive):**
`IsBusy` (loading) > `IsError` (load threw) > `IsEmpty` (loaded, count=0) > content panel.

**VM state model — add to every data VM:**
```vb
' ─── Load State ──────────────────────────────────────────────────────────────

Private _isBusy As Boolean
Public Property IsBusy As Boolean
    Get : Return _isBusy : End Get
    Set(value As Boolean)
        SetProperty(_isBusy, value)
        OnPropertyChanged(NameOf(IsEmpty))  ' ← must fire IsEmpty notification
    End Set
End Property

Private _isError As Boolean
Public Property IsError As Boolean
    Get : Return _isError : End Get
    Set(value As Boolean)
        SetProperty(_isError, value)
        OnPropertyChanged(NameOf(IsEmpty))  ' ← must fire IsEmpty notification
    End Set
End Property

Private _errorMessage As String = String.Empty
Public Property ErrorMessage As String
    Get : Return _errorMessage : End Get
    Set(value As String) : SetProperty(_errorMessage, value) : End Set
End Property

Public ReadOnly Property IsEmpty As Boolean
    Get
        Return TheCollection.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
    End Get
End Property
```

**LoadDataAsync pattern — normalize ALL load paths:**
```vb
Private Async Function LoadDataAsync() As Task
    IsError = False
    IsBusy = True
    Try
        ' ... load data ...
        IsError = False   ' clear on success
    Catch ex As Exception
        ErrorMessage = ex.Message
        IsError = True
    Finally
        IsBusy = False    ' always resets
    End Try
End Function
```

**BC36943 trap:** Never `Await` inside a `Catch`/`Finally`. If error handling after the Try block
needs async work, use the captured-variable pattern (see `[[feedback-vbnet-await-catch]]`).

**Filter-driven VMs:** If `ApplyFilters()` modifies the collection without changing `IsBusy`,
add `OnPropertyChanged(NameOf(IsEmpty))` at the end of `ApplyFilters()` so IsEmpty re-notifies.

**XAML wiring — simple case (single primary collection, no client-side filter):**
```xml
<views:EmptyStatePanel ... Visibility="{Binding IsEmpty, Converter={StaticResource BoolToVis}}"/>
<views:BusyOverlay ... Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}"/>
<views:ErrorStatePanel Title="Load Failed" Message="{Binding ErrorMessage}"
                       RetryCommand="{Binding RefreshCommand}"
                       Visibility="{Binding IsError, Converter={StaticResource BoolToVis}}"
                       HorizontalAlignment="Center" VerticalAlignment="Center"/>
```

**XAML wiring — filter-driven or multi-section (raw Count + collapse guards):**
```xml
<views:EmptyStatePanel ...>
    <views:EmptyStatePanel.Style>
        <Style TargetType="UserControl">
            <Setter Property="Visibility" Value="Collapsed"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding Items.Count}" Value="0">
                    <Setter Property="Visibility" Value="Visible"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding IsError}" Value="True">
                    <Setter Property="Visibility" Value="Collapsed"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding IsBusy}" Value="True">
                    <Setter Property="Visibility" Value="Collapsed"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </views:EmptyStatePanel.Style>
</views:EmptyStatePanel>
```

> A view with a `DockPanel` root must wrap that `DockPanel` in a parent `Grid` so overlays can
> sit on top — overlays are the last child of a `Grid`, not docked.
> VMs using `IsLoading` instead of `IsBusy` (VatReliefReportVM, TamperAuditReportVM) — bind
> BusyOverlay to `IsLoading`; IsEmpty uses `Not IsLoading AndAlso Not IsError`.

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

### 4. Inline validation (`ObservableValidator`) — UX-18

Input-heavy VMs inherit `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator`, decorate properties with data-annotation attributes (`Required`, `Range`, `RegularExpression`), and report errors via `INotifyDataErrorInfo`. The primary action's `CanExecute` gates on `Not HasErrors`, and the save body guards with `ValidateAllProperties()` + `If HasErrors Then Return`. 

To standardize form presentation and avoid layout shifts, the following three-part UX-18 standard must be applied to all editable forms:

#### A. Shared Field Row Style (`FieldRowStyle`)
Wrap every editable input control inside a `HeaderedContentControl` in XAML, utilizing the shared `FieldRowStyle` defined in `Themes/Components.xaml`:
```xml
<HeaderedContentControl Header="Field Label" 
                        Style="{StaticResource FieldRowStyle}" 
                        helpers:FormHelper.IsRequired="True">
    <TextBox Text="{Binding FormProperty, UpdateSourceTrigger=PropertyChanged, NotifyOnValidationError=True}"
             Style="{StaticResource FormInput}"/>
</HeaderedContentControl>
```
*   **Asterisk Required Marker**: When `helpers:FormHelper.IsRequired="True"` is declared, a trailing red asterisk (`*`) is shown next to the label.
*   **Mnemonic Focus Target**: The template binds the label's `Target` to the inner `Content` control to enable keyboard Alt-mnemonic focus targeting (UX-17).
*   **Fixed Error Slot**: A fixed-height `18px` panel is reserved underneath the input slot, showing `Content.(Validation.Errors)[0].ErrorContent` when invalid. This prevents layout reflows and vertical jumping when errors toggle.
*   **Validation Bubble-up**: You **must** specify `NotifyOnValidationError=True` on the child input bindings so that error notifications propagate to the parent containers for the `ErrorSummary`.

#### B. Reusable Error Summary (`ErrorSummary`)
For long or multi-section forms (e.g. Product Editor, Purchase Order Editor), insert the collapsible `ErrorSummary` control at the top of the form layout:
```xml
<views:ErrorSummary TargetContainer="{Binding ElementName=EditorOverlayGrid}" Margin="0,0,0,12"/>
```
*   It aggregates all validation errors bubbled up in the visual tree of the specified `TargetContainer`.
*   Clicking an error button/link in the list automatically calls `Focus()` on the invalid element to guide the operator.

#### C. Numeric Input Affordances (`FormHelper.InputMode`)
To enforce input discipline, apply the attached property `helpers:FormHelper.InputMode` directly to `TextBox` elements:
*   `PositiveInteger`: Restricts entry to positive integers only (regex `^\d*$`), aligns text `Right`, and formats on `LostFocus` (falling back to `"0"`).
*   `PositiveDecimal`: Restricts entry to positive decimal numbers (up to 2 decimal places, regex `^\d*(\.\d{0,2})?$`), aligns text `Right`, and formats on `LostFocus` (e.g. `"12.00"` / `"0.00"`).
```xml
<TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, NotifyOnValidationError=True}"
         helpers:FormHelper.InputMode="PositiveDecimal"
         Style="{StaticResource FormInput}"/>
```

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

## Wired surface (post UX-15)

**Error state (`IsError`/`ErrorMessage` + `ErrorStatePanel`) — ALL data VMs and views (24 total):**
Every data view now has `ErrorStatePanel` wired to `IsError`. VMs normalize their load paths with
`IsError = True` in the catch, `IsError = False` on success, cleared at load start.

**Concurrency (via primitive — all write paths on RowVersion tables):**
- POS: `SalesCartViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`, `TransactionHistoryViewModel`
- Purchasing: `APLedgerViewModel`, `GoodsReceivingViewModel`, `PurchaseOrderListViewModel`, `ReorderSuggestionsViewModel`, `VendorListViewModel`
- Inventory: `ProductManagementViewModel`, `ShrinkageViewModel`

**Not guarded (no concurrency token on the target table):**
- `ReorderSuggestionsViewModel.SaveConfigAsync/DismissAsync` — `Pur_ReorderConfigs`/`Pur_ReorderSuggestions` (no RowVersion)
- `VendorCatalogViewModel` — `Pur_VendorProducts` (no RowVersion)

**Validation:** `ProductManagementViewModel`, `ShrinkageViewModel`, `GoodsReceivingViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`.

**Busy/Empty/Error:** 24 list/grid/report/dashboard views across all modules. `OwnerDashboardView`
was the missing UX-06 view — now has `ErrorStatePanel` (using existing manual loading Border).

## Related

- Patterns: `[[wpf-vista-theming-conventions]]`, `[[mariadb-pure-client-server-architecture]]`
- Antipatterns: `[[feedback-vbnet-await-catch]]`
