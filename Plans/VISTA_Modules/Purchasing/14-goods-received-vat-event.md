---
module: MerchSys.Purchasing
plan-id: PUR-14
title: "GoodsReceivedWithVatEvent Publisher"
depends-on: [PUR-04, INFRA-07, ACC-10, ACC-11]
estimated-files: 2
---

# GoodsReceivedWithVatEvent Publisher

## Context

INFRA-07 defined the cross-module VAT event payload contracts, including `GoodsReceivedWithVatEvent` — the input-VAT equivalent of `SaleCompletedWithVatEvent`. POS-14 wired the publisher for the sale side. The Accounting consumer handlers (ACC-10 schema, ACC-11 reporting) already subscribe to **both** VAT events and write to the input/output VAT ledgers accordingly.

The 2026-05-11 Infrastructure audit flagged the gap: **Purchasing never landed the publisher** for `GoodsReceivedWithVatEvent`. The legacy `GoodsReceivedEvent` is still fired by `PUR-04`'s goods-receipt confirmation flow, but its VAT-aware sibling is missing. As a result, the BIR VAT reporting service is silently under-counting input VAT — every goods receipt with VAT on the vendor invoice is invisible to the input-VAT side of the BIR Form 2550M/Q.

This plan adds the publisher alongside the existing event emission. It does not modify the legacy event; both fire in sequence so any existing handler keeps working. It also adds the input-VAT calculation step from the receiving form's captured values (vendor invoice VAT, vatable/exempt/zero-rated breakdown).

## Prerequisites

- **PUR-04** (Goods Receiving) — `IGoodsReceivingService.ConfirmReceiptAsync`, current `GoodsReceivedEvent` publication site
- **INFRA-07** (Cross-Module VAT Event Payload Contracts) — `GoodsReceivedWithVatEvent` payload definition in `SharedKernel`
- **ACC-10** (Accounting VAT Ledger Schema) — consumer side schema for input VAT
- **ACC-11** (BIR VAT Reporting Service) — handler that already subscribes to the event

## Wiki References

- `concepts/bir-compliance.md` — Input VAT credit; vendor invoice as the source of truth for input VAT amount
- `analysis/cross-module-data-flow.md` — `GoodsReceivedWithVatEvent` flow

## Deliverables

```
MerchSys.Purchasing/Services/Vat/
└── GoodsReceiptVatCalculator.vb                         ' New — pure function over GR lines

MerchSys.Purchasing/Services/GoodsReceivingService.vb    ' Modified — publish the new event
```

## Specification

### GoodsReceiptVatCalculator

A small, pure-function class that consumes the goods-receipt lines (with vendor-invoice VAT data already captured at receipt time) and produces the three-bucket breakdown plus input VAT:

```
Public Class GoodsReceiptVatCalculator

    Public Function Calculate(
        receipt As GoodsReceipt,
        lines As IReadOnlyList(Of GoodsReceiptLine)
    ) As GoodsReceiptVatBreakdown

End Class

Public Class GoodsReceiptVatBreakdown
    Public Property VatableInputs As Decimal
    Public Property VatExemptInputs As Decimal
    Public Property ZeroRatedInputs As Decimal
    Public Property InputVat As Decimal                  ' Sum of per-line VAT amounts
    Public Property VendorInvoiceTotal As Decimal
End Class
```

The calculator presumes `GoodsReceiptLine` carries per-line VAT classification — confirm by reading PUR-04 before implementing. If it does **not**, either:

1. Extend `GoodsReceiptLine` with `VatClassification` and per-line `VatAmount` columns (small schema migration, costs an additional file) — or
2. Default every line to `VatableInputs` at the 12% VAT rate and document this as a known limitation in the summary.

This plan picks **option 2** for scope-discipline reasons; a follow-up plan can introduce per-line classification once the receiving UI grows the corresponding fields. The choice is documented at the calculator level and is the entire reason this plan ships in 2 files instead of 4.

### GoodsReceivingService.vb modification

