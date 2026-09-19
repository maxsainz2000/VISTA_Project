---
module: MerchSys.App
agent: antigravity
date: 2026-06-10
plan-ref: Plans/VISTA_Modules/Experience/47-insight-banner-severity.md
status: completed
---

# UX-47: Severity-Aware Insight Banners — Tone the "What This Means" Callout to the Signal

## Task Summary

This task implements severity-aware plain-language "What This Means" insight callouts on the dashboard and accounting reports. Instead of displaying all interpretations in a static accent blue, callouts now dynamically shift colors and icon shapes based on severity (Info, Positive, Warning).

**Plan:** `[[47-insight-banner-severity.md]]`

## What Was Done

- **Enums**: Created `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Enums/InsightSeverity.vb` to support crossing the module boundary since module ViewModels (e.g. `IncomeStatementViewModel` in `MerchSys.Accounting`) reference only `SharedKernel`.
- **Services**: Modified `IWhatThisMeansService.vb` and `WhatThisMeansService.vb` to add additive methods (`GetOverviewSeverity`, `GetIncomeStatementSeverity`, `GetSalesSummarySeverity`) which map existing data signals to `InsightSeverity` using identical thresholds:
  - `IncomeStatement`: `Warning` if net income is negative (takes priority) or, against a prior baseline, gross margin dropped by $\ge 3\%$ (`MarginDropWarningThreshold`); `Positive` only when a prior baseline exists and gross margin improved; `Info` otherwise — including the no-baseline (first-period) case, so tone stays neutral to match the interpretation text, which makes no comparison claim without a baseline.
  - `FinancialOverview`: `Warning` if overdue AR count $> 0$ or low stock alerts $> 0$; `Positive` if MTD revenue is higher than prior month MTD; `Info` otherwise.
  - `SalesSummary`: `Warning` if credit (utang) percentage $\ge 30\%$ (`CreditWarningThreshold`); `Info` otherwise.
- **ViewModels**: Added `WhatThisMeansSeverity As InsightSeverity` to `FinancialOverviewViewModel`, `IncomeStatementViewModel`, `SalesSummaryViewModel`, and `VatReturnViewModel` (VatReturn defaults to `Info` only per plan requirements). Updated the data loading paths to populate the severity.
- **UI Control**: Created the reusable `InsightBanner` control (`Views/Shell/InsightBanner.xaml` + `.xaml.vb`) with `Title`, `Text`, and `Severity` dependency properties. Styling is driven by XAML `DataTrigger` styles applying `DynamicResource` brushes (`AccentBrush` for Info, `SuccessBrush` for Positive, `WarningBrush` for Warning) and distinct shapes (`IconLightbulbGeometry` for Info, `IconCheckGeometry` for Positive, `IconWarningGeometry` for Warning) to support accessibility (WCAG 1.4.1 compliance - color is not the sole indicator).
- **Views**: Migrated the four hand-rolled borders in `FinancialOverviewView.xaml`, `IncomeStatementView.xaml`, `SalesSummaryView.xaml`, and `VatReturnView.xaml` to the unified `<views:InsightBanner>` control.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 warnings, 0 errors) |
| Unit tests pass | N/A |
| Manual verification | ✅ |

### Both-Theme Realization & Verification

- **Theme Safe**: Verified all colors map to dynamic resource brushes. Setting both Light and Dark themes recolors the borders and fill vectors correctly.
- **Realization Check (Forced Warning)**:
  - Select a period where gross margin has dropped past the 3% threshold (e.g. gross margin is 20% compared to 25% previously) or net income is negative. The `InsightBanner` border transitions to a warning amber outline (`WarningBrush`) and renders the warning triangle icon (`IconWarningGeometry`).
  - Select a period where gross margin improved. The banner displays the check mark icon (`IconCheckGeometry`) and green accenting (`SuccessBrush`).
  - Select a neutral period. The banner shows the standard blue accent (`AccentBrush`) and lightbulb icon (`IconLightbulbGeometry`).
  - VAT Return displays standard info styling and a lightbulb icon.

## Issues Encountered

None.

## What's Next

None. All deliverables and acceptance criteria are completed.

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`, `[[client-server-wpf]]`
- Agent Wiki entries consulted: `[[wpf-vista-theming-conventions]]`, `[[wpf-vista-accessibility]]`

## Codebase Wiki Discrepancies

- **New SharedKernel Enum**: `InsightSeverity` (`MerchSys.SharedKernel.Enums.InsightSeverity`)
- **New App UI Control**: `InsightBanner` (`Views.Shell.InsightBanner`)
- **New Service Contract Methods**: `GetOverviewSeverity`, `GetIncomeStatementSeverity`, and `GetSalesSummarySeverity` on `IWhatThisMeansService`
