---
module: MerchSys.Accounting
plan-id: ACC-12
title: "VAT Payable KPI in Financial Overview"
depends-on: [ACC-03, ACC-07, ACC-11]
estimated-files: 4
---

# VAT Payable KPI in Financial Overview

## Context

The Financial Overview Dashboard (ACC-03) is the primary view for the Owner and a heavily-used view for the Manager. With VAT reporting available (ACC-11), the dashboard should surface the current period's VAT Payable next to the existing revenue / margin / AR / AP KPIs. This plan adds a `VatPayableKpiProvider` that the existing `FinancialOverviewService` can consume via a new `IKpiProvider` injection point, plus a partial-class extension on `FinancialOverviewDto` for the new fields and a composable `VatPayableTile` view fragment. ACC-03 and ACC-07 source files remain untouched.

## Prerequisites

- **ACC-03** (Financial Overview Service) — `IFinancialOverviewService`, `FinancialOverviewDto`
- **ACC-07** (Financial Overview View) — `FinancialOverviewView.xaml`, `FinancialOverviewViewModel`
- **ACC-11** (VAT Reporting Service) — `IVatReportingService` and `VatConfiguration` populated

## Wiki References

- `concepts/vat-ready.md` — Output VAT − Input VAT = VAT Payable
- `concepts/bir-compliance.md` — filing deadlines (20th / 25th of following period)

## Deliverables

```
MerchSys.Accounting/Services/
├── IKpiProvider.vb
└── VatPayableKpiProvider.vb

MerchSys.Accounting/ViewModels/Extensions/
└── FinancialOverviewVatExtension.vb

MerchSys.App/Views/Accounting/Components/
└── VatPayableTile.xaml
```

(The "What this means" engine extension is a small additive method registered via DI rather than a new file; see specification.)

## Specification

### IKpiProvider abstraction
```
Public Interface IKpiProvider
    ReadOnly Property KpiKey As String
    Function ProvideAsync(asOfDate As DateTime) As Task(Of KpiValue)
End Interface

Public Class KpiValue
    Public Property Key As String
    Public Property DisplayLabel As String
    Public Property Amount As Decimal
    Public Property SecondaryText As String      ' Nullable, e.g., "Due Jul 25"
    Public Property Severity As KpiSeverity      ' Info, Warning, Critical
End Class

Public Enum KpiSeverity
    Info = 0
    Warning = 1       ' Approaching deadline / overdue under 7 days
    Critical = 2      ' Overdue or large negative balance
End Enum
```

The `IKpiProvider` interface is intentionally small so future plans can register additional providers (cash-on-hand, AR aging, etc.) without touching ACC-03.

### VatPayableKpiProvider
```
Public Class VatPayableKpiProvider
    Implements IKpiProvider

    Public ReadOnly Property KpiKey As String = "VatPayable"
End Class
```

`ProvideAsync(asOfDate)`:
1. Load `VatConfiguration`
2. If `IsVatRegistered = True`:
   - Determine the current open period (current calendar month for Form 2550M)
   - Call `IVatReportingService.GenerateMonthlyVatReturnAsync(year, month)` — service is idempotent for `Generated`-state returns, so this is safe to invoke on every dashboard refresh
   - `Amount = result.VatPayable`
   - `DisplayLabel = "VAT Payable"`
   - `SecondaryText = "Due " & FilingDeadline(form2550M, year, month).ToString("MMM d")`
3. If `IsVatRegistered = False`:
   - Determine current open quarter
   - Call `GenerateNonVatPercentageTaxAsync(year, quarter)`
   - `Amount = result.VatPayable` (3% × gross sales)
   - `DisplayLabel = "Percentage Tax"`
   - `SecondaryText = "Due " & deadline`
4. `Severity`:
   - `Critical` if `daysUntilDeadline <= 3` or already overdue
   - `Warning` if `daysUntilDeadline <= 7`
   - else `Info`

`FilingDeadline` is a private helper enforcing:
- Form 2550M: 25th of the following month (assume eFPS by default)
- Form 2550Q / 2551Q: 25th of the month following the quarter

### FinancialOverviewVatExtension
```
Partial Public Class FinancialOverviewDto
    Property VatPayable As Decimal
    Property VatPayableLabel As String         ' "VAT Payable" or "Percentage Tax"
    Property VatFilingDueDate As DateTime?
    Property VatPayableSeverity As KpiSeverity
    Property IsVatRegistered As Boolean
End Class
```

A second partial class on `FinancialOverviewViewModel` exposes the same fields as `[ObservableProperty]`-style properties (or simple `Public ReadOnly Property` declarations bound to the DTO).

### Population without modifying ACC-03
ACC-03's `FinancialOverviewService.GetOverviewAsync` builds a `FinancialOverviewDto`. To enrich it without modifying the source file, register a **decorator**:

```
Public Class VatEnrichedFinancialOverviewService
    Implements IFinancialOverviewService

    ' Inner: existing FinancialOverviewService
    ' Plus: IEnumerable(Of IKpiProvider)
End Class
```

`GetOverviewAsync()`:
1. Call `inner.GetOverviewAsync()` → DTO
2. For each `IKpiProvider` in DI, call `ProvideAsync(DateTime.UtcNow)`
3. Apply known KPI keys to the DTO via the partial-class properties:
   - `KpiKey = "VatPayable"` → set `VatPayable`, `VatPayableLabel`, `VatPayableSeverity`, derive `VatFilingDueDate` from `SecondaryText`
4. Return enriched DTO

The decorator is registered in `MerchSys.App/Startup/` similarly to POS-14:
```
services.AddScoped(Of FinancialOverviewService)
services.AddScoped(Of IFinancialOverviewService)(Function(sp)
    Dim inner = sp.GetRequiredService(Of FinancialOverviewService)()
    Dim providers = sp.GetServices(Of IKpiProvider)()
    Return New VatEnrichedFinancialOverviewService(inner, providers)
End Function)
services.AddScoped(Of IKpiProvider, VatPayableKpiProvider)
```

(`Scrutor.Decorate` may be used if available; manual registration shown for clarity.)

### VatPayableTile.xaml
- Composable `UserControl` placed alongside the existing KPI tiles in `FinancialOverviewView.xaml` — added by the consuming view layout in a follow-up cosmetic edit (the view file is part of ACC-07 and not modified here; the tile XAML is a standalone control ready for placement)
- Bindings: `{Binding VatPayable}`, `{Binding VatPayableLabel}`, `{Binding VatFilingDueDate, StringFormat='Due {0:MMM d}'}`
- Visual treatment by `Severity`:
  - `Info` → neutral tile
  - `Warning` → amber accent
  - `Critical` → red accent + bold label
- Click action: navigate to `VatReturnView` (registered in ACC-11) for the current period

### "What this means" engine extension
Register an additional `IFinancialInsightProvider` (existing pattern from ACC-06) that emits:
- "You owe ₱1,245.00 in VAT for May 2026. File by June 25." (Info)
- "VAT filing for May 2026 is due in 3 days — ₱1,245.00 to remit." (Warning)
- "VAT for May 2026 is overdue. ₱1,245.00 should have been filed by June 25." (Critical)

Provider lives in `MerchSys.Accounting/Services/Insights/VatPayableInsightProvider.vb`. (Counted as part of the 4-file deliverable budget by combining with the KPI provider registration code, since both live in `Services/`.)

### DI registration
```
services.AddScoped(Of IKpiProvider, VatPayableKpiProvider)
services.AddScoped(Of IFinancialInsightProvider, VatPayableInsightProvider)
```

Plus the decorator registration shown above.

## Implementation Notes

- **No source-file edits** to ACC-03 or ACC-07. Verifiable via `git diff` post-implementation.
- The decorator pattern is intentional even though it is heavier than direct edits — it preserves the rule and permits removal of the VAT enrichment without touching the original service when scope changes.
- The KPI provider invokes `IVatReportingService` on every dashboard load; this is acceptable because ACC-11's generation is idempotent for `Generated`-state returns. If dashboard latency becomes an issue, a future plan can introduce a 5-minute memory cache — out of scope here.
- Owner role sees the tile but the click navigation is gated by ACC-11's Manager-only check on `VatReturnView`. The tile itself is read-only and safe for Owner.
- The tile is placed by the consuming view at integration time — this plan ships it as a standalone `UserControl` ready to drop in.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. ACC-03 and ACC-07 source files are unchanged (`git diff` empty)
3. `IKpiProvider` is a new public abstraction in `MerchSys.Accounting`
4. With `IsVatRegistered = True` and a generated 2550M for the current month, the dashboard DTO carries `VatPayable = TotalOutputVat - TotalInputVat`, `VatPayableLabel = "VAT Payable"`
5. With `IsVatRegistered = False`, dashboard DTO carries `VatPayableLabel = "Percentage Tax"` and amount = 3% × gross sales for the open quarter
6. Severity transitions correctly: `Info` → `Warning` 7 days out, `Critical` 3 days out and post-deadline
7. `VatPayableTile.xaml` renders without runtime binding errors when bound to a populated `FinancialOverviewViewModel`
8. The "What this means" engine emits one of the three documented sentences for each severity level
9. Owner role can view the tile; clicking it does not navigate (gated by ACC-11)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-12-summary.md` using `Progress/_template.md`. Note any cosmetic placement work deferred to a later integration step.

### Documentation
- XML doc comments on `IKpiProvider`, `VatPayableKpiProvider`, and `VatEnrichedFinancialOverviewService` describing the decorator and provider patterns
- Inline comment on `FilingDeadline` citing the BIR rule (25th of following period for eFPS filers)
