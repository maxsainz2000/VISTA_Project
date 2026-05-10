---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/Accounting/11-vat-reporting-service.md
status: completed
---

## Task Summary

Implemented the full BIR VAT Reporting Service pipeline for ACC-11. This includes MediatR handlers that consume VAT-enriched events, `IVatReportingService` that generates/files/amends BIR Form 2550M (monthly VAT), Form 2550Q (quarterly VAT), and Form 2551Q (quarterly 3% percentage tax), a `VatReturnExporter` for CSV and plain-text PDF export, and a Manager-only WPF view to preview, lock, and export returns.

**Plan:** `Plans/VISTA_Modules/Accounting/11-vat-reporting-service.md`

## What Was Done

**New files — SharedKernel:**
- Created `MerchSys.SharedKernel/Queries/GetVatConfigurationQuery.vb` — cross-module MediatR query for POS VatConfiguration; follows GetProductCostQuery pattern
- Created `MerchSys.SharedKernel/Queries/GetVatConfigurationResult.vb` — result DTO: `IsVatRegistered`, `VatRate`, `NonVatPercentageTaxRate`

**New files — MerchSys.POS:**
- Created `MerchSys.POS/Handlers/GetVatConfigurationQueryHandler.vb` — reads `POSDbContext.VatConfigurations` AsNoTracking; returns safe defaults with warning if row is missing

**New files — MerchSys.Accounting:**
- Created `MerchSys.Accounting/Exceptions/VatReturnLockedException.vb` — inherits `InvalidOperationException`; properties: `ReturnId`, `Year`, `Period`, `FormType`
- Created `MerchSys.Accounting/Handlers/SaleCompletedWithVatHandler.vb` — handles `SaleCompletedWithVatEvent`; idempotency on `(SourceTransactionId, ProductId)`; updates VAT columns if record exists, creates full record otherwise
- Created `MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb` — handles `GoodsReceivedWithVatEvent`; idempotency via `(SourceModule, SourceReferenceId.HasValue + .Value, Description.Contains)`; populates three-bucket VAT on ExpenseRecord
- Created `MerchSys.Accounting/Services/IVatReportingService.vb` — interface with Generate/Get/List/File/Amend methods
- Created `MerchSys.Accounting/Services/VatReportingService.vb` — full implementation; uses IMediator to get VatConfiguration cross-module; private `LedgerData` class aggregates three-bucket totals; `GuardAndClearExistingAsync` enforces lock on Filed returns; `BuildVatReturn` maps all BIR line numbers (2550M: 1-23, 2550Q: 1-28, 2551Q: 1-14); banker's rounding throughout
- Created `MerchSys.Accounting/Services/VatReturnExporter.vb` — both `IVatReturnExporter` interface and `VatReturnExporter` class; CSV export enumerates all form-specific BIR lines; PDF export reads embedded `.template` resource with `{{Placeholder}}` substitution, falls back to built-in string if resource missing; returns `MemoryStream`
- Created `MerchSys.Accounting/ViewModels/VatReturnViewModel.vb` — `VatReturnLineRow`, `ExportReadyEventArgs`, full ViewModel with IsForm2550M/Q/R boolean properties, `UpdateWhatThisMeans()`, `AsyncRelayCommand` for Generate/File/Amend/ExportCSV/ExportPDF; export commands raise `ExportReady` event (keeps ViewModel in class library without WPF reference)
- Created `MerchSys.Accounting/Migrations/20260515100000_FixVatReturnAmendedIndex.vb` — drops full unique index on `(Year, Period, PeriodType, FormType)`; recreates as partial unique index `WHERE FilingStatus != 3` to allow Amended rows without constraint violation

**New files — MerchSys.App:**
- Created `MerchSys.App/Views/Accounting/VatReturnView.xaml` — Manager-only UserControl; toolbar, "What This Means" strip, Year/Period ComboBoxes, form-type RadioButtons bound to IsForm2550M/Q, summary KPI cards (5 columns), Generate/File/Amend/ExportCSV/ExportPDF buttons, Lines DataGrid
- Created `MerchSys.App/Views/Accounting/VatReturnView.xaml.vb` — code-behind subscribes to `ExportReady`; handles `SaveFileDialog` and writes stream to chosen path

**New files — Templates:**
- Created `MerchSys.Accounting/Reports/Templates/Form2550M.template` — plain-text BIR Form 2550M layout with `{{Placeholder}}` substitution markers
- Created `MerchSys.Accounting/Reports/Templates/Form2550Q.template` — quarterly VAT form template
- Created `MerchSys.Accounting/Reports/Templates/Form2551Q.template` — non-VAT percentage tax form template

