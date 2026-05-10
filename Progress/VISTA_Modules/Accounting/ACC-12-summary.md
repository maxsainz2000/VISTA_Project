---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/Accounting/12-vat-payable-kpi.md
status: completed
---

## Task Summary

Implemented the VAT Payable KPI widget for the Financial Overview Dashboard (ACC-12).
Added a `VatPayableKpiProvider` behind an `IKpiProvider` abstraction, a decorator
`VatEnrichedFinancialOverviewService` that enriches the ACC-03 DTO without touching its
source file, partial-class extensions on both `FinancialOverviewDto` and
`FinancialOverviewViewModel`, a "What This Means" insight provider, and a standalone
`VatPayableTile.xaml` UserControl.

**Plan:** `[[12-vat-payable-kpi]]`

## What Was Done

**New files — MerchSys.Accounting:**
- Created `Services/IKpiProvider.vb` — `IKpiProvider` interface, `KpiValue` (with `DueDate As DateTime?` extension beyond plan spec to avoid fragile string parsing), `KpiSeverity` enum
- Created `Services/VatPayableKpiProvider.vb` — full implementation; handles `VatReturnLockedException` by falling back to `ListReturnsAsync` (Await-in-Catch pattern avoided per VB.NET BC36943 restriction — captured bool flag before re-await)
- Created `Services/VatEnrichedFinancialOverviewService.vb` — decorator; calls inner service then each `IKpiProvider`; maps `"VatPayable"` key to DTO VAT extension fields
- Created `Services/Insights/IFinancialInsightProvider.vb` — `IFinancialInsightProvider` interface
- Created `Services/Insights/VatPayableInsightProvider.vb` — generates Info/Warning/Critical insight sentences from the enriched DTO
- Created `ViewModels/Extensions/FinancialOverviewVatExtension.vb` — two partial classes:
  - `Partial Public Class FinancialOverviewDto` (Services namespace) — adds `VatPayable`, `VatPayableLabel`, `VatFilingDueDate`, `VatPayableSeverity`, `IsVatRegistered`, `VatPeriodDescription`
  - `Partial Public Class FinancialOverviewViewModel` (ViewModels namespace) — adds six observable VAT properties, `IsVatWarning`/`IsVatCritical` read-only helpers, `NavigateToVatReturnCommand`, `NavigateToVatReturnRequested` event, and an `OnPropertyChanged` override that triggers `ApplyVatDataFromServiceAsync` on every IsBusy True→False transition (hooking into the existing refresh cycle without modifying the ACC-07 file)

**New files — MerchSys.App:**
- Created `Views/Accounting/Components/VatPayableTile.xaml` — standalone `UserControl` with severity DataTriggers (neutral/amber/red), peso amount, label, and due-date secondary line; inherits parent DataContext
- Created `Views/Accounting/Components/VatPayableTile.xaml.vb` — minimal code-behind (`InitializeComponent` only; DataContext from parent)

**Modified files:**
- `Services/WhatThisMeansService.vb` — added constructor accepting `IEnumerable(Of IFinancialInsightProvider)`; `GenerateOverviewInterpretation` now appends each provider's sentence after the standard text
- `MerchSys.App/Application.xaml.vb` — replaced single `IFinancialOverviewService` registration with decorator pattern (`FinancialOverviewService` registered as concrete, `IFinancialOverviewService` resolved via factory that wraps it in `VatEnrichedFinancialOverviewService`); added `IKpiProvider`, `IFinancialInsightProvider`, and `VatPayableTile` registrations

**Unchanged (verified via `git diff`):**
- `Services/IFinancialOverviewService.vb` (ACC-03)
- `Services/FinancialOverviewService.vb` (ACC-03)
- `ViewModels/FinancialOverviewViewModel.vb` (ACC-07)
- `Views/Accounting/FinancialOverviewView.xaml` (ACC-07)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A — deferred to separate testing session |

## Issues Encountered

- **Issue:** `Await` inside `Catch` block is illegal in VB.NET (BC36943) — used in the `VatReturnLockedException` fallback path.
  - **Resolution:** Captured a `Boolean` flag before the `Catch` block, re-awaited after the `Try`/`End Try` inside an `If` block. Matches existing project pattern documented in agent wiki.

- **Issue:** Compiler warning BC42104 (`vatReturn` used before assigned) — variable declared without initializer.
  - **Resolution:** Initialised `vatReturn = Nothing` (and other locals) at declaration site.

- **Design decision:** The plan calls for a `DueDate As DateTime?` on `KpiValue` beyond the spec's `SecondaryText` string; this avoids parsing "Due Jul 25" back into a `DateTime` in the decorator. The `SecondaryText` display string is still populated for forward compatibility.

- **Design decision:** The ViewModel partial class hooks VAT loading via an `OnPropertyChanged` override detecting `IsBusy` True→False, avoiding any modification to the private `LoadDataAsync` method in the ACC-07 file. This results in a second call to `GetOverviewAsync()` after each main refresh; this is acceptable per the plan's note on idempotency.

- **Design decision:** The `VatPayableTile.xaml` inherits its `DataContext` from the parent view (which will be `FinancialOverviewView` / `FinancialOverviewViewModel`). The tile is a standalone UserControl — cosmetic placement into `FinancialOverviewView.xaml` is deferred to an integration step (the view is part of ACC-07 and not modified here).

## What's Next

- [ ] Place `VatPayableTile` into `FinancialOverviewView.xaml` alongside the existing KPI cards (cosmetic integration — modifies ACC-07 view file, requires separate authorisation)
- [ ] Wire `NavigateToVatReturnRequested` event in `FinancialOverviewView.xaml.vb` to the `MainWindowViewModel.NavigateCommand` for the `VatReturnView` navigation item
- [ ] End-to-end smoke test: seed a month of VAT ledger data, verify tile shows correct amount and severity colour changes as the BIR deadline approaches

## Cross-References

- Domain Wiki consulted: `concepts/vat-ready.md`, `concepts/bir-compliance.md`
- Prior plans: ACC-03 (DTO + service), ACC-07 (ViewModel + View), ACC-11 (VAT reporting service)
- Agent Wiki: Await-in-Catch BC36943 pattern applied in `VatPayableKpiProvider`
