---
module: MerchSys.POS
plan-id: POS-19
title: "PDF Receipt Rendering"
depends-on: [POS-06, POS-18]
estimated-files: 6
---

# PDF Receipt Rendering

## Context

POS-18 introduced `IReceiptBodyComposer` / `BirCompliantReceiptBodyComposer`, which produces a structured `ReceiptBody` (five named blocks: Header, Items, Totals, VatDisclosure, Footer) carrying BIR-compliant Official Receipt text using the Unicode "₱" glyph (U+20B1).

Today, `ReceiptService.PrintReceiptAsync` only invokes the composer and writes the joined text to `System.Console.WriteLine`. There is no PDF emitter, and the receipt cannot be persisted to disk for reprint, archival, or electronic delivery.

This plan adds the **rendering layer** that consumes a `ReceiptBody` and produces a real output. Two renderers are introduced:

1. **Console** — existing behaviour, extracted into a real renderer class and kept as the development/diagnostic default.
2. **PDF** — monospace single-page PDF file for digital receipts, reprints, and archival.

ESC/POS thermal printer output is **explicitly deferred** to a future plan. Villon Farm Supply does not currently have a thermal printer available for verification, and shipping thermal code that cannot be exercised against hardware would mean carrying unverified bytes to deployment. The `IReceiptRenderer` abstraction introduced here is the seam where a future `EscPosThermalRenderer` will plug in without touching `ReceiptService` or the composer.

The composer is **not modified**. Per the POS-18 boundary, any output-side transforms (currency substitution, width wrapping) are the renderer's responsibility, not the composer's. PDF natively supports Unicode "₱", so no transform is needed for this plan.

## Prerequisites

- **POS-06** (Receipt Generation) — `ReceiptService.PrintReceiptAsync` is the integration point.
- **POS-18** (Receipt Body VAT Bucket Printing) — `IReceiptBodyComposer` + `ReceiptBody.AllLines` define the input contract.

## Wiki References

- `concepts/bir-compliance.md` — Official Receipt rules. BIR allows electronic OR copies provided the same disclosure block is present; a PDF rendering of `ReceiptBody.AllLines` satisfies this.
- `analysis/tech-stack-reference.md` — Pinned package versions; consult before adding the PDF NuGet.

## Deliverables

```
MerchSys.POS/Services/ReceiptRendering/
├── IReceiptRenderer.vb                              ' New abstraction
├── ReceiptRenderTarget.vb                           ' Enum: Console, Pdf
├── ConsoleReceiptRenderer.vb                        ' New — existing behaviour, moved out of ReceiptService
├── PdfReceiptRenderer.vb                            ' New — monospace PDF via PdfSharpCore
└── ReceiptPdfOptions.vb                             ' New — strongly-typed config for the PDF renderer

MerchSys.POS/Services/ReceiptService.vb              ' Modified — delegate output to IReceiptRenderer
MerchSys.App/Startup/PosServiceRegistration.vb       ' Modified — register chosen renderer based on config
```

A NuGet addition (QuestPDF) is expected. The exact resolved version is to be selected during implementation; pin it in `MerchSys.POS.vbproj` (no floating ranges) and record it in the summary.

> **Library choice — note:** an earlier revision of this plan named `PdfSharpCore` as the PDF library. That choice was reversed during implementation because `PdfSharpCore` pulls `SixLabors.ImageSharp 1.0.4` as a transitive dependency, which carries several high-severity NuGet security advisories (NU1903 — GHSA-65x7-c272-7g7r, GHSA-2cmq-823j-5qj8, GHSA-63p8-c4ww-9cg7). VISTA's build standard is **0 errors, 0 warnings**, so a library with a known-vulnerable transitive cannot ship. `QuestPDF` uses `SkiaSharp` internally and pulls no vulnerable transitives. It carries a Community License that is free for organisations under USD 1M annual revenue, which Villon Farm Supply satisfies. The license must be declared once at startup via `QuestPDF.Settings.License = LicenseType.Community`.

## Specification

### IReceiptRenderer

```vb
Public Interface IReceiptRenderer

    ''' <summary>
    ''' Emits the receipt to the renderer's configured output channel
    ''' (console stream or PDF file). A future thermal-printer renderer
    ''' will plug into this same interface.
    ''' </summary>
    Function RenderAsync(
        receipt As OfficialReceipt,
        body As ReceiptBody,
        cancellationToken As CancellationToken
    ) As Task

End Interface
```

One renderer is active at a time, chosen at composition root from configuration. Renderers are stateless and are registered as `Scoped` to match `ReceiptService`.

### ReceiptRenderTarget enum

```vb
Public Enum ReceiptRenderTarget
    Console = 0    ' Dev/diagnostic fallback. Writes to System.Console.
    Pdf = 1        ' Single-page monospace PDF file written to disk.
End Enum
```

Note: `Thermal` is intentionally absent from this enum. It will be added when the corresponding renderer is implemented.

### Configuration model

Read from `appsettings.json` under `POS:Receipt`:

