---
type: error-fix
module: MerchSys.Accounting
agent: claude-code
date: 2026-06-01
tags: [vb-net, ef-core, mariadb, role-enforcement, owasp-da5, kpi, dashboard, write-on-read, runtime-error]
error-code: UnauthorizedWriteException
severity: runtime-error
---

## Problem

Logging in as the **Owner** (read-only role) throws on dashboard load:

```
MerchSys.SharedKernel.Exceptions.UnauthorizedWriteException
  HResult=0x80070005
  Message=Role 'Owner' is not permitted to write to: Acc_VatReturns.
  at MerchSys.SharedKernel.Data.RoleGuardInterceptor.CheckRole(DbContext) RoleGuardInterceptor.vb:112
  at MerchSys.SharedKernel.Data.RoleGuardInterceptor.SavingChangesAsync(...) RoleGuardInterceptor.vb:43
  at Microsoft.EntityFrameworkCore.DbContext.<SaveChangesAsync>d__63.MoveNext()
```

At runtime the exception is actually swallowed (the VAT tile just renders blank); the full
stack dump is the **Visual Studio first-chance Exception Helper** breaking at the `Throw`
site before the `Catch` unwinds. It stops the debugger on every Owner login.

## Root Cause

**A read-only KPI display path performed a database write (generate-on-read).**

`OwnerDashboardViewModel.LoadAccountingKpisAsync` → `IFinancialOverviewService.GetOverviewAsync`
(the `VatEnrichedFinancialOverviewService` decorator) iterates `IKpiProvider`s and calls
`VatPayableKpiProvider.ProvideAsync`. That provider called
`IVatReportingService.GenerateMonthlyVatReturnAsync` / `GenerateNonVatPercentageTaxAsync`,
which do `_db.VatReturns.Add(...)` + `Await _db.SaveChangesAsync()`. For the read-only Owner,
`RoleGuardInterceptor` (OWASP DA5 data-layer enforcement) correctly rejects the write.

The interceptor was **not** the bug — it did exactly its job. The bug was upstream: a "view a
KPI" operation lazily generated and *persisted* a BIR tax artifact. Symptoms: (1) debugger
breaks on every Owner login; (2) the Owner can never see the VAT Payable KPI (always silently
fails). Latent risk: after the failed `SaveChangesAsync`, the scoped `AccountingDbContext`
still tracks the `VatReturn` as `Added`, so any later `SaveChanges` on that scope re-attempts
the orphaned insert.

## Fix

Added read-only **preview** methods that compute the same figures from the ledger but never
persist, and pointed the KPI provider at them. The `Generate*` methods are untouched and still
used by `VatReturnViewModel.GenerateAsync` (Manager-initiated, persists as intended).

```vb
' VatReportingService.vb — new read-only path (no Add / no SaveChangesAsync)
Public Async Function PreviewMonthlyVatReturnAsync(year As Integer, month As Integer) As Task(Of VatReturn) _
    Implements IVatReportingService.PreviewMonthlyVatReturnAsync
    Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())
    If Not vatConfig.IsVatRegistered Then Return Nothing
    Dim windowStart = New DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
    Dim data = Await CollectLedgerDataAsync(windowStart, windowStart.AddMonths(1))
    Dim vatPayable = Math.Round(data.TotalOutputVat - data.TotalInputVat, 2, MidpointRounding.ToEven)
    Return BuildVatReturn(year, month, VatReturnPeriodType.Monthly, VatReturnFormType.Form2550M,
                          data, vatPayable, vatConfig.IsVatRegistered)  ' in-memory only, Id = 0
End Function
```

```vb
' VatPayableKpiProvider.ProvideAsync — before / after
' Before (write-on-read): vatReturn = Await _vatReportingService.GenerateMonthlyVatReturnAsync(year, month)
' After  (read-only):     vatReturn = Await _vatReportingService.PreviewMonthlyVatReturnAsync(year, month)
```

## Prevention

- **A KPI / dashboard / overview / "Get" read path must never call a method that `Add`s an
  entity and `SaveChangesAsync`.** If a display value needs computing, compute it in-memory and
  return an untracked object; persistence belongs to an explicit user-initiated command.
- When an `UnauthorizedWriteException` fires for the **Owner** role, suspect a *write-on-read*
  upstream — the interceptor is almost always correct. Trace the caller, do not weaken the guard.
- For genuinely system-initiated background writes, the intended bypass is
  `IWriteContextScope.Enter(WriteContextKind.System)` — but do **not** use it to let a read-only
  Owner persist financial/BIR artifacts as a view side effect. Prefer a non-persisting read path.

## Related

- `[[mariadb-pure-client-server-architecture]]` — concurrency / role model context
- Domain Wiki: `owasp-da-top10.md` (DA5 role enforcement at the data layer), `bir-compliance.md`
- Files: `MerchSys.Accounting/Services/VatPayableKpiProvider.vb`,
  `MerchSys.Accounting/Services/VatReportingService.vb`,
  `MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb`