The current `ConfirmReceiptAsync` flow publishes `GoodsReceivedEvent` at the end of a successful confirmation. Add the new publisher immediately after, **inside the same transaction** if the publish path is transactional in this codebase (it is via INFRA-04's MediatR setup, typically post-`SaveChangesAsync`):

```
Dim vatBreakdown = _vatCalculator.Calculate(receipt, lines)

Dim legacyEvent As New GoodsReceivedEvent With { ... }   ' Existing
Await _eventBus.PublishAsync(legacyEvent, ct)

Dim vatEvent As New GoodsReceivedWithVatEvent With {
    .GoodsReceiptId = receipt.Id,
    .VendorId = receipt.VendorId,
    .ReceivedAt = receipt.ReceivedAt,
    .VatableInputs = vatBreakdown.VatableInputs,
    .VatExemptInputs = vatBreakdown.VatExemptInputs,
    .ZeroRatedInputs = vatBreakdown.ZeroRatedInputs,
    .InputVat = vatBreakdown.InputVat,
    .VendorInvoiceTotal = vatBreakdown.VendorInvoiceTotal
}
Await _eventBus.PublishAsync(vatEvent, ct)
```

The exact field names on `GoodsReceivedWithVatEvent` must match INFRA-07's definition verbatim — read that file first.

The constructor of `GoodsReceivingService` gains a `GoodsReceiptVatCalculator` dependency.

### DI registration

```
services.AddScoped(Of GoodsReceiptVatCalculator)
```

In the Purchasing module's existing DI extension.

## Implementation Notes

- Both events are published — do **not** replace the legacy `GoodsReceivedEvent`. Existing handlers (FIFO costing, stock movement writes, low-stock alerts) all subscribe to it.
- The two publishes happen back-to-back; if the legacy publish succeeds and the VAT publish throws, the goods-receipt row is already saved and the legacy event is already in flight. The Accounting handler must be idempotent on receipt of `GoodsReceivedWithVatEvent` — confirm this when reading ACC-10 / ACC-11. If it is not idempotent, the rollback semantics belong in a separate plan (this one does not own the consumer behaviour).
- The "default everything to VATable Inputs at 12%" path is a deliberate scope concession. Document it loudly in the implementation summary so a future agent does not mistake it for a fully-correct implementation. The BIR will accept this for businesses that genuinely have only VATable input purchases — which is the common Villon Farm Supply case — but a future plan should add per-line classification before the system serves businesses with mixed input types.
- Per the feedback memory (`feedback_vbnet_await_catch.md`): no `Await` inside `Catch`/`Finally`. If the modification wraps the dual publish in a `Try/Catch`, the error logging path captures state and awaits outside the block.
- This plan does not modify INFRA-07's payload definition. If a field is missing for the calculation results, that is INFRA-07's bug; raise it separately rather than silently extending the payload here.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `GoodsReceiptVatCalculator` returns a `GoodsReceiptVatBreakdown` for any non-empty receipt; total of the three buckets equals `VendorInvoiceTotal`.
3. With the option-2 simplification, `Calculate(...)` produces `VatableInputs = invoiceTotal / 1.12`, `InputVat = invoiceTotal - vatableInputs`, `VatExemptInputs = 0`, `ZeroRatedInputs = 0`.
4. `GoodsReceivingService.ConfirmReceiptAsync` publishes both `GoodsReceivedEvent` and `GoodsReceivedWithVatEvent` in order on every successful confirmation.
5. If the VAT publish throws, the legacy event is still in flight (no rollback on the legacy side) — documented as expected behaviour.
6. The ACC-10/ACC-11 handlers receive the new event and update their VAT ledger (verify via a smoke test: confirm a receipt, then query `Acc_VatReturnLines` for an input-VAT row).
7. The constructor gain is the only signature change to `GoodsReceivingService`; existing call sites continue to compile.
8. No edits to PUR-04 source files outside `GoodsReceivingService.vb` itself.
9. No edits to INFRA-07's event payload definition.
10. The DI registration of `GoodsReceiptVatCalculator` lives in the Purchasing module's DI extension.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-14-summary.md` using `Progress/_template.md`. Include:

- The exact `git diff` of `GoodsReceivingService.vb` showing the new publish step.
- A worked example: vendor invoice total ₱11,200 ⇒ `VatableInputs ≈ ₱10,000`, `InputVat = ₱1,200`.
- A smoke-test confirmation that `Acc_VatReturnLines` gains one input-VAT row per receipt.
- A loud, dedicated section labelled **"Known limitation"** documenting the option-2 simplification and recommending the future per-line-classification plan.

### Documentation
- XML doc on `GoodsReceiptVatCalculator` documenting the option-2 assumption and the future-plan recommendation.
- XML doc on the new publish-site comment in `GoodsReceivingService` citing INFRA-07 and the dual-event-emission rationale.
