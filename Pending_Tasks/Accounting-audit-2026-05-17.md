---
module: Accounting
audit-date: 2026-05-17
---

# VISTA Module Audit — Accounting

**Audit Date:** 2026-05-17
**Plans Folder:** `Plans/VISTA_Modules/Accounting/`
**Progress Folder:** `Progress/VISTA_Modules/Accounting/`

---

## What's Next Cleanup (Step 0)

**Items resolved this pass:** 4

| Summary | Item | Resolved By |
|---------|------|-------------|
| ACC-12 | Place VatPayableTile into FinancialOverviewView.xaml alongside existing KPI cards | ACC-14 |
| ACC-12 | Wire NavigateToVatReturnRequested event in FinancialOverviewView.xaml.vb to MainWindowViewModel.NavigateCommand | ACC-14 |
| ACC-13 | Wire VatLedgerSchemaHarnessRunner.RunAndReportAsync to a developer-only menu item in MerchSys.App | ACC-17 |
| ACC-15 | Future reporting plan: implement a tamper-incident report UI consuming ITamperAuditQueryService | ACC-18 |

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
| ACC-16 | VatPayableTile Financial Overview Placement & Navigation Wiring | ✅ Completed |
| ACC-17 | Schema Verification Harness Dev-Menu Integration | ✅ Completed |
| ACC-18 | Tamper Incident Report UI | ✅ Completed |

**Total Plans:** 18
**Completed:** 18 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### ACC-10 — Accounting VAT Ledger Schema Extension

**Status:** Completed

- [ ] Verify `AddVatLedgerColumns` migration applies cleanly against a fresh and existing SQLite database
- [ ] Verify composite unique index on `Acc_VatReturns` blocks duplicate filings
- [ ] Verify cascade delete from `VatReturn` to `VatReturnLines`

### ACC-12 — VAT Payable KPI in Financial Overview

**Status:** Completed

- [ ] End-to-end smoke test: seed a month of VAT ledger data, verify tile shows correct amount and severity colour changes as the BIR deadline approaches

### ACC-13 — VAT Ledger Schema Verification

**Status:** Completed

- [ ] Run the harness once against the production dev database to produce an actual `CheckResult` output and confirm all four checks pass
- [ ] Consider adding `IDbContextFactory(Of AccountingDbContext)` registration to `DatabaseConfig.AddModuleDbContexts` if future harnesses need factory-based multi-instance patterns

### ACC-14 — VAT Tile Integration into Financial Overview

**Status:** Completed

- [ ] Runtime verification: launch app as Manager, navigate to Financial Overview, confirm VAT tile displays and click navigates to VatReturnView
- [ ] Runtime verification: launch app as Owner, confirm VAT tile displays but click does not navigate
- [ ] Run `VatTileSmokeHarness.RunAsync(host)` in a Debug session and confirm `ComputedVatPayable = 9000` and `NavigationRouteFound = True`

### ACC-15 — Receipt Tamper Audit Handler

**Status:** Completed

- [ ] Add MariaDB-equivalent immutability triggers for the central replica of `Acc_TamperAuditLog` (INFRA-08 covers POS tables only, not Acc_*)

### ACC-16 — VatPayableTile Financial Overview Placement & Navigation Wiring

**Status:** Completed

- [ ] Runtime verification: launch as Manager, navigate to Financial Overview, confirm tile displays and click navigates to VatReturnView
- [ ] Runtime verification: launch as Owner, confirm tile displays but click does not navigate
- [ ] Run `VatTileSmokeHarness.RunAsync(host)` in a Debug session to confirm `ComputedVatPayable = 9000` assertion passes

### ACC-17 — Schema Verification Harness Dev-Menu Integration

**Status:** Completed

- [ ] Operator: run Debug build, navigate to "Developer Tools → Run VAT Schema Harness", click button, confirm `MessageBox` appears and a `.md` report appears in `%TEMP%`

### ACC-18 — Tamper Incident Report UI

**Status:** Completed

- [ ] Future: Export to CSV/PDF for BIR auditor submission (explicitly deferred in ACC-18 plan)

---

## Plans With No Progress File

*(None — all 18 plans have corresponding completed progress summaries.)*

---

## Amendments & Special Files

*(None)*

---

## Summary & Recommendations

- **100% complete** — all 18 Accounting plans have completed progress summaries.
- **12 pending `[ ]` tasks** across 8 plans; all are runtime verification, harness execution, or deferred future work.
- **ACC-14 and ACC-16 overlap** — both have near-identical runtime verification items (Manager/Owner tile test, VatTileSmokeHarness). A single runtime verification pass covers both plans' acceptance criteria simultaneously.
- **ACC-10 schema verification** — the three verification items (migration clean apply, unique index enforcement, cascade delete) are directly exercisable via the ACC-13 `VatLedgerSchemaHarness` harness. Running ACC-13's harness covers ACC-10's checks as well.
- **ACC-15 MariaDB triggers** — `Acc_TamperAuditLog` has SQLite immutability triggers (deployed) but no MariaDB central-replica equivalent. This is a standalone SQL task similar to INFRA-08, not a VB.NET implementation.
- **ACC-17 operator test** — requires the Debug build and developer menu; straightforward one-time verification step.
- **ACC-18 CSV/PDF export** — explicitly deferred to a future plan. No action needed at current stage.
