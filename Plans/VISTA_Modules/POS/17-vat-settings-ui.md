---
module: MerchSys.POS
plan-id: POS-17
title: "VAT Settings UI"
depends-on: [POS-14, INT-02]
estimated-files: 4
---

# VAT Settings UI

## Context

POS-14 introduced `VatConfiguration` (the `Pos_VatConfiguration` table with `IsVatRegistered`, `Tin`, `VatRate`, and related fields) and `VatConfigurationLoader` which caches the configuration in memory for the receipt and reporting paths. The 2026-05-11 POS audit notes that the **store owner has no UI to change these values** — they can only be modified by directly editing the SQLite row or running ad-hoc SQL. This is **Priority 3** for BIR compliance because a small business that is just over the threshold to register for VAT (or just under, dropping back to Percentage Tax) cannot make the transition without a developer present.

This plan delivers a single dedicated settings view, a view-model, the registration into the shell navigation, and the critical invalidation call so cached configuration in `VatConfigurationLoader` refreshes after a save.

## Prerequisites

- **POS-14** (VAT Configuration, Schema Extension & Calculation) — `VatConfiguration`, `IVatConfigurationLoader.Invalidate()`, `Pos_VatConfiguration` table
- **INT-02** (Shell Navigation & View Wiring) — shell navigation pattern, role gates

## Wiki References

- `concepts/bir-compliance.md` — VAT registration threshold (₱3M annual gross sales), TIN format
- `concepts/owasp-da-top10.md` — Manager-only access on financial configuration changes

## Deliverables

```
MerchSys.POS/ViewModels/
└── VatSettingsViewModel.vb                              ' New

MerchSys.POS/Services/
└── IVatConfigurationWriter.vb                           ' New — write-side abstraction
    (Same file: VatConfigurationWriter implementation)

MerchSys.App/Views/POS/
└── VatSettingsView.xaml + VatSettingsView.xaml.vb       ' New
```

The shell navigation entry is added alongside this plan; the additional dictionary entry is a one-line edit and is counted within the existing INT-02 surface (analogous to INT-13).

## Specification

### IVatConfigurationWriter

```
Public Interface IVatConfigurationWriter

    Function GetCurrentAsync() As Task(Of VatConfiguration)

    Function UpdateAsync(
        request As VatConfigurationUpdateRequest,
        cancellationToken As CancellationToken
    ) As Task(Of VatConfigurationUpdateResult)

End Interface

Public Class VatConfigurationUpdateRequest
    Public Property IsVatRegistered As Boolean
    Public Property Tin As String
    Public Property VatRate As Decimal                  ' 12% default; future-proof in case BIR changes
    Public Property PercentageTaxRate As Decimal        ' 3% default
    Public Property RegisteredBusinessName As String
    Public Property RegisteredAddress As String
End Class

Public Class VatConfigurationUpdateResult
    Public Property Persisted As Boolean
    Public Property ValidationErrors As IReadOnlyList(Of String)
    Public Property CacheInvalidated As Boolean
End Class
```

`UpdateAsync` behaviour:

1. Validate:
   - If `IsVatRegistered = True`, `Tin` is required and must match the BIR format `999-999-999-999` or `999-999-999-99999` (regex enforced; spec the regex inline).
   - `VatRate` between 0 and 1 exclusive (stored as a decimal fraction, e.g., `0.12`).
   - `PercentageTaxRate` between 0 and 1 exclusive.
   - `RegisteredBusinessName` and `RegisteredAddress` not null/whitespace.
2. If validation fails, return result with `Persisted = False` and the error list. Do not throw.
3. Open a `PosDbContext`, load the (singleton) `VatConfiguration` row, apply the updates, set `ModifiedAt`/`ModifiedBy`, `SaveChangesAsync`.
4. Call `_loader.Invalidate()` so the next receipt generation reads the new values.
5. Publish a `VatConfigurationChangedEvent` (define in `SharedKernel` if not already present from POS-14) — Accounting may want to react.
6. Return result with `Persisted = True`, `CacheInvalidated = True`.

Per the feedback memory: any `Try/Catch` around `SaveChangesAsync` must not `Await` inside the `Catch` block.

### VatSettingsViewModel

```
Public Class VatSettingsViewModel
    Inherits ObservableObject

    Public Sub New(writer As IVatConfigurationWriter, notifications As INotificationService)
        ' Load current state in OnLoadedAsync
    End Sub

    <ObservableProperty>
    Private _isVatRegistered As Boolean

    <ObservableProperty>
    Private _tin As String

    <ObservableProperty>
    Private _vatRatePercent As Decimal           ' Displayed as "12" not "0.12"

    <ObservableProperty>
    Private _percentageTaxRatePercent As Decimal ' Displayed as "3" not "0.03"

    <ObservableProperty>
    Private _registeredBusinessName As String

    <ObservableProperty>
    Private _registeredAddress As String

    <ObservableProperty>
    Private _validationErrors As ObservableCollection(Of String)

    <ObservableProperty>
    Private _isSaving As Boolean

    <RelayCommand>
    Private Async Function SaveAsync() As Task

    <RelayCommand>
    Private Async Function ReloadAsync() As Task

End Class
```