```json
"POS": {
  "Receipt": {
    "Renderer": "Console",            // Console | Pdf
    "Pdf": {
      "OutputDirectory": "%LOCALAPPDATA%\\MerchSys\\Receipts",
      "FileNamePattern": "OR-{ReceiptNumber}-{YYYYMMDD}.pdf",
      "FontFamily": "Consolas",
      "FontSizePt": 9
    }
  }
}
```

- `Renderer` is case-insensitive and binds to `ReceiptRenderTarget`. Missing or unknown values fall back to `Console` with a single startup log warning.
- `Pdf:OutputDirectory` may contain environment variables; expanded via `Environment.ExpandEnvironmentVariables`.
- Bind into a `ReceiptPdfOptions` POCO via `IOptions(Of ReceiptPdfOptions)` so the renderer does not read `IConfiguration` directly.

### ConsoleReceiptRenderer

- Joins `body.AllLines` with `Environment.NewLine`.
- Writes via `System.Console.WriteLine` (per CLAUDE.md trap: always `System.Console.WriteLine` to avoid `Microsoft.Extensions.Logging.Console` shadow).
- No transforms. The "₱" glyph is left intact.

This class simply lifts the existing `Console.WriteLine` call out of `ReceiptService` so it lives behind the same interface as future renderers — no behavioural change.

### PdfReceiptRenderer

- Uses `QuestPDF` (Community License; SkiaSharp-backed; no `SixLabors.ImageSharp` transitive). Add the NuGet to `MerchSys.POS.vbproj`.
- Layout:
  - Single page, page size = `PageSizes.A5` portrait.
  - Monospace font from options (`FontFamily`, `FontSizePt`) applied via `page.DefaultTextStyle`.
  - Lines emitted via a `Column` of `Item().Text(line)` calls, one row per `body.AllLines` entry.
  - No currency substitution — PDF supports Unicode. The composer's stored text is rendered as-is.
  - QuestPDF performs internal font substitution if the configured family is not installed (typically falls back to DejaVu Sans). Detection of substitution is not exposed by the library, so the renderer logs an advisory whenever the configured family is not a known-system default; manual font validation belongs to the operator.
- Output path: `{OutputDirectory}/{FileNamePattern}` after env-var expansion. Token substitution:
  - `{ReceiptNumber}` → `receipt.ReceiptNumber`
  - `{YYYYMMDD}` → `receipt.IssueDate.ToLocalTime().ToString("yyyyMMdd")`
- If the output directory does not exist, `Directory.CreateDirectory` is called once.
- File collisions are not expected (receipt numbers are unique) but if the file exists, **throw** — never overwrite a stored PDF receipt. This is a defensive guard, not a fallback.
- QuestPDF generation runs inside a `Try/Catch` so any internal failure is logged with the receipt number and rethrown — the caller decides retry policy.

### ReceiptPdfOptions

```vb
Public Class ReceiptPdfOptions
    Public Property OutputDirectory As String = "%LOCALAPPDATA%\MerchSys\Receipts"
    Public Property FileNamePattern As String = "OR-{ReceiptNumber}-{YYYYMMDD}.pdf"
    Public Property FontFamily As String = "Consolas"
    Public Property FontSizePt As Double = 9.0
End Class
```

Bound at startup via `services.Configure(Of ReceiptPdfOptions)(configuration.GetSection("POS:Receipt:Pdf"))`.

### ReceiptService.PrintReceiptAsync changes

```vb
' Before:
Dim rendered = String.Join(Environment.NewLine, body.AllLines)
Console.WriteLine(rendered)

' After:
Await _renderer.RenderAsync(receipt, body, CancellationToken.None)
```

`ReceiptService` gains a constructor parameter `renderer As IReceiptRenderer`. The composer call is unchanged.

### DI registration

In `PosServiceRegistration.vb`:

```vb
' QuestPDF license must be declared before any Document.Create call.
QuestPDF.Settings.License = LicenseType.Community

services.Configure(Of ReceiptPdfOptions)(configuration.GetSection("POS:Receipt:Pdf"))

Dim target = configuration.GetValue(Of String)("POS:Receipt:Renderer", "Console")
Dim parsed As ReceiptRenderTarget
If Not [Enum].TryParse(target, ignoreCase:=True, parsed) Then
    parsed = ReceiptRenderTarget.Console
    logger.LogWarning("Unknown POS:Receipt:Renderer '{0}' — falling back to Console.", target)
End If

Select Case parsed
    Case ReceiptRenderTarget.Pdf
        services.AddScoped(Of IReceiptRenderer, PdfReceiptRenderer)
    Case Else
        services.AddScoped(Of IReceiptRenderer, ConsoleReceiptRenderer)
End Select
```

## Implementation Notes

