---
module: Accounting
audit-date: 2026-05-07
---

# VISTA Module Audit — Accounting

**Audit Date:** 2026-05-07  
**Plans Folder:** `Plans/VISTA_Modules/Accounting/`  
**Progress Folder:** `Progress/VISTA_Modules/Accounting/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Depends On | Status |
|---------|-----------|------------|--------|
| ACC-01 | Accounting Domain Models | INFRA-02 | ✅ Completed |
| ACC-02 | Accounting Data Access | INFRA-03, ACC-01 | ✅ Completed |
| ACC-03 | Financial Overview Service | ACC-02 | ✅ Completed |
| ACC-04 | Income Statement Service | ACC-02 | ✅ Completed |
| ACC-05 | Sales Summary Service | ACC-02 | ✅ Completed |
| ACC-06 | "What This Means" Engine | ACC-03, ACC-04, ACC-05 | ✅ Completed |
| ACC-07 | View — Financial Overview | ACC-03, ACC-06 | ✅ Completed |
| ACC-08 | View — Income Statement | ACC-04, ACC-06 | ✅ Completed |
| ACC-09 | View — Sales Summary | ACC-05, ACC-06 | ✅ Completed |

**Total Plans:** 9  
**Completed:** 9 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### ACC-02 — Accounting Data Access

**Status:** Completed

- [ ] ACC-03: Accounting Services — implement `IAccountingService` for period aggregation and snapshot refresh
- [ ] ACC-04: Accounting ViewModels and Views — KPI dashboard and plain-language summaries

> Note: These items were written before ACC-03 through ACC-09 were completed. They are now superseded by subsequent plans — no action required.

---

### ACC-07 — View — Financial Overview

**Status:** Completed

- [ ] DI registration of `FinancialOverviewViewModel` as Transient in `Application.xaml.vb` (when INFRA-02 DI wiring is finalized)
- [ ] Wire `FinancialOverviewView` into the main navigation shell

---

### ACC-08 — View — Income Statement

**Status:** Completed

- [ ] DI registration of `IncomeStatementViewModel` as Transient in `Application.xaml.vb` (when INFRA-02 DI wiring is finalized)
- [ ] Wire `IncomeStatementView` into the main navigation shell

---

### ACC-09 — View — Sales Summary

**Status:** Completed

- [ ] DI registration of `SalesSummaryViewModel` as Transient (deferred to INFRA-02 DI wiring finalization)
- [ ] Wire `SalesSummaryView` into the main navigation shell

---

## Known Limitations Noted in Summaries

These are not blocking bugs but documented shortfalls that require future cross-module work:

| Plan | Limitation | Resolution Path |
|------|-----------|-----------------|
| ACC-02 | `RevenueRecord.COGS` is recorded as `0` — `SaleCompletedEvent` carries no unit cost | Backfill when a per-product cost query from Inventory is available (ACC-03+ service layer) |
| ACC-03 | `OverdueARCount`, `OverdueAPCount`, `LowStockAlertCount` all return `0` | Requires `GetTotalARQuery`, `GetTotalAPQuery`, `GetLowStockAlertCountQuery` MediatR contracts in SharedKernel |
| ACC-03 | `TotalAR` and `TotalAP` in `RefreshSnapshotAsync` are carried forward from prior-day snapshot | No live AR/AP cross-module query contract exists yet |

---

## Build Status

| Plan ID | Build Result |
|---------|-------------|
| ACC-01 | ✅ 0 errors, 0 warnings |
| ACC-02 | ✅ 0 errors, 0 warnings |
| ACC-03 | ✅ 0 errors, 0 warnings |
| ACC-04 | ✅ 0 errors, 0 warnings |
| ACC-05 | ✅ 0 errors, 0 warnings |
| ACC-06 | ✅ 0 errors, 0 warnings |
| ACC-07 | ✅ 0 errors, 0 warnings |
| ACC-08 | ✅ 0 errors, 0 warnings |
| ACC-09 | ✅ 0 errors, 0 warnings |

---

## Plans With No Progress File

None. All 9 plans have matching progress summaries.

---

## Amendments & Special Files

None found. No `*-amendment.md` or non-standard files in the Progress folder.

---

## Summary & Recommendations

- **100% plan coverage.** All 9 Accounting plans have completed progress summaries with green builds (0 errors, 0 warnings). The Accounting module is fully implemented at the code level.
- **6 active pending tasks** remain across ACC-07, ACC-08, and ACC-09 — all concern DI registration and navigation shell wiring, which are deferred to INFRA-02 finalization. These are not Accounting-module tasks; they are App-layer tasks.
- **2 superseded tasks** in ACC-02 (pointing to ACC-03/ACC-04 as next steps) are now obsolete — those plans are complete.
- **COGS accuracy gap:** `RevenueRecord.COGS = 0` across all revenue records until a cross-module inventory cost query is wired. This affects gross profit figures in the income statement and financial overview. Priority action: add a `GetProductCostQuery` contract in SharedKernel and update `SaleCompletedAccountingHandler` to resolve FIFO unit cost at sale time.
- **Cross-module alert counts are zeroed:** `OverdueARCount`, `OverdueAPCount`, and `LowStockAlertCount` on the Financial Overview dashboard all display `0` until SharedKernel MediatR query contracts for these are defined. Recommended next step: define `GetTotalARQuery`, `GetTotalAPQuery`, and `GetLowStockAlertCountQuery` in a SharedKernel update plan.