The percent fields display as whole numbers (12, 3) and convert to/from the underlying decimal fraction (0.12, 0.03) at the view-model boundary. This is the convention every accounting application uses; do not surface `0.12` in the UI.

`SaveAsync` toggles `IsSaving`, calls `IVatConfigurationWriter.UpdateAsync`, raises a `Notification.Wpf` toast on success ("VAT settings updated — receipts will use new values immediately"), or surfaces `ValidationErrors` on failure.

### VatSettingsView.xaml

A simple two-column grid:

- Toggle for `IsVatRegistered` (`<CheckBox>` or `<ToggleSwitch>`)
- TIN textbox (visible only when `IsVatRegistered = True`)
- VAT rate textbox (visible only when `IsVatRegistered = True`)
- Percentage tax rate textbox (visible only when `IsVatRegistered = False`)
- Registered business name textbox
- Registered address textbox (multi-line)
- Validation errors `ItemsControl` (red text, hidden when empty)
- Save and Reload buttons

Visibility binding uses an existing `BooleanToVisibilityConverter` — do not introduce a parallel converter.

Manager-only role gate. Mirror the pattern used by ACC-11's `VatReturnView`.

### Shell navigation entry

Add `{"VatSettings", GetType(VatSettingsView)}` to `MainWindowViewModel`'s navigation dictionary. Add a sidebar entry with `Role="Manager"` and an appropriate icon (`Settings`).

### `VatConfigurationChangedEvent`

If POS-14 did not define this event, add the contract to `SharedKernel`:

```
Public Class VatConfigurationChangedEvent
    Implements INotification

    Public Property OccurredAt As DateTime
    Public Property IsVatRegistered As Boolean
    Public Property PreviousIsVatRegistered As Boolean
End Class
```

Publish it from `UpdateAsync` after the cache invalidation. No handler is required by this plan; an Accounting handler can be added later if needed.

## Implementation Notes

- The TIN regex must support both 12-digit (with branch code) and 9-digit forms commonly used by Philippine small businesses. Cite the BIR-form rule in an inline comment.
- The view must call `_loader.Invalidate()` — without it, `VatAwareReceiptService` continues to read stale values until app restart. The acceptance criteria assert this with a behavioural test.
- The "What this means" engine (ACC-06) and the VAT KPI provider (ACC-12) both read `VatConfiguration` through the loader; their behaviour should change on the next dashboard refresh after this view saves. No code change in those plans; the invalidation is the integration point.
- Do **not** add a "deactivate VAT registration" wizard or any bulk-data-migration logic. Toggling `IsVatRegistered` is a forward-looking switch only; historical receipts retain whatever bucket was active at the time of sale.
- Per the feedback memory: VB.NET `Await` is forbidden in `Catch`/`Finally`. The view-model's `SaveAsync` exception path captures the message and surfaces it to `ValidationErrors` after the `Try` block, not inside `Catch`.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `IVatConfigurationWriter.UpdateAsync` persists the row and calls `IVatConfigurationLoader.Invalidate()`.
3. A subsequent `IVatConfigurationLoader.GetAsync()` reflects the new values without any application restart.
4. Validation: an empty TIN with `IsVatRegistered = True` returns `Persisted = False` and at least one error message.
5. Validation: a TIN that does not match the regex returns `Persisted = False`.
6. `VatRate` outside `(0, 1)` returns `Persisted = False`.
7. View renders without binding errors at runtime.
8. Toggling `IsVatRegistered` in the UI swaps the visible rate textbox between "VAT rate" and "Percentage tax rate".
9. Save succeeds as Manager.
10. The view is hidden from Owner via the existing role-gate mechanism.
11. `VatConfigurationChangedEvent` is published on every successful save with the correct `PreviousIsVatRegistered` value.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-17-summary.md` using `Progress/_template.md`. Include:

- The exact TIN regex used and its origin (BIR form specification).
- A confirmation that `VatConfigurationChangedEvent` was either reused from POS-14 or newly added to SharedKernel.
- The shell navigation key string (so future audits can verify it matches the INT-02 convention).
- A screenshot or ASCII rendering of the view in both VAT-registered and non-VAT-registered states.

### Documentation
- XML doc on `IVatConfigurationWriter` describing the invalidation contract.
- XML doc on `VatConfigurationChangedEvent` listing what state-change consumers might care about.
- Inline comment in the view-model explaining the percent ↔ decimal-fraction conversion at the binding boundary.