- **No changes to `BirCompliantReceiptBodyComposer`** or any POS-18 file. The composer continues to emit "₱". The PDF renderer preserves it; a future thermal renderer will substitute it at its own boundary.
- **VB.NET traps to watch for:**
  - `Console` namespace shadow: use `System.Console.WriteLine` in `ConsoleReceiptRenderer`, not bare `Console.WriteLine`, in case the file imports `Microsoft.Extensions.Logging`.
  - `Await in Catch/Finally`: capture exception state before awaiting if the PDF writer ever needs to clean up; QuestPDF's `GeneratePdf` is synchronous so this likely never applies.
  - Lambda param shadowing: when iterating `body.AllLines.Select(Function(l) ...)`, do not name the parameter `line` if any local with that name exists. Use `ln`.
  - `List.Count(predicate)` trap: don't call `.Count(Function(...) ...)` on `IReadOnlyList(Of String)`; use `Enumerable.Count`.
  - **Local-variable name collision with imported types (BC30980).** Do not name a local `Document`, `Page`, `Column`, or any other identifier that matches a QuestPDF type brought in by `Imports QuestPDF.Fluent`. VB.NET is case-insensitive, so `Dim document = Document.Create(...)` makes `document` refer to itself in the expression. Use `doc`, `pg`, etc.
  - **`System.Drawing.Text.InstalledFontCollection` is not in the class-library surface.** A `net10.0` class library does not reference `System.Drawing.Common`; do not attempt to enumerate installed fonts from inside `MerchSys.POS`. Let QuestPDF handle font fallback internally, or perform the check in `MerchSys.App` if it ever becomes necessary.
- **License posture:** QuestPDF is Community-licensed (free for organisations under USD 1M annual revenue) and pulls SkiaSharp transitively. Confirm during implementation that no transitive dependency carries a NuGet security advisory (NU1902/NU1903). Pin the version in `MerchSys.POS.vbproj`.
- **Default config:** ship `appsettings.json` with `"Renderer": "Console"` so a fresh checkout still runs without any output directory configured.
- **Cancellation:** `CancellationToken` is passed through but PdfSharpCore is synchronous internally — cancellation is best-effort.
- **No file I/O in the composer.** PDF writes happen inside the PDF renderer only.
- **Owner role and renderers:** printing/saving a receipt is a read-side operation; `PrintReceiptAsync` does not write to any DbContext, so no `IWriteContextScope` interaction is required.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. `IReceiptRenderer` is defined with the single `RenderAsync` method and is implemented by `ConsoleReceiptRenderer` and `PdfReceiptRenderer`.
3. `ReceiptService.PrintReceiptAsync` no longer references `System.Console` directly — it invokes the injected `IReceiptRenderer`.
4. Configuration-driven renderer selection works: setting `POS:Receipt:Renderer` to `"Pdf"` or `"Console"` registers the correct implementation. Unknown values fall back to `Console` with one warning log entry.
5. `PdfReceiptRenderer` writes a non-empty PDF file at the expanded output path; the file contains every line from `body.AllLines` in document order.
6. `PdfReceiptRenderer` throws `InvalidOperationException` (and does not overwrite) when a file already exists at the target path.
7. `ReceiptRenderTarget` enum contains only `Console` and `Pdf` — `Thermal` is not yet defined.
8. QuestPDF is the only new NuGet addition; the resolved version is pinned in `MerchSys.POS.vbproj` and no transitive dependency triggers an NU1902/NU1903 advisory.
9. No edits to `IReceiptBodyComposer`, `BirCompliantReceiptBodyComposer`, or any other POS-18 file.

## Output Requirements

### Implementation Summary
Create at `Progress/VISTA_Modules/POS/POS-19-summary.md` using `Progress/_template.md`. Include:

- Pinned QuestPDF version and a one-line note confirming both the transitive license review **and** that the resolved transitive tree triggers no NuGet security advisories.
- A screenshot or text dump of a generated PDF receipt for a VAT-registered taxpayer (open the file, inspect the rendered text, and quote it back in the summary).
- A note on which page size was chosen and why (A5 portrait is the default per this plan).
- Whether the configured font (`Consolas` default) was actually rendered, or QuestPDF substituted its built-in default.
- A `git diff` excerpt showing `ReceiptService.PrintReceiptAsync` swapping `Console.WriteLine` for `_renderer.RenderAsync`.
- A `## Deferred — Thermal Renderer` section confirming the `IReceiptRenderer` abstraction leaves room for a future `EscPosThermalRenderer` without touching `ReceiptService` or the composer.

### Documentation
- XML doc on `IReceiptRenderer` describing the abstraction and the composer/renderer boundary, and noting that a thermal implementation is deferred.
- XML doc on `PdfReceiptRenderer` describing the page layout choice and the no-overwrite guard.
- A `## Renderer Selection` section in the summary describing how operators switch from Console (default) to Pdf via `appsettings.json`.

## Out of Scope

- **ESC/POS thermal printer output.** Deferred to a future plan (see Deferred Features Backlog). The `IReceiptRenderer` seam is built here; the thermal implementation lands later, against real hardware.
- Graphical receipt templates (logos, barcodes, QR codes). The renderer surface accepts plain text only.
- Receipt reprint UI. PDF files are written to disk; a separate plan may add an in-app viewer.
- Receipt email/delivery channels. Out of scope; covered by a hypothetical future POS plan.
