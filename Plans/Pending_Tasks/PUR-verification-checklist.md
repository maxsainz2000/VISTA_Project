---
module: Purchasing
source: Purchasing-audit-2026-05-15.md
generated: 2026-05-16
---

# Operator Verification Checklist — Purchasing

> Extracted from the 2026-05-15 module audit. Only operator/manual verification tasks are included here.
> Code changes are tracked as a separate plan file (PUR-15).

---

## PUR-14 — GoodsReceivedWithVatEvent Publisher

- [ ] Smoke test: confirm a goods receipt, then query `Acc_VatReturnLines` for the input-VAT row
  - **How:** Complete a purchase order receipt in the UI → open DB browser → query `SELECT * FROM Acc_VatReturnLines WHERE PurchaseOrderId = <id>`
- [ ] Verify ACC-10 / ACC-11 handlers are idempotent on `PurchaseOrderId`
  - **How:** Re-publish `GoodsReceivedWithVatEvent` for the same PO → confirm no duplicate VAT entries in `Acc_VatReturnLines`
