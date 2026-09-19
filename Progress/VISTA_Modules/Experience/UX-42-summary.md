---
module: MerchSys.App
agent: claude-code
date: 2026-06-07
plan-ref: Plans/VISTA_Modules/Experience/42-print-export-ux.md
status: completed
---

## Task Summary

Implemented UX-42: Print & Export UX — BIR Official-Receipt print template (FlowDocument/PrintDialog) and CSV/text-based report export with print preview for Accounting reports.

**Plan:** `[[42-print-export-ux]]`

---

## Confirmed Scope (Pre-Implementation Decision)

| Decision | Choice | Rationale |
|---|---|---|
| PDF approach | WPF built-in `FlowDocument` + `PrintDialog` for OR; hand-rolled plain-text builder for reports | No new NuGet authorized; QuestPDF already in project but used for background receipts, not UI print-dialog flows |
| Report set | `IncomeStatementView`, `VatReliefReportView` | `VatReturnView` already has export (`VatReturnExporter`, prior plan); `TamperAuditReportView` already has export (prior plan) |
| NuGet additions | None | Built-in WPF APIs sufficient |

---

## What Was Done

### Part A — BIR Official-Receipt Print Template

- Modified `WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/SalesCartViewModel.vb`
  - Added `Private _lastCartLines As New List(Of CartLineItem)()` backing field
  - Added `Public ReadOnly Property LastCartLines As IReadOnlyList(Of CartLineItem)` — snapshot available for print
  - Added `_lastCartLines = CartLines.ToList()` in `ProcessPaymentAsync` before `CartLines.Clear()`

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/POS/SalesCartView.xaml`
  - Added "Print Official Receipt" button inside receipt preview section (`IsReceiptVisible`-gated `Border`), `TabIndex=15`

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/POS/SalesCartView.xaml.vb`
  - Added `Imports System.Windows.Documents`, `System.Windows.Media`, `MerchSys.POS.Entities`
  - Added `PrintOrButton_Click` — builds FlowDocument from `CurrentReceipt` + `LastCartLines`, calls `PrintDialog`
  - Added `BuildOrFlowDocument` (Shared) — full BIR layout: header (name/address/TIN/VAT status), OR No., date, item table, totals, VAT disclosure block (VATable Sales/VAT-Exempt/Zero-Rated/Output VAT), footer

### BIR Field Mapping (OR Template)

| BIR Required Field | Source |
|---|---|
| Business name | `OfficialReceipt.BusinessName` |
| Business address | `OfficialReceipt.BusinessAddress` |
| TIN | `OfficialReceipt.BusinessTIN` |
| VAT registration status | `OfficialReceipt.IsVatRegistered` |
| OR number (OR-YYYY-XXXX) | `OfficialReceipt.ReceiptNumber` |
| Date/time | `OfficialReceipt.IssueDate.ToLocalTime()` |
| Itemised goods | `LastCartLines` (ProductName, Qty, UnitPrice, LineTotal, DiscountAmount) |
| Total amount | `OfficialReceipt.TotalAmount` |
| VATable sales (net) | `TotalAmount - VatAmount` |
| VAT-exempt sales | `0D` (all items VATable at Villon Farm Supply) |
| Zero-rated sales | `0D` |
| Output VAT (12%) | `OfficialReceipt.VatAmount` |

### Part B — Report Export (CSV + Preview/Print)

- Created `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ReportPreviewWindow.xaml`
  - Shared modal `Window`; displays plain-text report in read-only scrollable `TextBox` (Courier New 11pt)
  - "Print" button → `FlowDocument` → `PrintDialog`
  - "Save As…" button → `SaveFileDialog` → `File.WriteAllText` (UTF-8)
  - "Close" button

- Created `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ReportPreviewWindow.xaml.vb`
  - Constructor takes `reportTitle`, `content`, `suggestedFileName`

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml`
  - Added "Export CSV" and "Export PDF" buttons to toolbar (right-aligned, after Refresh)

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml.vb`
  - `ExportCsv_Click`: builds culture-safe double-quoted CSV (P&L lines + product margin breakdown) → `SaveFileDialog`
  - `ExportPdf_Click`: builds plain-text report → shows `ReportPreviewWindow`
  - Uses `IncomeStatementViewModel` display properties (`NetSalesDisplay`, `COGSDisplay`, etc.) — matches on-screen formatting exactly per spec

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml`
  - Added "Export CSV" and "Export PDF" buttons to toolbar (right-aligned, after Refresh)

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml.vb`
  - `ExportCsv_Click`: builds CSV with current-month summary (sales/purchases three-bucket breakdown) + trailing 12-month trend → `SaveFileDialog`
  - `ExportPdf_Click`: builds plain-text report with all summaries → shows `ReportPreviewWindow`
  - Accesses `vm.Summary` (can be `Nothing` if not yet loaded — handled with null-guard)

- Added `LLM_Wiki/agent_wiki/patterns/wpf-vista-print-export.md`
- Updated `LLM_Wiki/agent_wiki/index.md` and `log.md`