**Modified files:**
- Modified `MerchSys.Accounting/MerchSys.Accounting.vbproj` — added `<EmbeddedResource>` entries for all three `.template` files
- Modified `MerchSys.App/Data/DatabaseInitializer.vb` — added `ApplyVatLedgerColumns` (creates `Acc_VatReturns`, `Acc_VatReturnLines`, adds VAT columns to Revenue/Expense tables, backfills zeros) and `ApplyFixVatReturnAmendedIndex` (partial unique index migration) — fills the gap left by ACC-10 which defined the EF migration file but never wired it into the initializer
- Modified `MerchSys.App/Application.xaml.vb` — registered `IVatReportingService`, `IVatReturnExporter`, `VatReturnViewModel`, `VatReturnView` with DI
- Modified `MerchSys.App/ViewModels/MainWindowViewModel.vb` — extracted `BuildAccountingNavItems()`; injects `ISessionService`; VAT Return nav item gated on `UserRole.Manager`
- Modified `MerchSys.POS/Services/VatConfigurationLoader.vb` — added missing `Imports System.Threading` (pre-existing BC30002 from ACC-10)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A — testing phase is separate |
| Manual verification | N/A — testing phase is separate |

## Issues Encountered

- **Issue:** BC36637/BC30370/BC30035 in `VatReturnViewModel.vb` — C#-style `??` null-coalescing operator used inside string interpolation (`{vatReturn.Lines?.Count ?? 0}`)
  - **Resolution:** Replaced with VB.NET `If()` ternary: `{If(vatReturn.Lines?.Count, 0)}`

- **Issue:** XAML `EnumBoolConverter` reference for RadioButton `IsChecked` bindings — converter did not exist in project
  - **Resolution:** Added `IsForm2550M`, `IsForm2550Q`, `IsForm2551Q` boolean properties to `VatReturnViewModel`; bound `IsChecked` directly without a converter; each property setter updates `SelectedFormType` and raises `OnPropertyChanged` for all dependent properties

- **Issue:** BC42016 implicit `Boolean?` to `Boolean` in `GoodsReceivedWithVatHandler.vb` — `e.SourceReferenceId = poId` where both sides are `Integer?` returns `Boolean?`
  - **Resolution:** Changed to `e.SourceReferenceId.HasValue AndAlso e.SourceReferenceId.Value = poId` with `poId As Integer` (non-nullable); EF Core translates `.HasValue` → `IS NOT NULL` and `.Value` → column reference correctly

- **Issue:** BC30002 `SemaphoreSlim` not defined in `VatConfigurationLoader.vb` — pre-existing omission from ACC-10
  - **Resolution:** Added `Imports System.Threading`

- **Issue:** Amendment would violate unique index on `(Year, Period, PeriodType, FormType)` — `AmendReturnAsync` creates a second row for the same period with `FilingStatus = Amended`
  - **Resolution:** Added migration `20260515100000_FixVatReturnAmendedIndex` that replaces the full unique index with a partial unique index `WHERE FilingStatus != 3`

- **Issue:** ACC-10 EF migration file was created but never added to `DatabaseInitializer.vb` — `Acc_VatReturns` and related columns would never be applied at runtime
  - **Resolution:** ACC-11 wires in both `ApplyVatLedgerColumns` and `ApplyFixVatReturnAmendedIndex` into the initializer; the migration IDs are consistent with ACC-10's file names

## PDF Library Decision

No PDF library (e.g., iTextSharp, PdfPig) is included in this project. Per plan specification, the PDF exporter uses a `.pdf.txt` plain-text fallback format: the exporter reads the embedded `.template` file, performs `{{Placeholder}}` substitution, and returns the result as a UTF-8 `MemoryStream`. The `SaveFileDialog` filter in the View's code-behind is set to `"PDF Text (*.pdf.txt)|*.pdf.txt"`. This produces a human-readable BIR form layout without requiring a PDF rendering dependency.

## What's Next

- [ ] INT-01: Cross-module integration wiring (MediatR publisher/subscriber setup at App startup)
- [ ] INT-02 onward: Integration plans per the implementation order defined in CLAUDE.md

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/concepts/bir-compliance.md`, `LLM_Wiki/wiki/analysis/cross-module-data-flow.md`
- Codebase Wiki consulted: `LLM_Wiki/codebase_wiki/index.md`, `LLM_Wiki/codebase_wiki/modules/accounting.md`
