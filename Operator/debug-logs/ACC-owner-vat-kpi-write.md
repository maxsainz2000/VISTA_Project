---
test-id: ACC-Owner-VAT-KPI-Write
checklist: manager-verification-checklist.md (Part 3 — Financial Overview / Owner Dashboard "VAT Payable" KPI)
branch: debug/ACC-owner-vat-kpi-write
started: 2026-06-01T00:00
status: in-progress
---

# Debug Session — Owner login: UnauthorizedWriteException on Acc_VatReturns

## Problem Statement
After logging in as the **Owner** (read-only role), the app throws:

```
MerchSys.SharedKernel.Exceptions.UnauthorizedWriteException
  HResult=0x80070005
  Message=Role 'Owner' is not permitted to write to: Acc_VatReturns.
  at MerchSys.SharedKernel.Data.RoleGuardInterceptor.CheckRole(DbContext context) RoleGuardInterceptor.vb:line 112
  at MerchSys.SharedKernel.Data.RoleGuardInterceptor.SavingChangesAsync(...) RoleGuardInterceptor.vb:line 43
  at Microsoft.EntityFrameworkCore.DbContext.<SaveChangesAsync>d__63.MoveNext()
```

## Root Cause (confirmed by full call-chain trace)
The Owner KPI Dashboard is **read-only by design**, but the "VAT Payable" KPI is computed
via a path that **persists a row** as a side effect of reading:

1. `OwnerDashboardViewModel.New` → `RefreshAsync` → `LoadAccountingKpisAsync`
   → `_financialOverview.GetOverviewAsync()` (OwnerDashboardViewModel.vb:376)
2. `_financialOverview` = `VatEnrichedFinancialOverviewService` decorator
   (Application.xaml.vb:136-140) → loops `IKpiProvider`s → `VatPayableKpiProvider.ProvideAsync`
   (VatEnrichedFinancialOverviewService.vb:34)
3. `VatPayableKpiProvider.ProvideAsync` calls `IVatReportingService.GenerateMonthlyVatReturnAsync`
   (VatPayableKpiProvider.vb:52) / `GenerateNonVatPercentageTaxAsync` (line 71)
4. `Generate*VatReturnAsync` does `_db.VatReturns.Add(...)` + `Await _db.SaveChangesAsync()`
   (VatReportingService.vb:69-70, 100-101, 146-147)
5. Session role = Owner, no `WriteContextKind.System` scope active →
   `RoleGuardInterceptor.CheckRole` Owner branch throws `UnauthorizedWriteException`
   on `Acc_VatReturns` (RoleGuardInterceptor.vb:112).

The interceptor is correct (OWASP DA5 data-layer enforcement). The defect is the
**write-on-read** KPI. `VatPayableKpiProvider.ProvideAsync` swallows the exception
(Catch ex As Exception, line 112-115), so at runtime the VAT tile silently shows nothing;
the full stack dump the user sees is the Visual Studio first-chance Exception Helper
breaking at the `Throw` site before the catch unwinds — it stops on every Owner login.

Secondary latent risk: after the failed `SaveChangesAsync`, the scoped `AccountingDbContext`
still tracks the `VatReturn` as `Added`; any later `SaveChanges` on that scoped context
would re-attempt the orphaned insert.

