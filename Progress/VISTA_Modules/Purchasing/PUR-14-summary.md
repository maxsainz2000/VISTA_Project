---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-12
plan-ref: Plans/VISTA_Modules/Purchasing/14-goods-received-vat-event.md
status: completed
---

## Task Summary

Implemented the `GoodsReceivedWithVatEvent` publisher for the Purchasing module (PUR-14).
The existing `GoodsReceivedEvent` emission in `GoodsReceivingService.ReceiveGoodsAsync` was
left intact; the new VAT-aware event fires immediately after it so legacy consumers are
unaffected and ACC-10 / ACC-11 now receive input-VAT data on every goods receipt.

**Plan:** `[[14-goods-received-vat-event]]`

## What Was Done

- Created `src/MerchSys.Purchasing/Services/Vat/GoodsReceiptVatCalculator.vb` — pure-function
  calculator; produces `GoodsReceiptVatBreakdown` (VatableInputs, VatExemptInputs, ZeroRatedInputs,
  InputVat, VendorInvoiceTotal) using the Option-2 simplification (all lines vatable at 12 %).
- Modified `src/MerchSys.Purchasing/Services/GoodsReceivingService.vb` — added
  `GoodsReceiptVatCalculator` constructor dependency; publishes `GoodsReceivedWithVatEvent`
  back-to-back with the legacy `GoodsReceivedEvent` after `SaveChangesAsync`.
- Modified `src/MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb` —
  registered `GoodsReceiptVatCalculator` as `AddScoped`.

## GoodsReceivingService.vb — git diff

```diff
@@ -4,6 +4,7 @@ Imports MerchSys.Purchasing.Data
 Imports MerchSys.Purchasing.Dtos
 Imports MerchSys.Purchasing.Entities
 Imports MerchSys.Purchasing.Helpers
+Imports MerchSys.Purchasing.Services.Vat
 Imports MerchSys.SharedKernel.Enums
 Imports MerchSys.SharedKernel.Events
 
@@ -15,11 +16,13 @@ Namespace Services
         Private ReadOnly _db As PurchasingDbContext
         Private ReadOnly _mediator As IMediator
         Private ReadOnly _priceChangeService As IPriceChangeService
+        Private ReadOnly _vatCalculator As GoodsReceiptVatCalculator
 
-        Public Sub New(db As PurchasingDbContext, mediator As IMediator, priceChangeService As IPriceChangeService)
+        Public Sub New(db As PurchasingDbContext, mediator As IMediator, priceChangeService As IPriceChangeService, vatCalculator As GoodsReceiptVatCalculator)
             _db = db
             _mediator = mediator
             _priceChangeService = priceChangeService
+            _vatCalculator = vatCalculator
         End Sub
 
@@ -86,6 +89,40 @@ Namespace Services
             Next
 
             Await _mediator.Publish(ev)
+
+            ' Publish VAT-aware sibling (INFRA-07 / PUR-14).  Both events fire so legacy consumers
+            ' (FIFO costing, stock movement, low-stock alerts) keep receiving GoodsReceivedEvent
+            ' while ACC-10 and ACC-11 consume GoodsReceivedWithVatEvent for input-VAT accounting.
+            ' NOTE — Option-2 simplification: all lines are treated as vatable at 12 % because
+            ' GoodsReceiptLine has no per-line VAT classification yet.  A follow-up plan should
+            ' add that column and replace this calculator with a per-line-aware version.
+            Dim vatBreakdown = _vatCalculator.Calculate(receipt, receipt.Lines)
+
+            Dim vatEvent As New GoodsReceivedWithVatEvent With {
+                .PurchaseOrderId = purchaseOrderId,
+                .ReceivedDate = receipt.ReceivedDate,
+                .VatableInput = vatBreakdown.VatableInputs,
+                .VatExemptInput = vatBreakdown.VatExemptInputs,
+                .ZeroRatedInput = vatBreakdown.ZeroRatedInputs,
+                .InputVat = vatBreakdown.InputVat
+            }
+
+            For Each grLine In receipt.Lines
+                Dim unitCostExcl As Decimal = Math.Round(grLine.UnitCost / 1.12D, 4)
+                Dim lineInputVat As Decimal = Math.Round(CDec(grLine.QuantityReceived) * (grLine.UnitCost - unitCostExcl), 2)
+                vatEvent.Items.Add(New GoodsReceivedWithVatEvent.GoodsReceivedItemWithVat With {
+                    .ProductId = grLine.ProductId,
+                    .ProductName = grLine.ProductName,
+                    .QuantityReceived = grLine.QuantityReceived,
+                    .UnitCost = Math.Round(unitCostExcl, 2),
+                    .ExpiryDate = grLine.ExpiryDate,
+                    .Treatment = VatTreatment.Vatable,
+                    .InputVat = lineInputVat
+                })
+            Next
+
+            Await _mediator.Publish(vatEvent)
+
             Await _priceChangeService.DetectChangesAsync(receipt.Id)
```

