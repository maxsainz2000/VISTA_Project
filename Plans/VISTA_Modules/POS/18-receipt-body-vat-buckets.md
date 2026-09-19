---
module: MerchSys.POS
plan-id: POS-18
title: "Receipt Body VAT Bucket Printing"
depends-on: [POS-06, POS-14, POS-15]
estimated-files: 3
---

# Receipt Body VAT Bucket Printing

## Context

POS-14 introduced the three-bucket BIR VAT calculation (Vatable Sales, VAT Exempt Sales, Zero-Rated Sales, plus Output VAT). The values are persisted on each receipt and available to the Accounting VAT reporting service (ACC-11). The 2026-05-11 POS audit notes that the values are **not yet printed on the physical receipt body** — BIR explicitly requires this disclosure on every Official Receipt for a VAT-registered taxpayer. Without it, the receipt is technically non-compliant and an audit could disallow the entire transaction record.

POS-15 (Receipt Numbering Integration) is a sibling plan that wires `ReceiptService` to `IReceiptIntegrityService` for the receipt **number**. This plan handles the receipt **body content**. The two plans land independently but should both reach `main` before any production deployment.

This plan modifies the existing receipt template/composition path to surface the three buckets, the output VAT, the TIN, and the BIR-required "VAT-Registered Taxpayer" boilerplate, conditioned on `VatConfiguration.IsVatRegistered`.

## Prerequisites

- **POS-06** (Receipt Generation) — `ReceiptService.GenerateReceiptAsync`, receipt formatting path
- **POS-14** (VAT Configuration, Schema Extension & Calculation) — three-bucket persisted values on `Pos_OfficialReceipts`, `VatConfiguration.Tin`, `VatConfiguration.IsVatRegistered`
- **POS-15** (Receipt Numbering Integration) — `ReceiptService` already constructor-injects `IReceiptIntegrityService`; this plan layers on top of that signature

## Wiki References

- `concepts/bir-compliance.md` — Required receipt body fields for VAT-registered taxpayers
- `concepts/utang-credit-system.md` — Credit-sale receipts must include the same disclosure as cash sales

## Deliverables

```
MerchSys.POS/Services/ReceiptFormatting/
├── IReceiptBodyComposer.vb                              ' New abstraction
└── BirCompliantReceiptBodyComposer.vb                   ' New implementation

MerchSys.POS/Services/ReceiptService.vb                  ' Modified — delegate body composition
```

## Specification

### IReceiptBodyComposer

```
Public Interface IReceiptBodyComposer

    Function ComposeAsync(
        receipt As OfficialReceipt,
        lineItems As IReadOnlyList(Of OfficialReceiptLine),
        vatConfig As VatConfiguration
    ) As Task(Of ReceiptBody)

End Interface

Public Class ReceiptBody
    Public Property HeaderLines As IReadOnlyList(Of String)
    Public Property ItemLines As IReadOnlyList(Of String)
    Public Property TotalsBlock As IReadOnlyList(Of String)
    Public Property VatDisclosureBlock As IReadOnlyList(Of String)
    Public Property FooterLines As IReadOnlyList(Of String)
End Class
```

The receipt body is a structured list of plain-text lines, broken into named blocks. The printer/PDF emitter joins them with newlines. This structure keeps the BIR-mandated VAT block clearly separable so a future plan (e.g., a graphical receipt template) can render it visually distinct.

### BirCompliantReceiptBodyComposer behaviour

`ComposeAsync` builds blocks in order:

#### HeaderLines
- `vatConfig.RegisteredBusinessName`
- `vatConfig.RegisteredAddress` (may span multiple lines on width-limited paper; split on newline characters in the stored value)
- One of:
  - `"VAT-Registered Taxpayer"` + `"TIN: " & vatConfig.Tin` if `IsVatRegistered = True`
  - `"Non-VAT Taxpayer"` + `"TIN: " & vatConfig.Tin` if `IsVatRegistered = False`
- `"OR No.: " & receipt.ReceiptNumber.ToString("D9")` (9-digit zero-padded — matches BIR receipt-numbering convention)
- `"Date: " & receipt.IssuedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")`

#### ItemLines
- One line per `OfficialReceiptLine`: SKU, name, qty, unit price, line total. Existing format from POS-06; do not redesign here.

#### TotalsBlock
- `"Subtotal: " & money(subtotal)`
- `"Total: " & money(receipt.TotalAmount)`

#### VatDisclosureBlock — the key deliverable

