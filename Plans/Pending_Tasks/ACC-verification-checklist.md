---
module: Accounting
source: Accounting-audit-2026-05-15.md
generated: 2026-05-16
---

# Operator Verification Checklist — Accounting

> Extracted from the 2026-05-15 module audit. Only operator/manual verification tasks are included here.
> Code changes are tracked as separate plan files (ACC-16, ACC-17, ACC-18).

---

## ACC-10 — Accounting VAT Ledger Schema Extension

### Build Fix

- [ ] Fix pre-existing POS build error: add `Imports System.Threading` to `VatConfigurationLoader.vb`
  - **How:** Open `MerchSys.POS/Services/VatConfigurationLoader.vb`, add `Imports System.Threading` to the imports block
  - **Verify:** `dotnet build` succeeds with 0 errors

### Schema Verification

- [ ] Verify `AddVatLedgerColumns` migration applies cleanly against a **fresh** SQLite database
- [ ] Verify `AddVatLedgerColumns` migration applies cleanly against an **existing** SQLite database
- [ ] Verify composite unique index on `Acc_VatReturns` blocks duplicate filings
  - **How:** Attempt to insert two rows with the same `PeriodStart` + `PeriodEnd` + `FormType`; expect unique constraint violation
- [ ] Verify cascade delete from `VatReturn` to `VatReturnLines`
  - **How:** Delete a `VatReturn` row; confirm all child `VatReturnLines` are automatically removed

---

## ACC-12 — VAT Payable KPI in Financial Overview

- [ ] End-to-end smoke test: seed a month of VAT ledger data, verify tile shows correct amount and severity colour changes as the BIR deadline approaches
  - **Note:** Requires ACC-16 (VatPayableTile placement) to be completed first

---

## ACC-13 — VAT Ledger Schema Verification

- [ ] Run `VatLedgerSchemaHarnessRunner.RunAndReportAsync` against the production dev database and confirm all four checks pass
  - **Note:** Requires ACC-17 (dev-menu wiring) to be completed first, OR invoke directly from Immediate Window in Debug mode

---

## ACC-14 — VAT Tile Integration into Financial Overview

- [ ] Launch app as **Manager**, navigate to Financial Overview, confirm VAT tile displays and click navigates to VatReturnView
- [ ] Launch app as **Owner**, confirm VAT tile displays but click does **not** navigate (role restriction)
- [ ] Run `VatTileSmokeHarness.RunAsync(host)` in a Debug session and confirm:
  - `ComputedVatPayable = 9000`
  - `NavigationRouteFound = True`
