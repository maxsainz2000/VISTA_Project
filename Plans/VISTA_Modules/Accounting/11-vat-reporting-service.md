---
module: MerchSys.Accounting
plan-id: ACC-11
title: "BIR VAT Reporting Service & Views"
depends-on: [ACC-10, POS-14, INFRA-07]
estimated-files: 9
---

# BIR VAT Reporting Service & Views

## Context

ACC-10 added the schema; this plan turns it into BIR-form output. Two MediatR handlers consume `SaleCompletedWithVatEvent` and `GoodsReceivedWithVatEvent` (INFRA-07) to keep ledger VAT columns and the running input/output totals in sync. `IVatReportingService` produces three reports — Form 2550M (monthly VAT), Form 2550Q (quarterly VAT), Form 2551Q (quarterly 3% percentage tax for non-VAT mode). A WPF view lets the Manager preview, lock, and export each return to CSV and PDF in BIR cell-for-cell layout.

## Prerequisites

- **ACC-10** (VAT Ledger Schema Extension) — `VatReturn`, `VatReturnLine`, ledger VAT columns
- **POS-14** (VAT Configuration & Calculation) — publishes `SaleCompletedWithVatEvent` with three-bucket totals
- **INFRA-07** (VAT Event Payload) — event contracts

## Wiki References

- `concepts/vat-ready.md` — output VAT vs input VAT; Form 2550 line numbers
- `concepts/bir-compliance.md` — filing windows, percentage tax for non-VAT
- `Sources/POS-Module_AcademicPaper.md` — VAT reporting requirement

## Deliverables

```
MerchSys.Accounting/Handlers/
├── SaleCompletedWithVatHandler.vb
└── GoodsReceivedWithVatHandler.vb

MerchSys.Accounting/Services/
├── IVatReportingService.vb
├── VatReportingService.vb
└── VatReturnExporter.vb

MerchSys.Accounting/ViewModels/
└── VatReturnViewModel.vb

MerchSys.App/Views/Accounting/
├── VatReturnView.xaml
└── VatReturnView.xaml.vb

MerchSys.Accounting/Reports/Templates/
└── (CSV + PDF templates referenced by VatReturnExporter — counted as one deliverable)
```

## Specification

### SaleCompletedWithVatHandler
```
Public Class SaleCompletedWithVatHandler
    Implements INotificationHandler(Of SaleCompletedWithVatEvent)
End Class
```
On each event: write one revenue ledger row per `Items` element with the corresponding `VatableAmount` / `VatExemptAmount` / `ZeroRatedAmount` / `OutputVat` and `VatTreatment`. Idempotent on `(SourceModule="POS", SourceTable, SourceRowId=TransactionId)` — re-publish (legacy + new event during POS-14 migration window) must not double-post.

### GoodsReceivedWithVatHandler
```
Public Class GoodsReceivedWithVatHandler
    Implements INotificationHandler(Of GoodsReceivedWithVatEvent)
End Class
```
Mirror behavior on the expense/COGS ledger; `InputVat` populated, `OutputVat = 0`. Idempotency key: `(SourceModule="Purchasing", SourceTable, SourceRowId=PurchaseOrderId, ProductId)`.

### IVatReportingService
```
Public Interface IVatReportingService
    Function GenerateMonthlyVatReturnAsync(year As Integer, month As Integer) As Task(Of VatReturn)
    Function GenerateQuarterlyVatReturnAsync(year As Integer, quarter As Integer) As Task(Of VatReturn)
    Function GenerateNonVatPercentageTaxAsync(year As Integer, quarter As Integer) As Task(Of VatReturn)
    Function GetReturnAsync(returnId As Integer) As Task(Of VatReturn)
    Function ListReturnsAsync(year As Integer?) As Task(Of IReadOnlyList(Of VatReturn))
    Function FileReturnAsync(returnId As Integer, filedBy As String) As Task
    Function AmendReturnAsync(returnId As Integer) As Task(Of VatReturn)
End Interface
```

#### Generation rules

**Form 2550M (`GenerateMonthlyVatReturnAsync`):**
1. Reject if `VatConfiguration.IsVatRegistered = False` — caller should use `GenerateNonVatPercentageTaxAsync` instead
2. Window: `[year-month-01 00:00, next-month-01 00:00)` UTC
3. Pull every revenue ledger row where `TransactionDate` falls in window: sum `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`
4. Pull every expense/COGS ledger row in window: sum `VatableAmount` (as VatablePurchases) and `InputVat`
5. `VatPayable = TotalOutputVat - TotalInputVat`. Negative values are carried as a credit (stored as negative; never floored to 0)
6. Insert `VatReturn` row with `FormType = Form2550M, PeriodType = Monthly, FilingStatus = Generated`
7. Insert one `VatReturnLine` per source ledger row contributing to the totals
8. If a `VatReturn` for the same `(Year, Period, FormType)` already exists with `FilingStatus = Generated`: regenerate (delete and recreate). If `Filed`: throw `VatReturnLockedException` and direct caller to `AmendReturnAsync`.

**Form 2550Q (`GenerateQuarterlyVatReturnAsync`):**
- Window: full BIR fiscal quarter (Q1=Jan–Mar, Q2=Apr–Jun, Q3=Jul–Sep, Q4=Oct–Dec)
- Same logic as Monthly but spanning 3 months

**Form 2551Q (`GenerateNonVatPercentageTaxAsync`):**
1. Reject if `VatConfiguration.IsVatRegistered = True`
2. Window: BIR fiscal quarter
3. `TotalVatableSales` = sum of all revenue ledger rows in window (everything is treated as exempt under the non-VAT regime; "vatable" repurposed here as "gross taxable receipts")
4. `VatPayable = TotalVatableSales × VatConfiguration.NonVatPercentageTaxRate` (default 0.03)
5. `TotalOutputVat`, `TotalInputVat`, `TotalVatablePurchases` are all `0`
6. Insert `VatReturn` with `FormType = Form2551Q, PeriodType = Quarterly`