If `IsVatRegistered = True`:
```
VATable Sales:        ₱<vatable>
VAT-Exempt Sales:     ₱<exempt>
Zero-Rated Sales:     ₱<zeroRated>
                       ─────────
Output VAT (12%):     ₱<outputVat>
```

If `IsVatRegistered = False`:
```
Gross Sales:          ₱<total>
Percentage Tax (3%):  ₱<percentageTax>
```

The values come from the persisted receipt columns introduced by POS-14 (`VatableSales`, `VatExemptSales`, `ZeroRatedSales`, `OutputVat`). If any value is `0`, still print the line — BIR wants the disclosure to be unambiguous, not condensed.

#### FooterLines
- `"Thank you for your business."`
- `"This serves as your Official Receipt."`

### ReceiptService modification

Currently `ReceiptService.GenerateReceiptAsync` builds the body inline (or via a method on the class). Replace the inline body construction with:

```
Dim body = Await _bodyComposer.ComposeAsync(receipt, lineItems, vatConfig)
Dim renderedText = String.Join(Environment.NewLine, body.AllLines)
```

Where `body.AllLines` is a helper concatenating the five blocks in order with appropriate spacing (blank line between blocks).

The constructor gains an `IReceiptBodyComposer` parameter. POS-15 already changed the constructor to accept `IReceiptIntegrityService`; layering `IReceiptBodyComposer` on top keeps both new dependencies in a single coherent commit if POS-15 has not yet landed, **or** is a clean additive change if POS-15 is already on `main`.

`VatAwareReceiptService` (the decorator from POS-14) does not change — body composition happens inside `inner.GenerateReceiptAsync` after the decorator has computed and persisted the buckets.

### DI registration

```
services.AddScoped(Of IReceiptBodyComposer, BirCompliantReceiptBodyComposer)
```

Lives in the POS module DI extension alongside POS-15's `IReceiptIntegrityService` registration.

## Implementation Notes

- The receipt body format is plain text intentionally. A future plan can introduce ESC/POS bytes, PDF rendering, or a graphical template; the structured `ReceiptBody` class is the contract that survives that future work.
- The 9-digit zero-padded OR number convention is per BIR Form 2541 layout. Cite the rule inline.
- The "₱" peso glyph must render correctly on the chosen receipt printer; if a printer is ASCII-only, fall back to "PHP " — but the **stored** receipt text uses "₱". Display-layer transformation is the printer's job, not the composer's. Document this boundary in the implementation summary.
- If `vatConfig.RegisteredBusinessName` or `Tin` is missing, the composer throws `InvalidOperationException` with a clear message — a non-VAT-registered taxpayer with no TIN is a configuration error, not a runtime fallback condition. The validation in POS-17's `VatSettingsView` is the proper place to prevent this state from arising.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally`. The composer is largely synchronous so this is mostly a non-issue here.
- This plan does **not** modify the persisted receipt schema. POS-14's columns suffice.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `BirCompliantReceiptBodyComposer.ComposeAsync` returns a `ReceiptBody` with five populated blocks for a VAT-registered taxpayer.
3. The `VatDisclosureBlock` lines for a VAT-registered receipt include all four mandated labels: VATable Sales, VAT-Exempt Sales, Zero-Rated Sales, Output VAT.
4. For a non-VAT-registered taxpayer, the disclosure block shows Gross Sales and Percentage Tax (3%) instead.
5. Zero values are still printed (not condensed/hidden).
6. OR number is rendered as a 9-digit zero-padded string.
7. `ReceiptService.GenerateReceiptAsync` no longer builds the body inline; the body composer is invoked exactly once per receipt.
8. The composer throws `InvalidOperationException` if `vatConfig.Tin` is null/empty.
9. DI registration of `IReceiptBodyComposer` is in the POS module-local extension, not in the composition root.
10. No edits to POS-14 source files; this plan consumes its outputs.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-18-summary.md` using `Progress/_template.md`. Include:

- A sample rendered receipt body for a VAT-registered taxpayer (full multi-line text).
- A sample for a non-VAT-registered taxpayer.
- The `git diff` of `ReceiptService.GenerateReceiptAsync` showing the swap from inline body building to composer delegation.
- A note documenting the "₱" vs "PHP " printer-layer transformation rule.

### Documentation
- XML doc on `IReceiptBodyComposer` describing the structured block model and the BIR motivation.
- Inline comment on the 9-digit zero-padding citing the BIR Form 2541 layout.
- A short note in the summary listing any width assumptions the composer makes about the target receipt paper (e.g., 32-char standard thermal width) — if no such assumption is encoded, say so explicitly.