## Worked Example

Vendor invoice total: **₱11,200.00** (single-line receipt, 10 units × ₱1,120.00/unit, VAT-inclusive).

| Field | Calculation | Result |
|---|---|---|
| `VendorInvoiceTotal` | 10 × 1,120.00 | **₱11,200.00** |
| `VatableInputs` | 11,200.00 / 1.12 | **₱10,000.00** |
| `InputVat` | 11,200.00 − 10,000.00 | **₱1,200.00** |
| `VatExemptInputs` | Option-2 default | **₱0.00** |
| `ZeroRatedInputs` | Option-2 default | **₱0.00** |

Per-item (event `Items[0]`):

| Field | Value |
|---|---|
| `UnitCost` (VAT-exclusive) | Math.Round(1,120 / 1.12, 2) = **₱1,000.00** |
| `InputVat` | 10 × (1,120 − 1,000) = **₱1,200.00** |
| `Treatment` | `VatTreatment.Vatable` |

## Smoke-Test Confirmation

On every successful `ReceiveGoodsAsync` call:

1. `GoodsReceivedEvent` fires → Inventory FIFO batch and stock movement write proceed unchanged.
2. `GoodsReceivedWithVatEvent` fires immediately after → `ACC-10`'s handler writes the AP liability
   journal entry and `ACC-11`'s handler inserts a row into `Acc_VatReturnLines` with
   `TransactionType = 'InputVAT'`, `VatableAmount = VatableInput`, `VatAmount = InputVat`.

Expected `Acc_VatReturnLines` row after confirming a receipt:

```sql
SELECT TransactionType, VatableAmount, VatAmount
FROM   Acc_VatReturnLines
ORDER  BY CreatedAt DESC
LIMIT  1;
-- Expected: InputVAT | 10000.00 | 1200.00
```

## ⚠ Known Limitation — Option-2 Simplification

**Every goods-receipt line is treated as 100 % vatable at the 12 % Philippine VAT rate.**

This is correct for Villon Farm Supply's typical vendor transactions (all vatable agricultural
inputs) but will over-state input VAT if the business ever purchases VAT-exempt or zero-rated
goods from the same supplier.

**Root cause:** `GoodsReceiptLine` does not yet carry a `VatClassification` (Vatable / Exempt /
ZeroRated) column or a per-line `VatAmount` column.  Until those fields exist, the calculator
cannot do a line-by-line breakdown.

**Future work:** A follow-up plan should:
1. Add `VatClassification As VatTreatment` and `VatAmount As Decimal` to `GoodsReceiptLine`
   (schema migration + receiving-UI fields).
2. Replace `GoodsReceiptVatCalculator` with a per-line-aware version that reads those columns
   instead of defaulting everything to vatable.
3. Remove the Option-2 comment block from `GoodsReceivingService` once the upgrade ships.

This limitation is also documented in the `GoodsReceiptVatCalculator` XML doc comment.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings (pre-existing POS warning unrelated) |
| Unit tests pass | N/A |
| Manual verification | Pending smoke test against live DB |

## Issues Encountered

None. `GoodsReceivedWithVatEvent` field names confirmed from INFRA-07 source
(`PurchaseOrderId`, `ReceivedDate`, `VatableInput`, `VatExemptInput`, `ZeroRatedInput`, `InputVat`,
`Items`) — the plan's pseudo-code used slightly different names (`VendorId`, `VatableInputs`)
which are not on the actual event; implemented against the real definition.

## What's Next

- [ ] Follow-up plan: add per-line `VatClassification` + `VatAmount` to `GoodsReceiptLine` and extend the goods-receiving UI.
- [ ] Smoke test: confirm a receipt, then query `Acc_VatReturnLines` for the input-VAT row.
- [ ] Verify ACC-10 / ACC-11 handlers are idempotent on `PurchaseOrderId` (plan notes this as a consumer-side responsibility).

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[cross-module-data-flow]]`
- Agent Wiki entries consulted: `[[feedback_vbnet_await_catch]]` (no Await in Catch/Finally — no try/catch added here)
- INFRA-07: `SharedKernel/Events/GoodsReceivedWithVatEvent.vb` — payload definition
- ACC-10 / ACC-11: consumer handlers (not modified)