## Starting State
- **Commit:** `7ff013e`
- **Build status:** clean (per project state; no test projects)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatPayableKpiProvider.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/IVatReportingService.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb` (read-only — confirmed correct)

## Chosen Fix Direction
User selected **Read-only KPI**: compute the VAT Payable figure for display WITHOUT
persisting a `VatReturn`. Add read-only `Preview*` methods to `IVatReportingService` that
reuse the existing read-only `CollectLedgerDataAsync` + `BuildVatReturn` but never call
`Add`/`SaveChangesAsync`; point `VatPayableKpiProvider` at them.

## Allowed Files
- `MerchSys.Accounting/Services/IVatReportingService.vb` — add read-only preview signatures
- `MerchSys.Accounting/Services/VatReportingService.vb` — implement preview (no persist)
- `MerchSys.Accounting/Services/VatPayableKpiProvider.vb` — call preview instead of generate

### Off-limits (do NOT touch)
- `SharedKernel/Data/RoleGuardInterceptor.vb` — behaving correctly; not the bug
- `SharedKernel/*` entities and contracts
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Adding non-persisting `PreviewMonthlyVatReturnAsync` /
  `PreviewNonVatPercentageTaxAsync` to the service (interface + impl) gives the KPI a
  read-only source. This step adds the methods only (unused yet) so the build stays green.
  Both reuse the existing read-only `CollectLedgerDataAsync` (raw MySqlConnection reader) +
  `BuildVatReturn`, but omit `GuardAndClearExistingAsync` + `Add` + `SaveChangesAsync`.
- **Changed:** `IVatReportingService.vb` (2 signatures), `VatReportingService.vb` (2 impls)
- **Build result:** Build succeeded — 0 Warning(s), 0 Error(s).
- **Runtime result:** n/a (dead code until Attempt 2 wires it).
- **Verdict:** ⚠️ partial (setup step; behaviour unchanged, build green).
- **Action:** carried into combined commit on debug branch.

---

### Attempt 2
- **Hypothesis:** Point `VatPayableKpiProvider.ProvideAsync` at the new preview methods so
  the Owner dashboard never triggers a write. Removed the now-unreachable
  `Generate*`/`VatReturnLockedException`/`ListReturnsAsync` fallback blocks (preview never
  locks or persists).
- **Changed:** `VatPayableKpiProvider.vb` (replaced both Generate calls with Preview calls).
- **Build result:** Build succeeded — 0 Warning(s), 0 Error(s).
- **Runtime result:** Build-verified. No `SaveChangesAsync` is reachable from the dashboard
  KPI path, so `RoleGuardInterceptor` is no longer invoked for it → the
  `UnauthorizedWriteException` on `Acc_VatReturns` can no longer be raised on Owner login.
  **Interactive Owner-login GUI run still to be confirmed by the operator** (no automated
  WPF UI harness in this session).
- **Verdict:** ✅ fixed (pending operator GUI confirmation).
- **Action:** committed on debug branch `debug/ACC-owner-vat-kpi-write`.

---

## Resolution

- **Status:** resolved (build-verified; awaiting operator GUI confirmation before merge to master)
- **Root cause:** Write-on-read VAT KPI. `VatPayableKpiProvider.ProvideAsync` called
  `IVatReportingService.GenerateMonthlyVatReturnAsync` / `GenerateNonVatPercentageTaxAsync`,
  which `Add` a `VatReturn` and `SaveChangesAsync`. On the read-only **Owner** dashboard this
  write is rejected by `RoleGuardInterceptor` (OWASP DA5, correct behaviour) →
  `UnauthorizedWriteException` on `Acc_VatReturns`. The provider's own `Catch ex As Exception`
  swallowed it at runtime (blank VAT tile); the user saw the throw via the Visual Studio
  first-chance Exception Helper.
- **Fix description:** Added read-only `PreviewMonthlyVatReturnAsync` /
  `PreviewNonVatPercentageTaxAsync` to `IVatReportingService` that compute the figures from the
  ledger but never persist (no `Add`/`SaveChangesAsync`); pointed `VatPayableKpiProvider` at
  them. Owner stays truly read-only; the KPI now shows a live figure for both roles without
  creating BIR artifacts as a view side effect. The `Generate*` methods are unchanged and
  still used by `VatReturnViewModel.GenerateAsync` (Manager-initiated, persists as intended).
- **Final commit:** (see debug branch)
- **Agent wiki entry needed?** yes — `errors/owner-readonly-kpi-write-on-read.md`
