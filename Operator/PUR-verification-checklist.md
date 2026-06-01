---
module: Purchasing
source: Purchasing-audit-2026-06-01.md
originally-generated: 2026-05-17
last-synced: 2026-06-01
---

# Operator Verification Checklist — Purchasing

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 15 Purchasing plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).


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
3. Create a Purchase Order with items that all have the **same** VAT classification (for example, all "Vatable"). Use a VAT-inclusive unit cost such as ₱1,120 (= ₱1,000 net + ₱120 VAT).
4. Submit the PO, then go to **Goods Receiving** and receive the goods for that PO.
5. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
6. Run this query (replace `<PO_ID>` with the actual Purchase Order ID — you can find it in `Pur_PurchaseOrders`):
   ```sql
   SELECT Id, Description, Amount, VatableAmount, VatExemptAmount, InputVat, VatTreatment
   FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **Note:** `GoodsReceivedWithVatHandler` writes VAT data to `Acc_ExpenseRecords` (adding/updating the
> `VatableAmount`, `VatExemptAmount`, `InputVat`, and `VatTreatment` columns added by migration
> `20260510100000_AddVatLedgerColumns`). `Acc_VatReturnLines` is the BIR period-filing table and is
> only populated when you formally generate a VAT return — it is **not** written during goods receiving.

> **Event publisher:** `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\GoodsReceivingService.vb`
> **Event handler:** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- One row per line item on the PO appears in `Acc_ExpenseRecords`.
- `VatableAmount` = net cost (VAT-inclusive price ÷ 1.12), e.g. ₱1,000 for a ₱1,120 item.
- `InputVat` = VAT portion, e.g. ₱120.
- `VatExemptAmount` = 0.
- `VatTreatment` = 0 (the integer value for the `Vatable` enum member).

- [x] Single-classification receipt: input-VAT row appears in Acc_ExpenseRecords with correct VatableAmount and InputVat — PO-2026-0001 (id=1): VatableAmount=1000.0, InputVat=120.0, VatExemptAmount=0, VatTreatment=0 ✅ 2026-05-20

> **⚠️ Post-pivot regression fixed 2026-05-29 (`debug/PUR-vat-ledger-columns`):** Confirm Receipt threw
> `MySqlException: Unknown column 'InputVat' in 'field list'`. The VAT columns existed only in the SQLite-era
> migration `20260510100000_AddVatLedgerColumns`, which was never ported to the central MariaDB schema during
> the 2026-05-28 pivot. Fixed by new migration `Migrations/Central/AddAccVatLedgerColumns.sql`
> (`ADD COLUMN IF NOT EXISTS` on `Acc_ExpenseRecords` + `Acc_RevenueRecords`). See
> `agent_wiki/errors/efcore-vat-ledger-columns-missing-central-schema.md`.

---

### Test 2: Mixed-classification receipt sums correctly

**What to do:**
1. Create a new Purchase Order with items that have **different** VAT classifications. For example:
   - Line 1: "Vatable" item, unit cost ₱1,120 VAT-inclusive (₱1,000 net + ₱120 VAT)
   - Line 2: "VAT-Exempt" item, unit cost ₱500
2. Submit the PO, then receive the goods for this PO.
3. Run this query with the new PO ID:
   ```sql
   SELECT Id, Description, Amount, VatableAmount, VatExemptAmount, InputVat, VatTreatment
   FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **VAT calculator:** `WPF_Applications\MerchSys\src\MerchSys.Purchasing\Services\Vat\GoodsReceiptVatCalculator.vb`

**What you should see:**
- Two rows in `Acc_ExpenseRecords` — one per line item.
- Vatable row: `VatableAmount` = ₱1,000, `InputVat` = ₱120, `VatTreatment` = 0.
- Exempt row: `VatExemptAmount` = ₱500, `InputVat` = 0, `VatTreatment` = 1.
- The Exempt line contributes nothing to `InputVat`.

- [x] Mixed-classification receipt: VAT sums only from Vatable lines, Exempt excluded — PO-2026-0002 (id=2): Vatable line VatableAmount=1000.0 InputVat=120.0; Exempt line VatExemptAmount=500.0 InputVat=0.0 ✅ 2026-05-20

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
   SELECT COUNT(*) FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **Handler (checks for duplicates):** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- The count is the **same** as before. No new rows were created.
- The handler finds the existing `Acc_ExpenseRecords` row(s) by `(SourceModule='Purchasing', SourceReferenceId, Description.Contains(ProductName))` and **updates** them in place rather than inserting duplicates.

- [x] Re-publishing the event for the same PO does NOT create duplicate VAT entries — PO-2026-0003 (id=3): COUNT=1 after second Immediate Window publish, EF log shows SELECT→UPDATE (no INSERT), row detail: VatableAmount=6696.0 InputVat=803.57 VatTreatment=0 ✅ 2026-05-21
