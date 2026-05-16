---
module: Accounting
audit-date: 2026-05-15
---

# VISTA Module Audit — Accounting

**Audit Date:** 2026-05-15  
**Plans Folder:** `Plans/VISTA_Modules/Accounting/`  
**Progress Folder:** `Progress/VISTA_Modules/Accounting/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| ACC-01 | Accounting Domain Models | ✅ Completed |
| ACC-02 | Accounting Data Access | ✅ Completed |
| ACC-03 | Financial Overview Service | ✅ Completed |
| ACC-04 | Income Statement Service | ✅ Completed |
| ACC-05 | Sales Summary Service | ✅ Completed |
| ACC-06 | What This Means Engine | ✅ Completed |
| ACC-07 | View — Financial Overview | ✅ Completed |
| ACC-08 | View — Income Statement | ✅ Completed |
| ACC-09 | View — Sales Summary | ✅ Completed |
| ACC-10 | Accounting VAT Ledger Schema Extension | ✅ Completed |
| ACC-11 | BIR VAT Reporting Service & Views | ✅ Completed |
| ACC-12 | VAT Payable KPI in Financial Overview | ✅ Completed |
| ACC-13 | VAT Ledger Schema Verification | ✅ Completed |
| ACC-14 | VAT Tile Integration into Financial Overview | ✅ Completed |
| ACC-15 | Receipt Tamper Audit Handler | ✅ Completed |

**Total Plans:** 15  
**Completed:** 15 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### ACC-10 — Accounting VAT Ledger Schema Extension

**Status:** Completed

- [ ] ACC-11: VAT return reporting service and BIR Form 2550M/Q view (consumes `VatReturn` / `VatReturnLine` schema delivered here) *(now completed in ACC-11)*
- [ ] Fix pre-existing POS build error: add `Imports System.Threading` to `VatConfigurationLoader.vb`
- [ ] Verify `AddVatLedgerColumns` migration applies cleanly against a fresh and existing SQLite database
- [ ] Verify composite unique index on `Acc_VatReturns` blocks duplicate filings
- [ ] Verify cascade delete from `VatReturn` to `VatReturnLines`

### ACC-11 — BIR VAT Reporting Service & Views

**Status:** Completed

- [ ] INT-01: Cross-module integration wiring (MediatR publisher/subscriber setup at App startup) *(now completed in INT-01)*
- [ ] INT-02 onward: Integration plans per the implementation order defined in CLAUDE.md *(now completed)*

### ACC-12 — VAT Payable KPI in Financial Overview

**Status:** Completed

- [ ] Place `VatPayableTile` into `FinancialOverviewView.xaml` alongside the existing KPI cards (cosmetic integration — modifies ACC-07 view file, requires separate authorisation)
- [ ] Wire `NavigateToVatReturnRequested` event in `FinancialOverviewView.xaml.vb` to the `MainWindowViewModel.NavigateCommand` for the `VatReturnView` navigation item
- [ ] End-to-end smoke test: seed a month of VAT ledger data, verify tile shows correct amount and severity colour changes as the BIR deadline approaches

### ACC-13 — VAT Ledger Schema Verification

**Status:** Completed

- [ ] Wire `VatLedgerSchemaHarnessRunner.RunAndReportAsync` to a developer-only menu item in `MerchSys.App` (similar to how INT-13 wired the VAT tile smoke harness).
- [ ] Run the harness once against the production dev database to produce an actual `CheckResult` output and confirm all four checks pass.
- [ ] Consider adding `IDbContextFactory(Of AccountingDbContext)` registration to `DatabaseConfig.AddModuleDbContexts` if future harnesses need factory-based multi-instance patterns.

### ACC-14 — VAT Tile Integration into Financial Overview

**Status:** Completed

- [ ] Runtime verification: launch app as Manager, navigate to Financial Overview, confirm VAT tile displays and click navigates to VatReturnView
- [ ] Runtime verification: launch app as Owner, confirm VAT tile displays but click does not navigate
- [ ] Run `VatTileSmokeHarness.RunAsync(host)` in a Debug session and confirm `ComputedVatPayable = 9000` and `NavigationRouteFound = True`

### ACC-15 — Receipt Tamper Audit Handler

**Status:** Completed

- [ ] Future reporting plan: implement a tamper-incident report UI consuming `ITamperAuditQueryService` (out of scope for ACC-15 per plan)
- [ ] INFRA-08: add MariaDB-equivalent immutability triggers for the central replica of `Acc_TamperAuditLog` (cross-reference noted in the migration file)

---

## Plans With No Progress File

*None — all 15 plans have corresponding progress summaries.*

---

## Amendments & Special Files

*None found in the Accounting Progress folder.*

---

## Summary & Recommendations

- **100% completion** — all 15 Accounting plans have `status: completed` summaries.
- **18 pending tasks** remain, concentrated in the VAT compliance chain (ACC-10, ACC-12, ACC-13, ACC-14).
- **ACC-10 build error:** The `Imports System.Threading` fix to `VatConfigurationLoader.vb` is flagged as a pre-existing POS build error — this needs immediate resolution to ensure the solution builds clean (0 warnings, 0 errors per CLAUDE.md).
- **ACC-12 VAT tile placement:** The `VatPayableTile` is not yet inserted into `FinancialOverviewView.xaml`. This is a cosmetic but visible gap in the Financial Overview screen.
- **ACC-14 runtime tests:** Both Manager and Owner navigation paths for the VAT tile are untested at runtime; run these before signing off on the Accounting module.
- **ACC-13 harness:** Wire the schema verification harness to a dev-menu item and run it once against the dev database to validate all four VAT schema checks pass.
