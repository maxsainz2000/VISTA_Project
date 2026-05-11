---
module: MerchSys.Accounting
plan-id: ACC-14
title: "VAT Tile Integration into Financial Overview"
depends-on: [ACC-07, ACC-11, ACC-12, INT-02]
estimated-files: 3
---

# VAT Tile Integration into Financial Overview

## Context

ACC-12 delivered `VatPayableTile.xaml` as a standalone `UserControl` with all bindings, severity colour states, and the "What this means" engine extension. It was explicitly written so that the consuming view (`FinancialOverviewView.xaml`, owned by ACC-07) could be edited later by an integration plan rather than at the time of authoring ACC-12. The 2026-05-11 Accounting audit identifies that follow-up step as still pending: the tile exists but is **not** placed inside `FinancialOverviewView.xaml`, and its `NavigateToVatReturnRequested` event is not wired to `MainWindowViewModel.NavigateCommand`.

The audit also flags one upstream prerequisite — `VatReturnView` is not in the INT-02 navigation shell's view dictionary. That gap is owned by **INT-13** (separate plan); this plan depends on INT-13 completing first.

This plan performs the cosmetic integration and the navigation wiring, plus a deterministic end-to-end smoke test seeded with a synthetic month of VAT ledger data to confirm the tile binds, refreshes, and routes correctly.

## Prerequisites

- **ACC-07** (Financial Overview View) — `FinancialOverviewView.xaml`, `FinancialOverviewView.xaml.vb`, `FinancialOverviewViewModel`
- **ACC-11** (VAT Reporting Service) — `VatReturnView` exists as a registered view
- **ACC-12** (VAT Payable KPI) — `VatPayableTile.xaml`, `FinancialOverviewVatExtension`, `VatPayableKpiProvider`
- **INT-02** (Shell Navigation) — `MainWindowViewModel.NavigateCommand`, view-key conventions
- **INT-13** (VatReturnView Navigation Wire-up) — `VatReturnView` registered in the shell navigation dictionary

## Wiki References

- `concepts/bir-compliance.md` — VAT filing deadline visibility on the dashboard
- `concepts/client-server-wpf.md` — navigation pattern via `MainWindowViewModel.NavigateCommand`

## Deliverables

```
MerchSys.App/Views/Accounting/FinancialOverviewView.xaml          ' Modified — add VatPayableTile
MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb       ' Modified — wire NavigateToVatReturnRequested
MerchSys.Accounting/Debug/VatTileSmokeHarness.vb                  ' New — end-to-end seeded verification
```

## Specification

### FinancialOverviewView.xaml edits

Add the tile to the existing KPI row alongside revenue, margin, AR, and AP tiles. Use the same `Grid` or `WrapPanel` container the existing tiles live in — do not introduce a new container layout. The tile's `DataContext` inherits from the view; bindings already target the `FinancialOverviewDto` partial fields added by `FinancialOverviewVatExtension`.

```xml
<vatTiles:VatPayableTile
    Grid.Column="4"
    NavigateToVatReturnRequested="OnNavigateToVatReturnRequested" />
```

Adjust `Grid.ColumnDefinitions` (or equivalent layout property) to accommodate the new tile. The other four tiles' positions are not reordered.

### FinancialOverviewView.xaml.vb wiring

```
Private Sub OnNavigateToVatReturnRequested(sender As Object, e As RoutedEventArgs)
    Dim shell = TryCast(DataContext, IShellNavigationAware)
    If shell Is Nothing Then
        Dim mainWindow = TryCast(Application.Current.MainWindow?.DataContext, MainWindowViewModel)
        If mainWindow Is Nothing Then Return
        If mainWindow.NavigateCommand.CanExecute("VatReturn") Then
            mainWindow.NavigateCommand.Execute("VatReturn")
        End If
        Return
    End If
    shell.NavigateTo("VatReturn")
End Sub
```

The dual lookup pattern (DataContext-aware interface preferred, MainWindow fallback) follows the existing convention used by other Accounting views — confirm by reading one of `IncomeStatementView.xaml.vb` or `SalesSummaryView.xaml.vb` before writing this method and match whichever pattern is already established. Do not introduce a third pattern.

