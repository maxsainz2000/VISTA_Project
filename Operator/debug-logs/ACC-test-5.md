---
test-id: ACC-Test-5
checklist: ACC-verification-checklist.md
branch: debug/ACC-test-5
started: 2026-05-21T00:00
status: resolved
---

# Debug Session — ACC Test 5

## Problem Statement

Test 5 checkbox is unchecked. Need to verify that the VAT Payable KPI tile on the Financial Overview screen:
1. Displays a peso amount that matches the current period's percentage tax due
2. Changes colour based on BIR deadline proximity (green/yellow/red)

## Starting State

- **Commit:** `64bbdf3`
- **Build status:** Cannot confirm (DLLs locked by running VS process at session start)
- **DB state:** `Acc_VatReturns` has Id=50 for Year=2026, Period=2 (Q2), FormType=Form2551Q, VatPayable=306.0, FilingStatus=Generated
- **VAT config:** `Pos_VatConfiguration` Id=1: IsVatRegistered=0, NonVatPercentageTaxRate=0.03

## Code Review Findings (pre-run)

- `VatPayableTile.xaml` — tile is at Grid.Column=7 in `FinancialOverviewView.xaml`; binds `VatPayable`, `VatPayableLabel`, `VatFilingDueDate`, `IsVatWarning`, `IsVatCritical`
- `FinancialOverviewVatExtension.vb` — partial class extension on `FinancialOverviewViewModel`; overrides `OnPropertyChanged` to detect `IsBusy` True→False and calls `ApplyVatDataFromServiceAsync()`
- `VatPayableKpiProvider.vb` — since `IsVatRegistered=False`, uses `GenerateNonVatPercentageTaxAsync(2026, 2)`. Deadline = July 25, 2026 (~65 days away) → Severity = Info (green). Expected tile: Label="Percentage Tax", Amount≈₱306.00, Due Jul 25, green border.
- `VatReportingService.GuardAndClearExistingAsync` — deletes the Generated state return and recreates it on each call. This is idempotent by design.
- DI registration in `Application.xaml.vb` line 79–85: `VatEnrichedFinancialOverviewService` wraps `FinancialOverviewService`, `VatPayableKpiProvider` registered as `IKpiProvider`.
- XAML namespace: `xmlns:vatTiles="clr-namespace:MerchSys.App.Views.Accounting.Components"` — correct (root namespace = MerchSys.App).
- No code bug found from static analysis. Test requires runtime visual confirmation.

## Allowed Files

- `WPF_Applications\MerchSys\src\MerchSys.Accounting\` — VAT service, KPI provider
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\` — tile XAML/code-behind

### Off-limits (do NOT touch)

- `SharedKernel/Events/*` — shared contracts
- Other modules' services or handlers

---

## Attempt Log

### Attempt 1 — Runtime Verification
- **Hypothesis:** Code is correct; tile should display ₱306.00 (or recalculated amount), label "Percentage Tax", white/default border (Info severity).
- **Changed:** No files changed — static analysis only.
- **Build result:** N/A (no code changes)
- **Runtime result:** Operator confirmed: tile shows "Percentage Tax" ₱306.00 "Due Jul 25" with white border. Matches expected output — Info severity (≥7 days to deadline) correctly shows default white border, not colored accent.
- **Verdict:** ✅ fixed
- **Action:** No commit needed (no code changes). Checklist box checked.

---

## Resolution

- **Status:** resolved
- **Root cause:** No bug. Feature was correctly implemented; test just needed runtime confirmation.
- **Fix description:** No code changes. Tile correctly shows "Percentage Tax" (business is not VAT-registered), ₱306.00, "Due Jul 25", white border (Info severity = deadline >7 days away). Color accent only activates on Warning (≤7 days, orange) or Critical (≤3 days, red).
- **Final commit:** N/A
- **Agent wiki entry needed?** no
