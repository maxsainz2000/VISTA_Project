---
module: MerchSys.Accounting
plan-id: ACC-23
title: "Export PDF — Replace Plain-Text Output with QuestPDF A4 Documents"
depends-on: [ACC-08, ACC-19, ACC-20]
estimated-files: 5
priority: high
---

# Export PDF — Replace Plain-Text Output with QuestPDF A4 Documents

## Context

The "Export PDF" button on three of four accounting views produces **plain text files** instead of actual PDF documents:

| View | Before (Broken) | Root Cause |
|---|---|---|
| **Income Statement** | Opens `ReportPreviewWindow` → saves `.txt` | `ExportPdf_Click` builds plain text and delegates to `ReportPreviewWindow` which only renders a `TextBox` and saves `.txt` |
| **VAT Relief Report** | Opens `ReportPreviewWindow` → saves `.txt` | Same pattern |
| **VAT Return** | Saves `.pdf.txt` via `ExportReadyEventArgs` | `VatReturnExporter.ExportPdfAsync` renders an embedded plain-text template into `Encoding.UTF8.GetBytes` and returns a `MemoryStream` of text. The ViewModel sets the file extension to `.pdf.txt` and filter to `"Text files (*.txt)"` |
| **Tamper Audit Report** | ✅ Already correct | Uses `TamperReportExporter` → QuestPDF `Document.Create()` → `GeneratePdf()` |

QuestPDF (version `2026.5.0`) is already referenced by `MerchSys.Accounting.vbproj`. `TamperReportExporter` serves as the reference implementation.

User requested that `ReportPreviewWindow` be preserved and upgraded to preview the A4 PDF page images (not plain text), with "Save As .txt" changed to "Save As PDF" and "Print" preserved.

## Prerequisites

- **ACC-08** (Income Statement View) — `IncomeStatementView.xaml.vb` with `ExportPdf_Click`
- **ACC-19** (VAT Relief Report) — `VatReliefReportView.xaml.vb` with `ExportPdf_Click`
- **ACC-20** (Tamper Report Export) — `TamperReportExporter` (reference QuestPDF pattern)

## Deliverables

```
MerchSys.Accounting/Services/Reporting/
├── ReportPdfResult.vb                      ' NEW — shared DTO: PdfBytes + PageImages + SuggestedFileName
├── IIncomeStatementPdfExporter.vb          ' NEW — interface
├── IncomeStatementPdfExporter.vb           ' NEW — QuestPDF A4 implementation
├── IVatReliefPdfExporter.vb               ' NEW — interface
└── VatReliefPdfExporter.vb                ' NEW — QuestPDF A4 implementation

MerchSys.Accounting/Services/
└── VatReturnExporter.vb                    ' MOD — ExportPdfAsync rewritten from plain-text template to QuestPDF

MerchSys.Accounting/ViewModels/
└── VatReturnViewModel.vb                   ' MOD — file extension .pdf.txt → .pdf; filter → "PDF files (*.pdf)|*.pdf"

MerchSys.App/Views/Shell/
├── ReportPreviewWindow.xaml                ' MOD — TextBox replaced with ItemsControl of page images
└── ReportPreviewWindow.xaml.vb             ' MOD — accepts ReportPdfResult; Save As PDF; Print from images

MerchSys.App/Views/Accounting/
├── IncomeStatementView.xaml.vb             ' MOD — inject IIncomeStatementPdfExporter; ExportPdf_Click generates PDF + opens preview
└── VatReliefReportView.xaml.vb             ' MOD — inject IVatReliefPdfExporter; ExportPdf_Click generates PDF + opens preview

MerchSys.App/
└── Application.xaml.vb                     ' MOD — register IIncomeStatementPdfExporter, IVatReliefPdfExporter
```

New source files: 5 (ReportPdfResult, IIncomeStatementPdfExporter, IncomeStatementPdfExporter, IVatReliefPdfExporter, VatReliefPdfExporter)

## Specification

### New Service: `IncomeStatementPdfExporter`

- Implements `IIncomeStatementPdfExporter.GenerateAsync(vm As IncomeStatementViewModel) As Task(Of ReportPdfResult)`
- Resolves business identity (name, TIN, address) via `GetVatConfigurationQuery` through MediatR
- Renders an A4 portrait QuestPDF document with:
  - Header: business name, address, TIN, report title, period description, generated timestamp
  - Comparative P&L table: Line Item / Current / Prior / Change columns
  - Product Breakdown table (if products exist): Product / Revenue / COGS / Gross Profit / Margin % / Units
  - Footer: paginated "page X of Y"
- Generates both `PdfBytes` (via `GeneratePdf()`) and `PageImages` (via `GenerateImages(dpi=150)`) on a background thread

### New Service: `VatReliefPdfExporter`

- Same pattern as above for `VatReliefReportViewModel`
- Renders Sales Summary, Purchases Summary, Net VAT Payable, Interpretation text, and Trailing 12-Month Trend table

### Modified: `VatReturnExporter.ExportPdfAsync`

- Replaced plain-text template rendering with QuestPDF `Document.Create()` → structured PDF table with BIR form line numbers, descriptions, and amounts
- Returns `MemoryStream` containing real PDF bytes

### Modified: `VatReturnViewModel`

- File extension: `.pdf.txt` → `.pdf`
- Dialog filter: `"Text files (*.txt)|*.txt|All files (*.*)|*.*"` → `"PDF files (*.pdf)|*.pdf"`

### Modified: `ReportPreviewWindow`

- XAML: `TextBox` replaced with `ItemsControl` inside `ScrollViewer`, rendering `BitmapImage` items with paper-like drop shadow
- Code-behind: new constructor accepts `(reportTitle, ReportPdfResult)`. Converts page image byte arrays to `BitmapImage` sources. "Save As PDF..." writes `PdfBytes` via `SaveFileDialog` with `.pdf` filter. "Print" renders page images through `PrintDialog.PrintVisual`.

### Modified: View Code-Behinds

- `IncomeStatementView` and `VatReliefReportView` constructors now accept an additional `IIncomeStatementPdfExporter` / `IVatReliefPdfExporter` parameter via DI
- `ExportPdf_Click` generates the PDF via the exporter, then opens `ReportPreviewWindow` with the result

### Modified: `Application.xaml.vb`

- Two new Scoped registrations: `IIncomeStatementPdfExporter → IncomeStatementPdfExporter`, `IVatReliefPdfExporter → VatReliefPdfExporter`

## Issues Encountered

- **`Unit` type ambiguity (BC30561):** Both `MediatR` and `QuestPDF.Infrastructure` export a `Unit` type. Resolved by fully qualifying all usages as `QuestPDF.Infrastructure.Unit.Point` in the three exporter files.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. Clicking "Export PDF" on Income Statement opens `ReportPreviewWindow` showing A4 page images (not plain text).
3. "Save As PDF..." saves a file that opens correctly in any PDF viewer.
4. "Print" prints the rendered page images via the system PrintDialog.
5. Same behavior for VAT Relief Report.
6. VAT Return "Export PDF" saves a `.pdf` file (not `.pdf.txt`) that opens as a real PDF.
7. Tamper Audit Report PDF export is unchanged and still works.

## Out of Scope

- Deleting `ReportPreviewWindow` — preserved per user request.
- Removing the old `LoadTemplate` / `ApplyTemplate` / `FallbackTemplate` methods from `VatReturnExporter` — they remain for potential CSV template use and backwards compatibility.
- Adding PDF preview to the VAT Return export flow (it uses the event-based direct-save pattern, not the preview window).
