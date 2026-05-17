---
module: Purchasing
audit-date: 2026-05-17
---

# VISTA Module Audit — Purchasing

**Audit Date:** 2026-05-17
**Plans Folder:** `Plans/VISTA_Modules/Purchasing/`
**Progress Folder:** `Progress/VISTA_Modules/Purchasing/`

---

## What's Next Cleanup (Step 0)

**Items resolved this pass:** 1

| Summary | Item | Resolved By |
|---------|------|-------------|
| PUR-14 | Follow-up plan: add per-line VatClassification + VatAmount to GoodsReceiptLine and extend goods-receiving UI | PUR-15 |

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| PUR-01 | Purchasing Domain Models | ✅ Completed |
| PUR-02 | Purchasing Data Access | ✅ Completed |
| PUR-03 | PO Lifecycle Service | ✅ Completed |
| PUR-04 | Goods Receiving | ✅ Completed |
| PUR-05 | Vendor Directory | ✅ Completed |
| PUR-06 | AP Tracking | ✅ Completed |
| PUR-07 | Reorder Suggestion Engine | ✅ Completed |
| PUR-08 | Price Change Detection | ✅ Completed |
| PUR-09 | View — PO Management | ✅ Completed |
| PUR-10 | View — Goods Receiving | ✅ Completed |
| PUR-11 | View — Vendor Directory | ✅ Completed |
| PUR-12 | View — AP Ledger | ✅ Completed |
| PUR-13 | View — Reorder Suggestions | ✅ Completed |
| PUR-14 | GoodsReceivedWithVatEvent Publisher | ✅ Completed |
| PUR-15 | GoodsReceiptLine VAT Classification Extension | ✅ Completed |

**Total Plans:** 15
**Completed:** 15 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### PUR-14 — GoodsReceivedWithVatEvent Publisher

**Status:** Completed

- [ ] Smoke test: confirm a receipt, then query `Acc_VatReturnLines` for the input-VAT row
- [ ] Verify ACC-10 / ACC-11 handlers are idempotent on `PurchaseOrderId`

### PUR-15 — GoodsReceiptLine VAT Classification Extension

**Status:** Completed

- [ ] Smoke test: confirm a mixed-classification receipt (one Vatable, one Exempt line) and verify `Acc_VatReturnLines` input-VAT row sums correctly
- [ ] Verify ACC-10 / ACC-11 handlers are idempotent on `PurchaseOrderId`

---

## Plans With No Progress File

*(None — all 15 plans have corresponding completed progress summaries.)*

---

## Amendments & Special Files

- `PUR-03-amendment.md` — Amendment to the PO Lifecycle Service plan; documents deviations or additions made during implementation of PUR-03.

---

## Summary & Recommendations

- **100% complete** — all 15 Purchasing plans have completed progress summaries.
- **4 pending `[ ]` tasks** — all are smoke tests / idempotency checks targeting the new VAT event pipeline (PUR-14/PUR-15). These require a live database with `Acc_VatReturnLines` populated.
- **PUR-14 and PUR-15 overlap on idempotency check** — "Verify ACC-10/ACC-11 handlers are idempotent on `PurchaseOrderId`" appears in both summaries. A single test pass covers both.
- **PUR-09/PUR-10 plain-text notes** — both summaries have unformatted (non-checkbox) What's Next items; `IPurchaseOrderService` extension for `Notes`/`ExpectedDeliveryDate` (PUR-09) and `Notification.Wpf` toast integration (PUR-10) remain open but are not tracked as formal pending tasks in this audit.
- **No blocking dependencies** — all pending items are runtime validation tasks, not implementation blockers.