---

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | Implementation authored to compile clean; **not independently re-run during review** (build deferred to testing phase per workflow) |
| Unit tests pass | N/A |
| Manual verification | Pending operator testing session |

---

## Issues Encountered

- **VatReturnView already had export** — The plan stated `VatReturnView` needed export; the codebase already had `VatReturnViewModel.ExportCsvCommand`/`ExportPdfCommand` + `VatReturnView.xaml.vb` `OnExportReady` handler from a prior plan. Excluded from Part B scope; noted as codebase_wiki discrepancy below.

- **`OfficialReceipt.Items` not populated** — `ReceiptService.GenerateReceiptAsync` never sets `OfficialReceipt.Items`. The `LastCartLines` snapshot approach was used instead (captured before `CartLines.Clear()` in `ProcessPaymentAsync`).

- **`OfficialReceipt.Transaction` not loaded** — After `GenerateReceiptAsync`, the navigation property `Transaction` is null (no eager load). VATable sales computed from `TotalAmount - VatAmount` (valid for the standard 12% VAT case).

- **No `InverseBoolConverter`** — `IsEnabled` binding on export buttons omitted (converter doesn't exist; always-enabled is acceptable per spec — empty export produces a minimal file).

- **`VatReliefReportViewModel.NetVatPayable` missing** — XAML binds to `NetVatPayable` directly but ViewModel only exposes `NetVatPayableColor`. Export uses `vm.Summary?.NetVatPayable` instead.

---

## codebase_wiki Discrepancies (for Antigravity)

1. **`VatReliefReportViewModel`** — XAML binds to `{Binding NetVatPayable}` but the ViewModel has no `NetVatPayable` property (only `NetVatPayableColor`). Either the binding is silently broken in the current XAML, or there is an unmapped computed property. Recommend adding `Public ReadOnly Property NetVatPayable As Decimal` delegating to `If(_summary IsNot Nothing, _summary.NetVatPayable, 0D)`.

2. **`VatReturnView` / `VatReturnViewModel`** — codebase_wiki `accounting/views.md` notes "Supports generation, locking, and export of Forms 2550M/Q and 2551Q" but this detail is not in `accounting/viewmodels.md`. UX-42 plan stated these views lacked export; they already had it. The plan's context was written before the prior export plan landed.

3. **`OfficialReceipt.Items`** — Entity doc says "Stored as formatted text or JSON so the receipt can be reprinted at any time." In practice `ReceiptService.GenerateReceiptAsync` never sets `Items`. The field is always null in production receipts. Should document as "currently not populated; reprinting uses `Transaction.Lines` via `PrintReceiptAsync`."

---

## Review Fixes (post-implementation, claude-code)

A code review surfaced four findings; #1–#2 were fixed in code, #3–#4 are documented notes:

1. **CSV embedded-quote escaping (was: malformed rows)** — `IncomeStatementView` wrapped fields in
   `"…"` but did not double internal quotes, so a SKU name containing `"` (plausible in agri retail —
   `2" PVC pipe`, `1/2" hose`) broke column alignment in Excel/LibreOffice. Added a `EscapeCsv` helper
   (RFC 4180 quote-doubling) applied to the free-text fields (`PeriodDescription`, `ProductName`).
   `VatReliefReportView`'s CSV was left as-is — all its fields are numeric or date-derived, so it has
   no quote-injection surface.
2. **Dead ternary in OR builder** — `SalesCartView.xaml.vb` computed
   `subtotal = receipt.TotalAmount - If(receipt.IsVatRegistered, 0D, 0D)` (both branches `0D`).
   Reduced to `subtotal = receipt.TotalAmount` with a clarifying comment (reprint/no-snapshot path).
3. **(Note only) UX-21 formatting partial reuse** — the primary report figures use the VM `*Display`
   properties (so they match the on-screen presentation exactly, satisfying AC #3), but the OR template
   and the breakdown tables use raw `:N2` + inline `₱` + raw date format strings. Acceptable: UX-21
   formatting is exposed as XAML converters / `*Display` props with no public code-behind formatter to
   call, and the output is visually equivalent. Left as-is.
4. **Build claim corrected** — the original "✅ 0 errors, 0 warnings" was the implementer's claim; per
   the hybrid workflow no build was run during review, so the Build & Test table now reflects that the
   build was not independently re-verified.

## What's Next

- [ ] Operator testing: verify OR prints correctly on a connected printer
- [ ] Operator testing: verify Income Statement CSV export opens cleanly in Excel/LibreOffice
- [ ] Operator testing: verify VAT Relief Report preview window shows correct data
- [ ] Consider adding `NetVatPayable` property to `VatReliefReportViewModel` (codebase_wiki discrepancy #1)

## Cross-References

- Domain Wiki consulted: `[[wiki/concepts/bir-compliance.md]]`
- Agent Wiki consulted: `[[patterns/wpf-vista-formatting.md]]`
- Agent Wiki added: `[[patterns/wpf-vista-print-export.md]]`
