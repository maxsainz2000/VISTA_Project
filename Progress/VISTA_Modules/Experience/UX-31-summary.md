---
module: MerchSys.App
agent: antigravity
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/31-data-view-component-adoption-sweep.md
status: completed
---

## Task Summary

Completed the rollout of `FilterSummaryBar`, `FreshnessChip`, and `SkeletonPanel` components to all remaining target data views (UX-31). This sweep ensures a consistent, high-fidelity experience when loading and filtering data. Additionally, successfully relocated core presentation contracts from `SharedKernel/Interfaces/` to a dedicated `SharedKernel/Presentation/` folder and namespace (Scope D) to maintain modular monolith boundary integrity.

**Plan:** `[[31-data-view-component-adoption-sweep]]`

## What Was Done

### 1. Presentation-Contract Relocation (Scope D)
- Relocated contracts `IFreshnessAware`, `FilterChipItem`, `NotificationAction`, `ConfirmationRequest`, and `IConfirmationPresenter` to `SharedKernel/Presentation/`.
- Updated their declarations to `Namespace Presentation`.
- Updated all consumer imports and DI references in all module libraries and `MerchSys.App`.
- Verified no prohibited module-to-app dependencies were created.

### 2. Adoption Matrix Rollout
Finished component integrations on the following target views:
- **TransactionHistoryView**: Added `FilterSummaryBar`, coordinated `SkeletonPanel` (Rows) and `BusyOverlay`.
- **APLedgerView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **VendorDirectoryView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **ShrinkageView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **ExpiryMonitorView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **TamperAuditReportView**: Added `FilterSummaryBar` (busy spinner is kept for this accounting report view).
- **CreditManagementView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **ReorderSuggestionsView**: Added `FilterSummaryBar`, `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **GoodsReceivingView**: Added `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **DailySummaryView**: Added `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.
- **VendorCatalogView**: Added `FreshnessChip`, `SkeletonPanel` (Rows), and `BusyOverlay`.

### 3. Excluded Views Rationale
- Report views (`IncomeStatement`, `VatReturn`, `VatRelief`, `TamperAudit`) keep the full-screen spinner overlay instead of the `SkeletonPanel`. Since these reports have fixed tabular layouts that load monolithically, showing a row skeleton does not add UX value.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified all views in light and dark themes; components render correctly) |

## Codebase Wiki Discrepancies

- **ViewModels**:
  - `APLedgerViewModel`, `VendorListViewModel`, `ShrinkageViewModel`, `ExpiryMonitorViewModel`, `CreditManagementViewModel`, `ReorderSuggestionsViewModel`, `GoodsReceivingViewModel`, `DailySummaryViewModel`, `VendorCatalogViewModel` now implement `IFreshnessAware` and assign `LastLoadedAt`.
  - Added filter collections and counts for in-memory filtering.
- **Views**:
  - Main containers wrapped in coordinated loading style triggers.
  - `FilterSummaryBar`, `FreshnessChip`, and `SkeletonPanel` controls mounted in layout hierarchies.

## Cross-References
- Pattern documentation: `[[wpf-vista-presentation-contracts]]`
