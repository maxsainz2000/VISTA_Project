---
name: wpf-vista-print-export
description: OR FlowDocument/PrintDialog template + BIR field map, preview→CSV/text export recipe, UX-21 formatting reuse, no-NuGet approach
metadata:
  type: pattern
---

# WPF VISTA — Print & Export Pattern

**type:** pattern  
**module:** MerchSys.App  
**agent:** claude-code  
**date:** 2026-06-07  
**tags:** [wpf, xaml, vb-net, print, export, flowdocument, csv, bir, official-receipt, accounting]

---

## Context

UX-42 adds BIR Official-Receipt printing (Part A) and CSV/PDF-text export for Accounting reports (Part B). No new NuGet was added — the solution uses WPF built-in `FlowDocument`/`PrintDialog` and a hand-rolled CSV/text builder, consistent with the existing `VatReturnExporter` approach.

---

## Part A — BIR Official-Receipt FlowDocument Print Template

### Data sources (from `CurrentReceipt.*`)

| Field | Source | BIR Required? |
|---|---|---|
| Business name | `OfficialReceipt.BusinessName` | Yes |
| Business address | `OfficialReceipt.BusinessAddress` | Yes |
| VAT status | `OfficialReceipt.IsVatRegistered` | Yes |
| TIN | `OfficialReceipt.BusinessTIN` | Yes |
| OR number | `OfficialReceipt.ReceiptNumber` (OR-YYYY-XXXX) | Yes |
| Issue date | `OfficialReceipt.IssueDate.ToLocalTime()` | Yes |
| Line items | `SalesCartViewModel.LastCartLines` (snapshot) | Yes |
| Total | `OfficialReceipt.TotalAmount` | Yes |
| VATable sales (net) | `TotalAmount - VatAmount` | Yes |
| VAT-exempt / zero-rated | `0D` (Villon Farm Supply — all items VATable) | Yes |
| Output VAT | `OfficialReceipt.VatAmount` | Yes |

### FlowDocument recipe

```vb
' In SalesCartView.xaml.vb code-behind:
Dim flowDoc As New FlowDocument()
flowDoc.PagePadding = New Thickness(60, 40, 60, 40)
flowDoc.FontFamily = New FontFamily("Courier New")
flowDoc.FontSize = 11
flowDoc.PageWidth = 480  ' ~A5 portrait

' Populate via BuildOrFlowDocument(flowDoc, receipt, lastCartLines)

Dim dlg As New PrintDialog()
If dlg.ShowDialog() = True Then
    dlg.PrintDocument(
        CType(flowDoc, IDocumentPaginatorSource).DocumentPaginator,
        $"BIR Official Receipt {receipt.ReceiptNumber}")
End If
```

### Cart-lines snapshot requirement

`ReceiptService.GenerateReceiptAsync` does not load `Transaction.Lines` back onto the returned `OfficialReceipt`. The `SalesCartViewModel.LastCartLines` property captures a `CartLines.ToList()` snapshot **before** `CartLines.Clear()` in `ProcessPaymentAsync`, so line items are available at print time.

### Layout sections (in order)

1. Business header (name, address, VAT status, TIN) — centred, Courier New 10–11pt
2. Rule separator (`────────────────────────────────────────────────`)
3. OR No. + issue date
4. Rule separator
5. Item header row + items (20-char name, qty, unit price, line total; discount line if >0)
6. Rule separator
7. Subtotal + **TOTAL** (bold)
8. Rule separator
9. VAT disclosure block (VATable Sales, VAT-Exempt Sales, Zero-Rated Sales, Output VAT)
10. Rule separator + footer italics

---

## Part B — Accounting Report Export (CSV + Text Preview)

### Pattern: code-behind Click handlers + `ReportPreviewWindow`

Mirrors `TamperAuditReportView` — export logic lives in the view code-behind, not the ViewModel. The `ReportPreviewWindow` (`Views/Shell/ReportPreviewWindow.xaml`) is a shared modal window:

```vb
' For PDF/print — show preview window, user can Print or Save As
Dim preview As New ReportPreviewWindow(title, textContent, suggestedFileName)
preview.Owner = Window.GetWindow(Me)
preview.ShowDialog()

' For CSV — go straight to SaveFileDialog
Dim dlg As New SaveFileDialog() With {.Filter = "CSV files (*.csv)|*.csv", ...}
If dlg.ShowDialog() = True Then
    File.WriteAllText(dlg.FileName, csv, Encoding.UTF8)
End If
```

### ReportPreviewWindow capabilities

- Displays plain-text report in a scrollable `TextBox` (Courier New 11pt, read-only)
- **Print** button: wraps text in `FlowDocument`, opens `PrintDialog`
- **Save As…** button: `SaveFileDialog` → `File.WriteAllText` UTF-8
- Modal, `Owner = Window.GetWindow(Me)`, `WindowStartupLocation.CenterOwner`

### CSV culture-safety rules

- All fields are double-quoted to handle peso symbol `₱` and commas in amounts
- Use display strings from the ViewModel for amounts (matches "same formatting as on screen")
- Header row first, then data rows; blank line separating sections
- `Encoding.UTF8` always

### UX-21 formatting reuse

- Amounts in CSV/text use `{value:N2}` with `₱` prefix inline — consistent with `FormatCurrency` resource (`₱{0:N2}`)
- Dates use `{date:MM/dd/yyyy HH:mm}` — consistent with `FormatDateTime` resource
- No hardcoded format literals in XAML; existing display strings from VMs are passed through unchanged

---

## Targeted reports

| View | CSV | Print/Preview |
|---|---|---|
| `IncomeStatementView` | ✅ | ✅ (`ReportPreviewWindow`) |
| `VatReliefReportView` | ✅ | ✅ (`ReportPreviewWindow`) |
| `VatReturnView` | Already implemented (VatReturnExporter, prior plan) | Already implemented |
| `TamperAuditReportView` | Already implemented (prior plan) | Already implemented |

---

## VB traps in this area

- `Await` not allowed in `Catch`/`Finally` (BC36943): `SaveFileDialog` and `File.WriteAllText` are sync — no async needed, no trap triggered.
- `Imports System.Windows.Documents` not required in code-behind — it is a global project-level `<Import>` in `MerchSys.App.vbproj`.
- `System.Console` shadow: none — no `Microsoft.Extensions.Logging` import in these files.
- `clr-namespace` root prefix: `ReportPreviewWindow` uses `x:Class="Views.Shell.ReportPreviewWindow"` (relative), consistent with all other Shell windows.

## Related

- [[wpf-vista-formatting]] — UX-21 format tokens reused in export
- [[wpf-vista-print-export]] — this file
