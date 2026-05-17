---
module: Purchasing
source: Purchasing-audit-2026-05-17.md
generated: 2026-05-17
---

# Operator Verification Checklist — Purchasing

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 15 Purchasing plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.

### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| GoodsReceivingService | `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\GoodsReceivingService.vb` |
| GoodsReceivedWithVatEvent | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Events\GoodsReceivedWithVatEvent.vb` |
| GoodsReceivedWithVatHandler | `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb` |
| GoodsReceiptVatCalculator | `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\Vat\GoodsReceiptVatCalculator.vb` |

---

## PUR-14 + PUR-15 — VAT Event Pipeline (combined)

> PUR-14 and PUR-15 share the same idempotency check. The tests below cover both plans.

### Test 1: Single-classification receipt creates VAT entries

**What to do:**
1. Launch the app (F5).
2. Go to the **Purchasing** section in the sidebar.
3. Create a Purchase Order with items that all have the **same** VAT classification (for example, all "Vatable").
4. Go to **Goods Receiving** and receive the goods for that PO.
5. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
6. Run this query (replace `<PO_ID>` with the actual Purchase Order ID — you can find it in `Pur_PurchaseOrders`):
   ```sql
   SELECT * FROM Acc_VatReturnLines WHERE PurchaseOrderId = <PO_ID>;
   ```

> **Event publisher:** `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\GoodsReceivingService.vb`
> **Event handler:** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- At least one row appears in `Acc_VatReturnLines`.
- The row has the correct input-VAT amount based on the PO total and VAT rate.

- [ ] Single-classification receipt: input-VAT row appears in Acc_VatReturnLines

---

### Test 2: Mixed-classification receipt sums correctly

**What to do:**
1. Create a new Purchase Order with items that have **different** VAT classifications. For example:
   - Line 1: "Vatable" item worth ₱1,000
   - Line 2: "VAT-Exempt" item worth ₱500
2. Receive the goods for this PO.
3. Run the same query as above with the new PO ID:
   ```sql
   SELECT * FROM Acc_VatReturnLines WHERE PurchaseOrderId = <PO_ID>;
   ```

> **VAT calculator:** `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\Vat\GoodsReceiptVatCalculator.vb`

**What you should see:**
- The `Acc_VatReturnLines` entry shows input-VAT calculated **only** from the Vatable line(s).
- The Exempt line does NOT add to the VAT amount.
- The total VAT amount makes sense (e.g., 12% of ₱1,000 = ₱120 for the Vatable line).

- [ ] Mixed-classification receipt: VAT sums only from Vatable lines, Exempt excluded

---

### Test 3: Handler is idempotent (no duplicate VAT entries)

> This single test covers both PUR-14 and PUR-15 idempotency requirements.

**What to do:**
1. Use one of the POs from Test 1 or Test 2.
2. Find a way to re-publish the `GoodsReceivedWithVatEvent` for the same PO. You can do this by:
   - Setting a breakpoint in `GoodsReceivingService.vb` where the event is published, OR
   - Calling the event publisher manually from the Immediate Window, OR
   - Simply receiving the same PO again if the UI allows it.
3. After triggering the event a second time, run the query again:
   ```sql
   SELECT COUNT(*) FROM Acc_VatReturnLines WHERE PurchaseOrderId = <PO_ID>;
   ```

> **Handler (checks for duplicates):** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- The count is the **same** as before. No new duplicate rows were created.
- The ACC-10 / ACC-11 handlers correctly detect that this PO was already processed and skip it.

- [ ] Re-publishing the event for the same PO does NOT create duplicate VAT entries