The view-key string `"VatReturn"` must match the key registered by **INT-13** in the navigation dictionary. If INT-13 chose a different key, use INT-13's value.

### VatTileSmokeHarness.vb

Debug-only end-to-end harness:

```
Public Class VatTileSmokeHarness

    Public Async Function RunAsync(host As IHost) As Task(Of VatTileSmokeReport)

End Class

Public Class VatTileSmokeReport
    Public Property SeededVatableSales As Decimal
    Public Property SeededOutputVat As Decimal
    Public Property SeededInputVat As Decimal
    Public Property ComputedVatPayable As Decimal
    Public Property TileSeverity As KpiSeverity
    Public Property DaysUntilDeadline As Integer
    Public Property NavigationRouteFound As Boolean
End Class
```

Behaviour:

1. Build a scratch `AccountingDbContext` against `%TEMP%\vista-vat-tile-smoke-<guid>.db`.
2. Migrate schema; seed:
   - One `VatConfiguration` row with `IsVatRegistered = True`, `Tin = "999-999-999-000"`.
   - Synthetic VAT ledger data for the previous calendar month: Vatable sales ₱100,000, Output VAT ₱12,000, Input VAT ₱3,000 ⇒ expected payable ₱9,000.
3. Resolve `IFinancialOverviewService` (the decorated, VAT-enriched instance) and call `GetOverviewAsync()`.
4. Assert the DTO carries `VatPayable = 9000`, `VatPayableLabel = "VAT Payable"`, severity transitions match deadline distance.
5. Inspect the registered navigation dictionary to confirm `"VatReturn"` resolves to `VatReturnView` (smoke-tests INT-13's wiring from the consumer side).
6. Write a Markdown report to `%TEMP%\vat-tile-smoke-report-<timestamp>.md`.

Gate with `#If DEBUG`.

## Implementation Notes

- The audit recorded this as **Priority 1** for Accounting because the VAT KPI is otherwise invisible to the owner. Do not defer the XAML placement to a "polish" plan — the tile is the entire user-visible deliverable of ACC-12 in practice.
- Re-running the harness must be safe: if a scratch DB from a previous run is detected at the target path, generate a fresh GUID rather than reusing.
- Do not introduce new XAML namespaces beyond the one needed for `VatPayableTile`. Reuse existing `xmlns:` declarations on the view root.
- Per the feedback memory (`feedback_vbnet_await_catch.md`): if the harness uses a `Try/Finally` for scratch-file cleanup, do not `Await` inside `Finally` — capture state and await after the block.
- Verify the Owner role: clicking the tile in the smoke harness must succeed for Manager and be a no-op (or visually suppressed) for Owner, per ACC-11's role gate on `VatReturnView`. The harness logs the role check result.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `FinancialOverviewView.xaml` contains a `<vatTiles:VatPayableTile />` element bound into the existing KPI layout.
3. `FinancialOverviewView.xaml.vb` defines `OnNavigateToVatReturnRequested`; the method calls `MainWindowViewModel.NavigateCommand` with the key `"VatReturn"` (or whichever key INT-13 registered).
4. The view loads at runtime without binding errors when `VatConfiguration.IsVatRegistered = True` and ledger data exists.
5. The view loads at runtime without binding errors when `VatConfiguration.IsVatRegistered = False` (tile shows "Percentage Tax").
6. Clicking the tile as Manager navigates to `VatReturnView`.
7. Clicking the tile as Owner does not crash and does not navigate (ACC-11 role gate).
8. `VatTileSmokeHarness.RunAsync` produces a `VatTileSmokeReport` with `ComputedVatPayable = 9000` from the seeded data.
9. The harness's `NavigationRouteFound` is `True` (proves INT-13's view-key registration is consumable).
10. The harness is excluded from release builds via `#If DEBUG`.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-14-summary.md` using `Progress/_template.md`. Include the `git diff` of the two modified files and a sample `VatTileSmokeReport` from a clean run.

### Documentation
- XML doc comment on `OnNavigateToVatReturnRequested` describing the dual-lookup pattern and citing the matched precedent file.
- A short note in the implementation summary listing which existing Accounting view's navigation pattern was matched, so future agents can keep the convention coherent.
