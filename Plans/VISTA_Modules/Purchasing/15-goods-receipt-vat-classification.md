---
module: MerchSys.Purchasing
plan-id: PUR-15
title: "GoodsReceiptLine VAT Classification Extension"
depends-on: [PUR-04, PUR-14]
estimated-files: 5
priority: medium
---

# GoodsReceiptLine VAT Classification Extension

## Context

PUR-14 delivered `GoodsReceivedWithVatEvent` which publishes input VAT data when goods are received. However, the 2026-05-15 Purchasing audit notes that the current implementation uses **aggregate-level** VAT calculation — the entire goods receipt carries a single VAT amount computed from the total. BIR compliance requires **per-line** VAT classification because different products may have different VAT treatments (Vatable at 12%, VAT-Exempt, or Zero-Rated).

This plan extends `GoodsReceiptLine` with per-line `VatClassification` and `VatAmount` fields and updates the goods-receiving UI to capture these values.

## Prerequisites

- **PUR-04** (Goods Receiving) — `GoodsReceivingService`, `GoodsReceiptLine` entity, goods receipt flow
- **PUR-14** (GoodsReceivedWithVatEvent Publisher) — `GoodsReceivedWithVatEvent`, VAT event publishing flow

## Wiki References

- `concepts/vat-ready.md` — Three-bucket VAT classification (Vatable, Exempt, Zero-Rated)
- `concepts/bir-compliance.md` — Per-line VAT treatment for input tax credits

## Deliverables

```
MerchSys.Purchasing/Entities/
└── GoodsReceiptLine.vb                         ' Modified — add VatClassification, VatAmount

MerchSys.Purchasing/Data/Migrations/
└── AddGoodsReceiptLineVatColumns.vb            ' New — EF migration

MerchSys.Purchasing/Services/
└── GoodsReceivingService.vb                    ' Modified — compute per-line VAT

MerchSys.App/Views/Purchasing/
└── GoodsReceivingView.xaml                     ' Modified — add VAT column to line items grid

MerchSys.App/Views/Purchasing/
└── GoodsReceivingView.xaml.vb                  ' Modified — bind VAT fields
```

## Specification

### GoodsReceiptLine Extensions

```vb
' Add to existing GoodsReceiptLine entity
Public Property VatClassification As VatClassificationType    ' Vatable, VatExempt, ZeroRated
Public Property VatAmount As Decimal                          ' Computed: LineTotal * 0.12 for Vatable, 0 otherwise
Public Property VatableSales As Decimal                       ' LineTotal / 1.12 for Vatable
```

`VatClassificationType` is the existing enum from POS-14 / SharedKernel — reuse it.

### GoodsReceivingService Modification

When processing a goods receipt line:
1. Look up the product's default `VatClassification` (from `Product.VatClassification` if it exists, otherwise default to `Vatable`).
2. Compute `VatAmount` based on classification:
   - `Vatable` → `LineTotal - (LineTotal / 1.12)` (VAT-inclusive pricing)
   - `VatExempt` or `ZeroRated` → `0`
3. Compute `VatableSales` → `LineTotal - VatAmount` for Vatable, `LineTotal` for others.
4. Update `GoodsReceivedWithVatEvent` to carry per-line VAT details instead of aggregate.

### UI Changes

Add a `VatClassification` combobox column to the goods receipt line items DataGrid. Default value from product lookup, editable by user. `VatAmount` column is read-only (auto-computed).

### Migration

```vb
' AddGoodsReceiptLineVatColumns migration
migrationBuilder.AddColumn(Of Integer)("VatClassification", "Pur_GoodsReceiptLines", defaultValue:=0)
migrationBuilder.AddColumn(Of Decimal)("VatAmount", "Pur_GoodsReceiptLines", defaultValue:=0D)
migrationBuilder.AddColumn(Of Decimal)("VatableSales", "Pur_GoodsReceiptLines", defaultValue:=0D)
```

## Implementation Notes

- `VatClassificationType` enum should already exist in SharedKernel from POS-14. Verify and reuse — do not create a duplicate.
- The default `VatClassification = Vatable` means existing receipts (before migration) will show as Vatable with `VatAmount = 0`. This is acceptable — historical receipts were not VAT-classified.
- The ACC-10/ACC-11 handlers that consume `GoodsReceivedWithVatEvent` may need to be updated to handle per-line data. That is a cross-module concern tracked in the master tracker.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally`.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `GoodsReceiptLine` has `VatClassification`, `VatAmount`, and `VatableSales` properties.
3. Migration applies cleanly to fresh and existing SQLite databases.
4. Goods-receiving UI shows VAT classification column with dropdown.
5. `VatAmount` is auto-computed based on classification and line total.
6. `GoodsReceivedWithVatEvent` carries per-line VAT details.
7. Existing receipts (pre-migration) display without errors.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-15-summary.md` using `Progress/_template.md`. Include:
- The per-line VAT computation formulas.
- Schema migration details.
- Note on ACC-10/ACC-11 handler compatibility.

### Documentation
- XML doc on `GoodsReceiptLine.VatClassification` describing the three-bucket model and BIR reference.
- Inline comment on the VAT computation citing the 12% rate source.
