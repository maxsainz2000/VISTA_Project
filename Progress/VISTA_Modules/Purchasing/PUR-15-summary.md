---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-16
plan-ref: Plans/VISTA_Modules/Purchasing/15-goods-receipt-vat-classification.md
status: completed
---

## Task Summary

Implemented per-line VAT classification on `GoodsReceiptLine`, replacing the PUR-14 Option-2
aggregate simplification. Each receipt line now carries its own `VatClassification`, `VatAmount`,
and `VatableSales` computed at receiving time. The goods-receiving UI exposes a VAT classification
ComboBox column and a read-only VAT amount column. `GoodsReceivedWithVatEvent` now carries true
per-line VAT data.

**Plan:** `[[15-goods-receipt-vat-classification]]`

## What Was Done

- Modified `src/MerchSys.Purchasing/Entities/GoodsReceiptLine.vb` — added `VatClassification As VatTreatment`, `VatAmount As Decimal`, and `VatableSales As Decimal` with BIR XML doc referencing NIRC Sec. 106/110.
- Modified `src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptLineConfiguration.vb` — configured `HasDefaultValue(0)`, `HasPrecision(18,2)`, and `HasDefaultValue(0D)` for the three new columns.
- Created `src/MerchSys.Purchasing/Data/Migrations/AddGoodsReceiptLineVatColumns.vb` — manual EF migration class (documentation artifact; actual runtime migration is in DatabaseInitializer per `efcore10-vbnet-migration-discovery-bug`).
- Modified `src/MerchSys.App/Data/DatabaseInitializer.vb` — added `20260516140000_AddGoodsReceiptLineVatColumns` migration that `ALTER TABLE`s `Pur_GoodsReceiptLines` to add the three columns with safe defaults.
- Modified `src/MerchSys.Purchasing/Dtos/ReceiveGoodsDto.vb` — added `VatClassification As VatTreatment` property.
- Modified `src/MerchSys.Purchasing/Services/GoodsReceivingService.vb` — computes `VatAmount` and `VatableSales` per line during receipt creation; event item loop now reads `grLine.VatClassification` and `grLine.VatAmount` instead of hardcoding `Vatable`.
- Modified `src/MerchSys.Purchasing/Services/Vat/GoodsReceiptVatCalculator.vb` — replaced Option-2 single-bucket logic with per-classification `Select Case` that reads `line.VatableSales` and `line.VatAmount` to fill the three aggregate buckets.
- Modified `src/MerchSys.Purchasing/ViewModels/GoodsReceivingViewModel.vb` — added `VatClassification` to `GRLineItem` (defaults to `Vatable`); auto-computes `VatAmount` when `VatClassification`, `QtyReceived`, or `UnitCost` changes; added `VatTreatmentValues` list to ViewModel for ComboBox binding; `ConfirmReceiptAsync` passes `VatClassification` in DTO.
- Modified `src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml` — added `DataGridTemplateColumn` with `ComboBox` bound to `VatClassification` (ItemsSource via `RelativeSource` to ViewModel `VatTreatmentValues`); added read-only `DataGridTextColumn` for `VatAmount` (N2 format, green foreground).

## Per-Line VAT Computation Formulas

| Classification | VatableSales | VatAmount |
|---|---|---|
| `Vatable` | `Math.Round(lineTotal / 1.12, 2)` | `Math.Round(lineTotal − VatableSales, 2)` |
| `Exempt` | `lineTotal` | `0` |
| `ZeroRated` | `lineTotal` | `0` |

Where `lineTotal = QuantityReceived × UnitCost` (VAT-inclusive vendor invoice price).

**Aggregate event fields** (`GoodsReceivedWithVatEvent`):
- `VatableInput` = sum of `VatableSales` for Vatable lines
- `VatExemptInput` = sum of `lineTotal` for Exempt lines
- `ZeroRatedInput` = sum of `lineTotal` for ZeroRated lines
- `InputVat` = sum of `VatAmount` across all lines

## Schema Migration Details

Migration ID: `20260516140000_AddGoodsReceiptLineVatColumns`

```sql
ALTER TABLE "Pur_GoodsReceiptLines" ADD COLUMN "VatClassification" INTEGER NOT NULL DEFAULT 0
ALTER TABLE "Pur_GoodsReceiptLines" ADD COLUMN "VatAmount"         TEXT    NOT NULL DEFAULT '0'
ALTER TABLE "Pur_GoodsReceiptLines" ADD COLUMN "VatableSales"      TEXT    NOT NULL DEFAULT '0'
```

Existing receipts (pre-migration) will show `VatClassification = 0` (Vatable) with `VatAmount = 0`
and `VatableSales = 0`. This is acceptable — historical receipts were not per-line VAT classified.
They display without errors in all views.

## ACC-10 / ACC-11 Handler Compatibility

`GoodsReceivedWithVatEvent` shape is unchanged — same properties, same types. The aggregate fields
(`VatableInput`, `VatExemptInput`, `ZeroRatedInput`, `InputVat`) and per-item `Items` collection
both still exist. The only behavioral change is that values are now derived from per-line
`VatClassification` rather than the Option-2 assumption. ACC-10 and ACC-11 handlers are not
affected and do not require modification.

For businesses that only purchase Vatable goods (the Villon Farm Supply typical case), the
resulting `GoodsReceivedWithVatEvent` values are identical to the pre-PUR-15 output.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings (pre-existing POS warning unrelated) |
| Unit tests pass | N/A |
| Manual verification | ✅ Passed (verified mixed-classification receipt sums correctly in Acc_ExpenseRecords) |

## Issues Encountered

None.

- `VatTreatment` enum in SharedKernel uses `Vatable / Exempt / ZeroRated` (not `VatClassificationType` as the plan spec suggested) — implemented against the actual enum as required.
- `GoodsReceivingView.xaml.vb` code-behind required no changes (no new constructor dependency).

## What's Next

- [x] Smoke test: confirm a mixed-classification receipt (one Vatable, one Exempt line) and verify `Acc_VatReturnLines` input-VAT row sums correctly. *(completed/verified in Operator checklist)*
- [x] Verify ACC-10 / ACC-11 handlers are idempotent on `PurchaseOrderId`. *(completed/verified in Operator checklist)*

## Cross-References

- Domain Wiki pages consulted: `[[vat-ready]]`, `[[bir-compliance]]`
- Agent Wiki entries consulted: `[[efcore10-vbnet-migration-discovery-bug]]`, `[[feedback_vbnet_await_catch]]`
- PUR-14 summary: Option-2 simplification now fully replaced.
