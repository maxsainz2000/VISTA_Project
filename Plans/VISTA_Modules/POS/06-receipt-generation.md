---
module: MerchSys.POS
plan-id: POS-06
title: "Receipt Generation"
depends-on: [POS-03]
estimated-files: 2
---

# Receipt Generation

## Context

Implements BIR-compliant official receipt generation — OR-YYYY-XXXX format, sequential numbering, all mandated fields, VAT-ready structure. Addresses Problem S3.

## Prerequisites

- **POS-03** (Cart & Transaction) — `SalesTransaction` created and finalized

## Wiki References

- `concepts/bir-compliance.md` — NIRC §113, receipt requirements, OR-YYYY-XXXX format
- `concepts/vat-ready.md` — VAT-registered vs non-VAT configuration

## Deliverables

```
MerchSys.POS/Services/
├── IReceiptService.vb
└── ReceiptService.vb
```

## Specification

### IReceiptService
```
GenerateReceiptAsync(transactionId As Integer) As Task(Of OfficialReceipt)
GetReceiptAsync(receiptNumber As String) As Task(Of OfficialReceipt)
GetReceiptByTransactionAsync(transactionId As Integer) As Task(Of OfficialReceipt)
PrintReceiptAsync(receiptId As Integer) As Task    ' Formats for printing
```

### Receipt Number Generation
- Format: `OR-YYYY-XXXX` (e.g., OR-2026-0001)
- **Strictly sequential** — no gaps, no duplicates
- Resets sequence each year
- Thread-safe generation (same pattern as PO numbers)

### Receipt Content (BIR-mandated)
- Business Name: "Villon Farm Supply"
- Business Address (from config)
- Business TIN (from config)
- Date and time of transaction
- Itemized list: product name, qty, unit price, line total
- Subtotal, Discount, VAT (if applicable), Grand Total
- Receipt Number (OR-YYYY-XXXX)
- Payment method

### VAT Logic
- If `IsVatRegistered = True`: show VAT breakdown (12%)
  - `VatableAmount = TotalAmount / 1.12`
  - `VatAmount = TotalAmount - VatableAmount`
- If `IsVatRegistered = False`: no VAT line on receipt

### Print Formatting
Generate a formatted text block suitable for thermal receipt printer (80mm width):
- 40-character line width
- Centered header
- Left-aligned items
- Right-aligned amounts
- Separator lines

## Acceptance Criteria

1. `dotnet build` succeeds
2. Receipt numbers follow OR-YYYY-XXXX, strictly sequential
3. All BIR-mandated fields present
4. VAT calculated correctly when applicable
5. Print format fits 80mm thermal printer

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-06-summary.md`
