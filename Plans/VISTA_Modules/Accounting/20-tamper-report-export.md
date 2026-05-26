---
module: MerchSys.Accounting
plan-id: ACC-20
title: "Tamper Incident Report — CSV / PDF Export"
depends-on: [ACC-15, ACC-18, POS-19]
estimated-files: 9
---

# Tamper Incident Report — CSV / PDF Export

## Context

ACC-15 persists tamper events to `Acc_TamperAuditLog` and exposes them through `ITamperAuditQueryService`. ACC-18 added the `TamperAuditReportView` UI on top of that service, with a date-range filter and a `DataGrid` rendering of incidents. The ACC-18 plan and the 2026-05-17 audit both explicitly defer **file export to CSV/PDF** for BIR auditor submission to a follow-up plan. This is that plan.

Today the audit data is only viewable inside the application — there is no way to hand a copy to a BIR examiner, attach evidence to an incident response, or archive a snapshot for the company's own records. Exports are the missing primitive.

POS-19 introduced `QuestPDF` to the project for receipt PDF generation. That same library covers the PDF half of this plan; the CSV half is a single text writer with no new dependencies. Both formats are produced from the same in-memory list, so the View can offer them as parallel options without round-tripping back to the database.

## Prerequisites

- **ACC-15** (Receipt Tamper Audit Handler) — `TamperAuditEntry` entity, `ITamperAuditQueryService.GetIncidentsAsync`
- **ACC-18** (Tamper Incident Report UI) — `TamperAuditReportViewModel`, `TamperAuditEntryDto`, `TamperAuditReportView.xaml`
- **POS-19** (PDF Receipt Rendering) — `QuestPDF` NuGet already present in the solution; Community License declaration pattern established

## Wiki References

- `concepts/bir-compliance.md` — BIR audit evidence requirements; immutable export expectation
- `concepts/owasp-da-top10.md` — Audit-log visibility and integrity

## Deliverables

```
MerchSys.Accounting/Services/Reporting/
├── ITamperReportExporter.vb                          ' New abstraction
├── TamperReportFormat.vb                             ' Enum: Csv, Pdf
├── TamperReportExportOptions.vb                      ' Strongly-typed config
├── TamperReportExporter.vb                           ' New — implements ITamperReportExporter (CSV + PDF)

MerchSys.Accounting/ViewModels/
└── TamperAuditReportViewModel.vb                     ' Modified — add ExportCsvCommand / ExportPdfCommand

MerchSys.App/Views/Accounting/
├── TamperAuditReportView.xaml                        ' Modified — Export buttons; SaveFileDialog wiring
└── TamperAuditReportView.xaml.vb                     ' Modified — code-behind opens dialog, forwards path

MerchSys.App/Startup/AccountingServiceRegistration.vb ' Modified — register exporter + QuestPDF license + options binding

MerchSys.Accounting/MerchSys.Accounting.vbproj       ' Modified — add QuestPDF PackageReference
```

The QuestPDF package is the only new dependency. The CSV writer uses `System.IO.StreamWriter` and `System.Text.Encoding.UTF8` from BCL — no NuGet additions for CSV.

## Specification

### TamperReportFormat enum

```vb
Public Enum TamperReportFormat
    Csv = 0
    Pdf = 1
End Enum
```

Both formats are simultaneously available — the user picks at export time, not at startup. This is the key contrast with `IReceiptRenderer` (POS-19), where one renderer is active per installation.

### ITamperReportExporter

```vb
Public Interface ITamperReportExporter

    ''' <summary>
    ''' Writes the supplied incidents to <paramref name="targetPath"/> in the requested
    ''' format. Caller is responsible for choosing the path (typically via SaveFileDialog).
    ''' Throws <see cref="InvalidOperationException"/> if the target file already exists.
    ''' </summary>
    Function ExportAsync(
        incidents As IReadOnlyList(Of TamperAuditEntry),
        dateFrom As DateTime,
        dateTo As DateTime,
        format As TamperReportFormat,
        targetPath As String,
        cancellationToken As CancellationToken
    ) As Task

End Interface
```

The exporter takes raw `TamperAuditEntry` rows (not the DTO) so exports preserve fields the on-screen grid truncates or omits — BIR examiners need the *full* hash values, the tamper kind, the detecting service, the machine name, and the operating user.

### TamperReportExportOptions