#### FileReturnAsync
- Transitions `FilingStatus` from `Generated` to `Filed`, sets `FiledAt = UtcNow`, `FiledBy = caller-supplied user identifier`
- Once `Filed`, the row is treated as immutable by the service: regenerate calls throw, and only `AmendReturnAsync` may proceed
- Does **not** rely on the POS-13 interceptor (this is a service-layer rule, not a DB-level immutability enforcement, since amendments are legitimate BIR workflows)

#### AmendReturnAsync
- Required for filed returns that need post-filing correction
- Creates a **new** `VatReturn` row with `FilingStatus = Amended` referencing the same period; the original `Filed` row is left untouched as the BIR audit trail
- Recomputes from current ledger state at amendment time

### VatReturnExporter
```
Public Interface IVatReturnExporter
    Function ExportCsvAsync(returnId As Integer) As Task(Of Stream)
    Function ExportPdfAsync(returnId As Integer) As Task(Of Stream)
End Interface
```

- CSV format mirrors BIR cell layout: one row per Form 2550 line item (line numbers 1–23 for 2550M, 1–28 for 2550Q, 1–14 for 2551Q)
- PDF generation uses an internal templated renderer (`Reports/Templates/Form2550M.template`, etc.). Templates are plain-text with `{{Placeholders}}` for cell values. The renderer is a small substitution engine — no external PDF library required beyond what `MerchSys.App` already pulls in for receipt printing. If no PDF library exists in INFRA-01's NuGet set, the exporter writes a human-readable plain-text representation in `.pdf.txt` form and flags the limitation in the implementation summary.

### VatReturnViewModel (CommunityToolkit.Mvvm)
- `[ObservableProperty] Year, Period, FormType, FilingStatus`
- `[ObservableProperty] TotalVatableSales, TotalVatExemptSales, TotalZeroRatedSales, TotalOutputVat, TotalVatablePurchases, TotalInputVat, VatPayable`
- `[ObservableProperty] Lines : ObservableCollection(Of VatReturnLineRow)`
- `[RelayCommand] GenerateAsync` — calls the corresponding service method based on the selected FormType
- `[RelayCommand] FileAsync` — confirms then calls `FileReturnAsync`
- `[RelayCommand] AmendAsync` — only enabled when `FilingStatus = Filed`
- `[RelayCommand] ExportCsvAsync` / `ExportPdfAsync`

### VatReturnView.xaml
- Period selector: Year, Period dropdown (months 1–12 or quarters 1–4 based on FormType)
- FormType radio: Form 2550M / Form 2550Q / Form 2551Q (auto-disable based on `VatConfiguration.IsVatRegistered`)
- Summary panel: every total bound to the ViewModel
- DataGrid of `Lines` — sortable, filterable, with source row drilldown
- Action buttons: Generate, File, Amend, Export CSV, Export PDF
- "What this means" plain-language strip at top, populated by `WhatThisMeansEngine` (existing in ACC-06): e.g., "You owe ₱1,245.00 to BIR for Q2 2026. Filing deadline: July 25, 2026."
- Manager-only access enforced at the navigation layer (Owner role hides this view)

### DI registration (in `MerchSys.App/Startup/`)
```
services.AddScoped(Of IVatReportingService, VatReportingService)
services.AddScoped(Of IVatReturnExporter, VatReturnExporter)
services.AddTransient(Of VatReturnViewModel)
```

The two MediatR handlers register automatically via the existing assembly scan (INFRA-04).

## Implementation Notes

- Filing deadlines are not scheduled here — the "What this means" engine reads them from a constant table:
  - Form 2550M: 20th of the following month (manual filing) or 25th (eFPS)
  - Form 2550Q: 25th day of the month after the quarter
  - Form 2551Q: 25th day of the month after the quarter
- All money values use `Decimal` precision(18,2), banker's rounding
- The view does not implement BIR eFPS submission — only export. Submission to BIR is out of scope for this phase.
- Idempotency on the two handlers is critical because POS-14 publishes both legacy and VAT-aware events for the same transaction
- `WhatThisMeansEngine` extension is **non-modifying** — call into the existing service via DI; do not edit ACC-06 source

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. Generating Form 2550M for a month with five sales totaling ₱11,200 vatable produces `TotalOutputVat = 1200.00`, `TotalVatableSales = 10000.00` (banker's rounding; verifies POS-14 → ACC-10 → ACC-11 chain)
3. Generating Form 2550M when `IsVatRegistered = False` throws and surfaces a user-actionable message
4. Generating Form 2551Q with `IsVatRegistered = True` likewise throws
5. `FileReturnAsync` transitions `Generated → Filed`; subsequent `GenerateMonthlyVatReturnAsync` for the same period throws `VatReturnLockedException`
6. `AmendReturnAsync` produces a new row with `FilingStatus = Amended`; the original `Filed` row is untouched
7. Re-publishing the same `SaleCompletedWithVatEvent` does not duplicate `VatReturnLine` entries (idempotency)
8. CSV export contains every BIR-mandated line for the selected form
9. PDF (or `.pdf.txt` fallback) is downloadable from the view
10. Owner role cannot navigate to `VatReturnView`

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-11-summary.md` using `Progress/_template.md`. Document any PDF library decision and whether `.pdf.txt` fallback is used.

### Documentation
- XML doc comments on `IVatReportingService`, `VatReturnExporter`, both handlers — citing form numbers and BIR deadlines
- Header comment in `VatReportingService.vb` listing the line-number mapping (1–23 / 1–28 / 1–14) used by the exporter
