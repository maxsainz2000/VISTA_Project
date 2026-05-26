---
module: MerchSys.Accounting
plan-id: ACC-19
title: "BIR VAT Relief Report (Monthly Three-Bucket Summary)"
depends-on: [ACC-10, ACC-11]
estimated-files: 5
priority: medium
---

# BIR VAT Relief Report (Monthly Three-Bucket Summary)

## Context

ACC-10 added the three-bucket VAT columns (`VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`, `InputVat`) to the revenue and expense ledgers. ACC-11 turned that data into the full BIR Forms 2550M/2550Q/2551Q with per-line detail.

A separate BIR compliance requirement — explicitly deferred from POS-14 and tracked in `Plans/Future/deferred-features-backlog.md` (item #1) — is the **VAT Relief Report**: a lightweight, month-scoped summary that shows running totals for each of the three VAT buckets across both sales and purchases, without the full per-line BIR form layout. The Relief data is what is reconciled against BIR's RELIEF (Reconciliation of Listings for Enforcement) system during audits and is read frequently by the bookkeeper, so it warrants a dedicated, faster-to-load view distinct from the formal Form 2550M view delivered by ACC-11.

This plan adds the report as a Manager- and Owner-accessible view (read-only by both roles) backed by a new service method that aggregates straight from the ledger tables — it does **not** read from `Acc_VatReturns`, because Relief reports are pulled ad-hoc and do not require the "generate / file / lock" lifecycle that the BIR forms do.

## Prerequisites

- **ACC-10** (VAT Ledger Schema Extension) — completed; provides the three-bucket columns on revenue + expense ledgers
- **ACC-11** (BIR VAT Reporting Service & Views) — completed; provides `IVatReportingService` and the line-number convention this report shares

## Wiki References

- `concepts/vat-ready.md` — three-bucket VAT model (Vatable / VAT-Exempt / Zero-Rated)
- `concepts/bir-compliance.md` — RELIEF reconciliation requirement
- `Sources/POS-Module_AcademicPaper.md` — origin of the deferred VAT Relief Report requirement

## Deliverables

```
MerchSys.Accounting/Services/
├── IVatReliefReportService.vb                  ' New
└── VatReliefReportService.vb                   ' New

MerchSys.Accounting/ViewModels/
└── VatReliefReportViewModel.vb                 ' New

MerchSys.App/Views/Accounting/
├── VatReliefReportView.xaml                    ' New
└── VatReliefReportView.xaml.vb                 ' New

MerchSys.App/Application.xaml.vb                ' Modified — DI registration
MerchSys.App/ViewModels/MainWindowViewModel.vb  ' Modified — nav item
```

## Specification

### VatReliefSummary (DTO)

Lives in `MerchSys.Accounting/Services/`.

```vb
Public Class VatReliefSummary
    Public Property Year As Integer
    Public Property Month As Integer

    ' Sales side (revenue ledger)
    Public Property VatableSales As Decimal
    Public Property VatExemptSales As Decimal
    Public Property ZeroRatedSales As Decimal
    Public Property OutputVat As Decimal
    Public Property TotalGrossSales As Decimal      ' Sum of the three buckets + OutputVat for cross-check

    ' Purchases side (expense ledger)
    Public Property VatablePurchases As Decimal
    Public Property VatExemptPurchases As Decimal
    Public Property ZeroRatedPurchases As Decimal
    Public Property InputVat As Decimal
    Public Property TotalGrossPurchases As Decimal

    ' Derived
    Public Property NetVatPayable As Decimal        ' OutputVat - InputVat (may be negative = credit)

    ' Row counts (for "what does this mean" interpretation)
    Public Property SalesRecordCount As Integer
    Public Property PurchaseRecordCount As Integer
End Class
```

### IVatReliefReportService

```vb
Public Interface IVatReliefReportService
    ''' <summary>
    ''' Aggregates revenue + expense ledger rows whose TransactionDate falls within the
    ''' calendar month [year-month-01 00:00 UTC, next-month-01 00:00 UTC).
    ''' Pure read; does not touch Acc_VatReturns.
    ''' </summary>
    Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of VatReliefSummary)

    ''' <summary>
    ''' Returns up to <paramref name="months"/> consecutive monthly summaries ending at
    ''' (endYear, endMonth), ordered chronologically — used by the trend grid.
    ''' </summary>
    Function GetTrailingMonthsAsync(endYear As Integer, endMonth As Integer, months As Integer) As Task(Of IReadOnlyList(Of VatReliefSummary))
End Interface
```

### VatReliefReportService

Implementation rules:

1. Use the **raw `SqliteConnection` + synchronous `reader.Read()` loop** pattern documented in `agent_wiki/errors/efcore10-vbnet-tolistasync-empty.md` — `ToListAsync()` on full entity queries silently returns empty under EF Core 10 + VB.NET.
2. Sum the three-bucket columns directly in SQL with `SUM(...)` and `COUNT(*)` — one round-trip per ledger table.
3. `TotalGrossSales = VatableSales + VatExemptSales + ZeroRatedSales + OutputVat`; same shape for purchases.
4. `NetVatPayable = OutputVat - InputVat`. Negative values are preserved (credit carryover); do not floor to zero.
5. All money values are `Decimal(18,2)` with banker's rounding applied at the boundary (`Math.Round(value, 2, MidpointRounding.ToEven)`).
6. `GetTrailingMonthsAsync` clamps `months` to `[1, 24]` and calls `GetMonthlySummaryAsync` per month.
7. Honour soft-delete: include only rows where `IsDeleted = 0` on both ledgers.

### VatReliefReportViewModel (CommunityToolkit.Mvvm)

- `[ObservableProperty] SelectedYear As Integer` (default: current year)
- `[ObservableProperty] SelectedMonth As Integer` (default: current month)
- `[ObservableProperty] Summary As VatReliefSummary`
- `[ObservableProperty] TrailingMonths As ObservableCollection(Of VatReliefSummary)`
- `[ObservableProperty] IsLoading As Boolean`
- `[ObservableProperty] WhatThisMeans As String`
- `[RelayCommand] LoadAsync` — calls both service methods, populates Summary + TrailingMonths, updates WhatThisMeans
- `[RelayCommand] RefreshAsync` — re-runs LoadAsync with the current selection

`WhatThisMeans` is populated locally (no dependency on the ACC-06 `WhatThisMeansEngine`), with template strings:
- If `NetVatPayable > 0`: "You owe ₱{NetVatPayable:N2} in net VAT for {MonthName} {Year}. This is what you'll remit when you file Form 2550M."
- If `NetVatPayable = 0`: "Your output VAT matched your input VAT for {MonthName} {Year}. Nothing to remit, nothing to carry forward."
- If `NetVatPayable < 0`: "Your input VAT exceeded output VAT by ₱{Abs(NetVatPayable):N2} for {MonthName} {Year}. This is a VAT credit that carries forward to next month."

### VatReliefReportView.xaml

A read-only view with:
- **Period selector** at top: `Year` ComboBox + `Month` ComboBox + `Refresh` button.
- **Two summary cards side-by-side**:
  - "Sales" card: rows for Vatable / VAT-Exempt / Zero-Rated / Output VAT / Total Gross / Record Count
  - "Purchases" card: rows for Vatable / VAT-Exempt / Zero-Rated / Input VAT / Total Gross / Record Count
- **Net VAT Payable** banner under the cards, large font, colour-coded:
  - Red text if `NetVatPayable > 0` (owe BIR)
  - Grey if `= 0`
  - Green if `< 0` (credit)
- **"What This Means" plain-language strip** below the banner, bound to `WhatThisMeans`.
- **Trailing 12 Months DataGrid** at the bottom: one row per month showing the same totals — for trend review.

No Generate / File / Amend / Export controls in this view (those belong to ACC-11's `VatReturnView`). This is purely a viewer.

### Navigation & DI

- `Application.xaml.vb`:
  ```vb
  services.AddScoped(Of IVatReliefReportService, VatReliefReportService)
  services.AddTransient(Of VatReliefReportViewModel)
  services.AddTransient(Of VatReliefReportView)
  ```
- `MainWindowViewModel.BuildAccountingNavItems()`: append a new nav item **"VAT Relief Report"** after the "VAT Return" item. Accessible to **Manager and Owner** (Owner is read-only, which this view already is).

## Implementation Notes

- **Do not** add a new EF entity. The service reads via raw `SqliteConnection`; it does not need a `DbSet`.
- **Do not** add columns to `Acc_VatReturns` — Relief reporting is ad-hoc and does not get persisted as a return.
- Connection string is obtained from `IConfiguration` the same way `VatReportingService` reads it (mirror the existing pattern in `MerchSys.Accounting/Services/VatReportingService.vb`).
- Banker's rounding is applied at the DTO boundary, not in SQL — keep SQL as plain `SUM(...)`.
- Format `WhatThisMeans` with `CultureInfo("en-PH")` so the ₱ symbol renders correctly; mirror what ACC-06's `WhatThisMeansEngine` does.
- Take care with the `Console` namespace shadow trap when `Microsoft.Extensions.Logging` is imported in the service file — use `System.Console` if logging fallback is needed.
- Owner-role write rejection from the data layer is **out of scope** here and tracked separately as deferred item #7 (DA5 Data-Layer Write Rejection).

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with 0 errors, 0 warnings.
2. Navigation item "VAT Relief Report" appears in the Accounting sidebar for both Manager and Owner sessions.
3. Loading the report for a month with mixed sales (vatable + exempt + zero-rated) shows the correct totals against the underlying ledger.
4. `NetVatPayable` matches `OutputVat - InputVat` exactly (verifies signed-decimal handling).
5. A month with no transactions returns a `VatReliefSummary` with all-zero buckets and counts (no exception, no null DTO).
6. The "Trailing 12 Months" grid displays exactly the requested number of months in chronological order.
7. `WhatThisMeans` text reflects the sign of `NetVatPayable` (owe / square / credit branches).
8. Owner role can navigate to and view the report (read-only); no edit controls are present for either role.
9. No runtime XAML binding errors when loading the view fresh.
10. Soft-deleted ledger rows are excluded from all aggregates.

## Output Requirements

### Implementation Summary

Create at: `Progress/VISTA_Modules/Accounting/ACC-19-summary.md` using `Progress/_template.md`. In particular, document:
- The exact SQL aggregate queries used against the revenue and expense ledger tables
- Whether any pre-existing helper (e.g. a connection-string accessor) was reused or re-implemented
- Confirmation that ledger soft-deletes are honoured

### Documentation

- XML doc comments on `IVatReliefReportService` describing the calendar-month window and that the service is read-only and stateless
- Header comment in `VatReliefReportService.vb` noting why raw `SqliteConnection` is used instead of `ToListAsync` (cite `agent_wiki/errors/efcore10-vbnet-tolistasync-empty.md`)

## Backlog Cleanup

On completion of ACC-19, remove item **#1 BIR VAT Relief Report** from `Plans/Future/deferred-features-backlog.md` and update the `last-synced` date at the top of that file.