```vb
Public Class TamperReportExportOptions
    Public Property DefaultDirectory As String = "%LOCALAPPDATA%\MerchSys\TamperReports"
    Public Property CsvFileNamePattern As String = "TamperReport-{YYYYMMDD-HHmm}.csv"
    Public Property PdfFileNamePattern As String = "TamperReport-{YYYYMMDD-HHmm}.pdf"
    Public Property PdfFontFamily As String = "Consolas"
    Public Property PdfFontSizePt As Double = 9.0
End Class
```

Bound from `Accounting:TamperReport:Export` in `appsettings.json`. The View consults this to seed the `SaveFileDialog`'s initial directory and filename; the user can override either before saving.

Token substitution applied to the pattern when the View opens the save dialog:
- `{YYYYMMDD-HHmm}` → current local time formatted

### TamperReportExporter behaviour

`ExportAsync` performs three steps regardless of format:

1. **Pre-flight guards** — throw `InvalidOperationException` if `File.Exists(targetPath)`. Create the parent directory if needed. Never overwrite. These exports may be submitted to BIR, and a silent overwrite would destroy evidence.
2. **Dispatch on format** — call `WriteCsv` or `WritePdf`.
3. **Return** — the file handle is closed inside the writer; no cleanup needed at the caller.

#### CSV format (RFC 4180)

- UTF-8 with BOM (so Excel autodetects encoding).
- Comma-delimited, CRLF line endings.
- All fields double-quoted; embedded `"` doubled.
- Header row (in this order — must match the BIR-friendly evidence shape):
  ```
  DetectedAtUtc,DetectedAtLocal,ReceiptId,ReceiptNumber,TamperKind,Severity,DetectedByService,ExpectedHash,ActualHash,MachineName,OperatingUser,AdditionalContextJson,CreatedAtUtc,CreatedBy
  ```
- One row per incident in `incidents` order (caller pre-sorts; the query service already returns `ORDER BY DetectedAt DESC`).
- `DetectedAtUtc` and `CreatedAtUtc` use round-trip ISO 8601 (`"o"` format). `DetectedAtLocal` uses `"yyyy-MM-dd HH:mm:ss"`.
- Empty `IReadOnlyList` still produces a file: header row only, no data rows. The file's existence is itself evidence ("no incidents detected in the period").

#### PDF format

