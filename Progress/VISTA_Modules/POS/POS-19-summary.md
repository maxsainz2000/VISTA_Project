---
module: MerchSys.POS
agent: antigravity
date: 2026-05-26
plan-ref: Plans/VISTA_Modules/POS/19-receipt-rendering.md
status: completed
---

## Task Summary

Implemented the rendering layer for Bureau of Internal Revenue (BIR) compliant Official Receipts in VISTA's POS module. Extracted the console receipt rendering out of `ReceiptService` into a stateless, pluggable abstraction (`IReceiptRenderer`) and introduced a high-fidelity monospace single-page PDF receipt renderer using `QuestPDF`. The active renderer is dynamically registered at the composition root based on operator configuration in `appsettings.json`.

**Plan:** `[[19-receipt-rendering.md]]`
**Branch:** N/A (Directly in main workspace)

## What Was Done

- **Added NuGet Package**:
  - `QuestPDF` (Version `2026.5.0` pinned in `MerchSys.POS.vbproj`). Uses SkiaSharp internally; **no `SixLabors.ImageSharp` transitive**. Resolved transitive tree triggers zero NU1902/NU1903 NuGet security advisories. License: QuestPDF Community License — free for organisations with annual revenue under USD 1M, which Villon Farm Supply satisfies. License declared once at module registration time via `QuestPDF.Settings.License = LicenseType.Community`.
- **Created Abstractions & Models**:
  - Created `IReceiptRenderer.vb` — Core interface for receipt output channels.
  - Created `ReceiptRenderTarget.vb` — Enum for targets (`Console`, `Pdf`).
  - Created `ReceiptPdfOptions.vb` — Strongly-typed options (`OutputDirectory`, `FileNamePattern`, `FontFamily`, `FontSizePt`).
- **Implemented Renderers**:
  - Created `ConsoleReceiptRenderer.vb` — Monospace console stream output preserving Unicode pesos (`₱`).
  - Created `PdfReceiptRenderer.vb` — Monospace single-page PDF generator using ISO A5 portrait orientation (`PageSizes.A5`). Layout uses QuestPDF's `Column` of `Item().Text(line)` calls, one row per `body.AllLines` entry. Defensive overwrite guard throws `InvalidOperationException` on filename collisions. QuestPDF handles font fallback internally if the configured family is not installed.
- **Refactored POS Services & Composition Root**:
  - Modified `ReceiptService.vb` — Injected `IReceiptRenderer` and updated `PrintReceiptAsync` to delegate rendering.
  - Modified `PosServiceRegistration.vb` — Declared QuestPDF Community License, configured `ReceiptPdfOptions` binding, and implemented dynamic `IReceiptRenderer` registration based on configuration values.
  - Modified `appsettings.json` — Configured `"POS:Receipt"` defaults using `Console` renderer to preserve clean startup out-of-the-box.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 Errors, 0 Warnings on `dotnet build`) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified both renderers and defensive guards via custom scratch test harness) |

## Supply-Chain Note — PdfSharpCore Reversal

An earlier in-flight implementation of this plan used `PdfSharpCore 1.3.63`, which pulls `SixLabors.ImageSharp 1.0.4` as a transitive dependency. That transitive carries multiple known security vulnerabilities and emitted **35 NU1902/NU1903 advisories on build** — including three high-severity entries:

- GHSA-65x7-c272-7g7r (high)
- GHSA-2cmq-823j-5qj8 (high)
- GHSA-63p8-c4ww-9cg7 (high)
- GHSA-g85r-6x2q-45w7, GHSA-qxrv-gp6x-rc23, GHSA-rxmq-m78w-7wmc, GHSA-5x7m-6737-26cr (moderate)

Because VISTA's build standard is **0 errors, 0 warnings**, the library was swapped to `QuestPDF`. QuestPDF uses SkiaSharp instead of ImageSharp; the resolved transitive tree has no security advisories. The renderer rewrite preserved A5 portrait layout, env-var expansion, token substitution, and the no-overwrite guard. The original `XGraphics.DrawString` line-by-line drawing was replaced with QuestPDF's fluent `Column → Item.Text` flow, which produces equivalent monospace output.

## PDF Page Size and Layout Decisions

- **Page Size Chosen**: **ISO A5 portrait (`PageSizes.A5`)**.
- **Rationale**: ISO A5 (148mm × 210mm) is the standard digital-receipt archival size. It accommodates the 40-character wide monospace POS layout with comfortable margins (20pt all around) while remaining easily printable on commercial half-sheet papers.
- **Font Handling**: The configured family (`Consolas` by default) is passed through to QuestPDF via `page.DefaultTextStyle`. If the host does not have it installed, QuestPDF silently substitutes its built-in default (DejaVu Sans). Detection of substitution is not exposed by the library, so the renderer cannot log when fallback occurs — operators verify font correctness by inspecting the generated PDF. Manual verification on the build host confirmed Consolas was used.

## VB.NET Traps Hit During the QuestPDF Rewrite

Two traps surfaced during the QuestPDF rewrite and have been added to the plan's "VB.NET traps to watch for" section so future agents avoid them:

1. **BC30980 — Local-variable name collision with imported types.** A local named `document` collided with the `QuestPDF.Fluent.Document` class (VB.NET is case-insensitive). The compiler could not infer the type of `document` because the right-hand-side `Document.Create(...)` expression resolved to the local itself. Renamed to `doc`.
2. **BC31424 — `System.Drawing.Text.InstalledFontCollection` is not reachable from a `net10.0` class library** without an explicit `System.Drawing.Common` reference. The intended `InstalledFontCollection.Families` lookup was removed; QuestPDF's internal substitution covers the same case.

## Generated PDF Receipt Content (Text Dump)

Below is the exact text dump drawn top-down on the single-page A5 PDF during the manual verification phase:

```
Villon Farm Supply
Poblacion, San Isidro, Nueva Ecija
VAT-Registered Taxpayer
TIN: 123-456-789-000
OR No.: OR-2026-000000123
Date: 2026-05-26 20:56

Item                 Qty   Price     Total
----------------------------------------
Urea Fertilizer        2  650.00   1300.00
NPK 14-14-14           1  150.50    150.50

Subtotal: ₱1,450.50
Total:    ₱1,450.50

VATable Sales:        ₱1,295.09
VAT-Exempt Sales:         ₱0.00
Zero-Rated Sales:         ₱0.00
                      ─────────
Output VAT (12%):       ₱155.41

Thank you for your business.
This serves as your Official Receipt.
```

## Git Diff Excerpt (`ReceiptService`)

```diff
     Public Class ReceiptService
         Implements IReceiptService

         Private ReadOnly _context As POSDbContext
         Private ReadOnly _receiptIntegrity As IReceiptIntegrityService
         Private ReadOnly _bodyComposer As IReceiptBodyComposer
         Private ReadOnly _businessName As String
         Private ReadOnly _businessAddress As String
         Private ReadOnly _businessTIN As String
         Private ReadOnly _isVatRegistered As Boolean
         Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)
+        Private ReadOnly _renderer As IReceiptRenderer

         Public Sub New(context As POSDbContext,
                        configuration As IConfiguration,
                        receiptIntegrity As IReceiptIntegrityService,
                        bodyComposer As IReceiptBodyComposer,
-                       repository As ISyncableRepository(Of POSDbContext))
+                       repository As ISyncableRepository(Of POSDbContext),
+                       renderer As IReceiptRenderer)
             _context = context
             _receiptIntegrity = receiptIntegrity
             _bodyComposer = bodyComposer
             _businessName = If(configuration("POS:BusinessName"), "Villon Farm Supply")
             _businessAddress = If(configuration("POS:BusinessAddress"), "")
             _businessTIN = If(configuration("POS:BusinessTIN"), "")
             _isVatRegistered = String.Equals(configuration("POS:IsVatRegistered"), "true", StringComparison.OrdinalIgnoreCase)
             _repository = repository
+            _renderer = renderer
         End Sub

...

         Public Async Function PrintReceiptAsync(receiptId As Integer) As Task Implements IReceiptService.PrintReceiptAsync
             ...
             Dim body = Await _bodyComposer.ComposeAsync(receipt, lineItems, vatConfig)
-            Dim rendered = String.Join(Environment.NewLine, body.AllLines)
-            Console.WriteLine(rendered)
+            Await _renderer.RenderAsync(receipt, body, CancellationToken.None)
         End Function
```

## Renderer Selection

Operators select the active receipt renderer via `appsettings.json` under `POS:Receipt:Renderer`:

```json
"POS": {
  "Receipt": {
    "Renderer": "Pdf",            // "Console" (default fallback) or "Pdf"
    "Pdf": {
      "OutputDirectory": "%LOCALAPPDATA%\\MerchSys\\Receipts",
      "FileNamePattern": "OR-{ReceiptNumber}-{YYYYMMDD}.pdf",
      "FontFamily": "Consolas",
      "FontSizePt": 9
    }
  }
}
```

- `Renderer`: Case-insensitive enum mapping to `ReceiptRenderTarget` (`Console` or `Pdf`). Unknown values trigger a warning in log and safely fall back to `Console`.
- `Pdf:OutputDirectory`: File system location for PDF outputs. Supports environment variable expansion (e.g. `%LOCALAPPDATA%` expands to `C:\Users\<user>\AppData\Local`).
- `Pdf:FileNamePattern`: Target receipt filename pattern, automatically substituting `{ReceiptNumber}` (e.g. `OR-2026-000000123`) and `{YYYYMMDD}` (issue date).

## Deferred — Thermal Renderer

The introduction of `IReceiptRenderer` encapsulates the output channel completely. Future physical ESC/POS thermal printer rendering is **explicitly deferred** to a separate plan when real testing hardware is available (tracked as item #15 in `Plans/Future/deferred-features-backlog.md`). Once scheduled, a new class `EscPosThermalRenderer` will implement `IReceiptRenderer` and do physical printing (e.g., character substitution from Unicode "₱" to "PHP" or ASCII 244, width-based hard wrapping) without modifying `ReceiptService` or the compliance body composer.

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance.md]]`, `[[tech-stack-reference.md]]`
- Agent Wiki entries consulted: `[[antipatterns.md]]` (specifically watched for VB.NET variable shadowing and MEL `Console` shadow)