- A4 portrait (more rows fit per page than the receipt's A5; tamper reports can have many incidents).
- QuestPDF document with:
  - Header block — business name, address, TIN (read from `VatConfiguration` if available, blank if not), report title "Tamper Audit Report", date range "From {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}", generated timestamp.
  - Tabular body with one row per incident. Columns: Detected (Local), Receipt #, Kind, Severity, Expected Hash, Actual Hash, Machine, Operator. The two hash columns wrap onto two lines per row if necessary so the full 64-char SHA-256 is preserved.
  - Footer per page — "Generated by MerchSys POS — page {n} of {m}".
  - If `incidents.Count = 0`, the body shows a single centered line: "No tamper incidents detected in the selected period."
- Font from `PdfFontFamily` / `PdfFontSizePt`; QuestPDF substitutes silently if the family is missing (same caveat as POS-19).

### ViewModel changes (`TamperAuditReportViewModel.vb`)

Add two `AsyncRelayCommand` properties:

```vb
Public ReadOnly Property ExportCsvCommand As AsyncRelayCommand(Of String)
Public ReadOnly Property ExportPdfCommand As AsyncRelayCommand(Of String)
```

Each command takes the chosen file path as its parameter (the View supplies it after the user picks via `SaveFileDialog`). Implementation:

```vb
Public Async Function ExportCsvAsync(targetPath As String) As Task
    If String.IsNullOrWhiteSpace(targetPath) Then Return        ' user cancelled
    Dim entries = Await _queryService.GetIncidentsAsync(DateFrom.Date, DateTo.Date.AddDays(1).AddTicks(-1))
    Await _exporter.ExportAsync(entries, DateFrom, DateTo, TamperReportFormat.Csv, targetPath, CancellationToken.None)
End Function
```

`ExportPdfAsync` is structurally identical with `TamperReportFormat.Pdf`. Both commands are disabled while `IsLoading` is True. Inject `ITamperReportExporter` via the existing constructor; expose `Options` so the View can read the default filename pattern.

`CanExecute` for both commands: `Not IsLoading AndAlso _entries IsNot Nothing`.

### View changes (`TamperAuditReportView.xaml` + `.xaml.vb`)

Add two buttons in the existing date-range/filter row: **"Export to CSV"** and **"Export to PDF"**. Disabled while loading or before first load.

Click handler in code-behind (NOT in the ViewModel — `SaveFileDialog` is a UI primitive and the ViewModel must remain platform-agnostic):

```vb
Private Sub ExportCsv_Click(sender As Object, e As RoutedEventArgs)
    Dim vm = DirectCast(DataContext, TamperAuditReportViewModel)
    Dim suggested = ResolveSuggestedPath(vm.Options.DefaultDirectory, vm.Options.CsvFileNamePattern)
    Dim dialog As New Microsoft.Win32.SaveFileDialog() With {
        .Filter = "CSV files (*.csv)|*.csv",
        .FileName = Path.GetFileName(suggested),
        .InitialDirectory = Path.GetDirectoryName(suggested),
        .OverwritePrompt = True
    }
    If dialog.ShowDialog() = True Then
        Await vm.ExportCsvCommand.ExecuteAsync(dialog.FileName)
    End If
End Sub
```

`ResolveSuggestedPath` expands `%LOCALAPPDATA%` and substitutes the `{YYYYMMDD-HHmm}` token. PDF handler is symmetric.

### DI registration (`AccountingServiceRegistration.vb`)

```vb
' QuestPDF license must be declared before any Document.Create call. Setting it
' here is idempotent with PosServiceRegistration; both modules ship the same
' Community License declaration so neither depends on the other being loaded first.
QuestPDF.Settings.License = LicenseType.Community

services.AddOptions(Of TamperReportExportOptions)().BindConfiguration("Accounting:TamperReport:Export")
services.AddScoped(Of ITamperReportExporter, TamperReportExporter)()
```

### Configuration model

`appsettings.json` additions under the existing `Accounting` section:

```json
"Accounting": {
  "TamperReport": {
    "Export": {
      "DefaultDirectory": "%LOCALAPPDATA%\\MerchSys\\TamperReports",
      "CsvFileNamePattern": "TamperReport-{YYYYMMDD-HHmm}.csv",
      "PdfFileNamePattern": "TamperReport-{YYYYMMDD-HHmm}.pdf",
      "PdfFontFamily": "Consolas",
      "PdfFontSizePt": 9
    }
  }
}
```

### `MerchSys.Accounting.vbproj` change

```xml
<PackageReference Include="QuestPDF" Version="2026.5.0" />
```

Pin the same version as `MerchSys.POS.vbproj` to avoid two QuestPDF versions resolving simultaneously in the same `AppDomain` (NuGet would warn). Bump both projects together when QuestPDF is upgraded.

## Implementation Notes

- **No write to any DbContext.** Exports are pure reads. `IWriteContextScope` is not involved; both Manager and Owner can export per ACC-15 / INFRA-20 / INFRA-16 visibility rules.
- **The exporter takes `TamperAuditEntry`, not `TamperAuditEntryDto`.** The DTO truncates hashes for grid display; exports must carry full values.
- **`SaveFileDialog` is platform-bound.** It lives in `MerchSys.App` code-behind. The ViewModel exposes commands that *accept* a path; the View *produces* one. This keeps the ViewModel platform-agnostic and the test seams clean.
- **No-overwrite guard is mandatory.** Audit exports are evidence; silent overwrite is unacceptable. The `SaveFileDialog`'s `OverwritePrompt = True` covers the UX path, but the exporter also enforces the rule internally so programmatic callers cannot bypass it.
- **CSV BOM matters.** Excel without BOM frequently mis-detects encoding and corrupts Unicode characters (e.g. the "₱" glyph that could appear in `AdditionalContextJson`). Use `New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)`.
- **Hash columns wrap in PDF.** A 64-char SHA-256 will not fit on one line at 9pt Consolas in A4. QuestPDF's text wrap handles this; do not truncate.
- **Empty result set is still exported.** An export of zero incidents is itself the evidence "we ran the report and there were none." Both writers must produce a valid file in that case.
- **VB.NET traps:**
  - `Console` namespace shadow — use `System.Console.WriteLine` if any logging falls back to console.
  - `Await in Catch/Finally` (BC36943) — capture exception state outside, await after.
  - **BC30980 — local-name collision with imported QuestPDF types.** Do not name a local `Document`, `Page`, `Column`, `Text`. Use `doc`, `pg`, `col`, `txt`. (Same trap surfaced in POS-19.)
  - `entry` in DbContext loop — N/A here, this code does not enumerate `ChangeTracker.Entries()`.
  - Parameter-shadows-property — when the View click handler builds a `SaveFileDialog`, do not name a parameter `dialog` if any class member uses that name.
- **License declaration is idempotent.** Setting `QuestPDF.Settings.License = LicenseType.Community` in both `AccountingServiceRegistration.AddAccountingModule` and `PosServiceRegistration.AddPosModule` is intentional — neither module loads conditionally on the other, and the static set is cheap.
- **Default directory creation.** If `DefaultDirectory` does not exist when the user accepts the suggested path, `TamperReportExporter` creates it via `Directory.CreateDirectory`. The `SaveFileDialog` already creates the directory if the user picks one that doesn't exist.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings, including no NU1902/NU1903 advisories.
2. `ITamperReportExporter.ExportAsync` produces a valid CSV file with UTF-8 BOM, comma-delimited, CRLF line endings, and the exact 14-column header specified above.
3. CSV double-quote escaping is correct for fields containing commas, quotes, or newlines (verified against a row whose `AdditionalContextJson` contains an embedded quote).
4. `ITamperReportExporter.ExportAsync` produces a valid PDF file with A4 portrait layout, the business header block, the date range, and one row per incident.
5. PDF hash columns wrap on two lines if needed; no truncation of the 64-character SHA-256 values.
6. Both exporters throw `InvalidOperationException` (and do not overwrite) when a file already exists at the target path.
7. Empty incident list still produces a valid CSV (header only) and a valid PDF ("No tamper incidents detected" centered).
8. The `TamperAuditReportView` shows two new buttons — "Export to CSV" and "Export to PDF" — disabled until the report has loaded data.
9. Clicking either button opens a `SaveFileDialog` with the suggested filename derived from `TamperReportExportOptions.*FileNamePattern` and `{YYYYMMDD-HHmm}` substituted.
10. Cancelling the `SaveFileDialog` is a silent no-op; no error toast, no log warning, no file created.
11. Manager and Owner can both invoke export (read-side; no `IWriteContextScope` interaction required).
12. `MerchSys.Accounting.vbproj` and `MerchSys.POS.vbproj` reference the **same** QuestPDF version. (Locked: 2026.5.0 at plan-write time.)
13. `QuestPDF.Settings.License = LicenseType.Community` is set in `AccountingServiceRegistration.AddAccountingModule` and `PosServiceRegistration.AddPosModule` — both calls present, both idempotent.

## Output Requirements

### Implementation Summary
Create at `Progress/VISTA_Modules/Accounting/ACC-20-summary.md` using `Progress/_template.md`. Include:

- Pinned QuestPDF version in `MerchSys.Accounting.vbproj` and confirmation it matches `MerchSys.POS.vbproj`.
- A short sample of CSV output (header + 1–2 rows) showing correct quoting on a synthetic row with an embedded comma and quote.
- A text dump of the PDF header block + first incident row, captured during manual verification.
- A note confirming behaviour on an empty result set for both formats.
- A `git diff` excerpt showing the `TamperAuditReportView.xaml.vb` click handler invoking `SaveFileDialog` and the ViewModel command.
- A `## Cross-Module QuestPDF Posture` section confirming both vbproj files pin the same version and both DI registrations declare the Community License.

### Documentation
- XML doc on `ITamperReportExporter.ExportAsync` describing the no-overwrite guard and the empty-list behaviour.
- XML doc on `TamperReportExporter.WriteCsv` citing RFC 4180 and the BOM requirement.
- XML doc on `TamperReportExporter.WritePdf` describing the A4 layout choice and the hash-wrap behaviour.
- Inline comment in `AccountingServiceRegistration` explaining why the license declaration is duplicated with `PosServiceRegistration` and noting that it is intentional.

## Out of Scope

- **Excel-native (.xlsx) export.** CSV with UTF-8 BOM opens cleanly in Excel and is the BIR-recommended evidence format. `.xlsx` requires a separate library (OpenXmlSDK or ClosedXML) and is unnecessary.
- **Email delivery of exports.** Out of scope; covered by a hypothetical future cross-cutting notification plan.
- **Filtering or re-sorting at export time.** Exports faithfully reflect the loaded incident set, in the order returned by `ITamperAuditQueryService.GetIncidentsAsync`.
- **Digital signature on the PDF.** BIR does not currently require signed PDFs for tamper-evidence submission. If they later do, a follow-up plan can add a signing step using a different library.
- **Re-running the report from inside the export.** The export consumes whatever the user has already loaded; it does not re-query. This keeps the snapshot stable between viewing and exporting.
