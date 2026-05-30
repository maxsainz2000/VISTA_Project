---
module: Security, Roles & E2E Capabilities
source: WPF_Applications
originally-generated: 2026-05-27
last-synced: 2026-05-29
infra-migration: INFRA-23 to INFRA-30 (Pure MariaDB client-server; Activity Rail sidebar; Multi-batch FIFO; Bir VAT)
reset: 2026-05-29 (Database factory-reset baseline applied)
verified: (pending)
verified-by: (pending)
verdict: (pending)
---

# Operator Verification Checklist — Manager Role & E2E System Capabilities

> **Target Role:** Manager (`manager`)
> **Credentials (Factory Reset):** Username: `manager` | Password: `Vista2026!` *(Triggers DA6 first-login mandatory password change on next boot)*
> **Focus:** Full Operational Capability, Multi-Batch FIFO COGS, Dynamic Retail Pricing, Purchase Order Valuations, and MariaDB Ledgers.
>
> **Prerequisite State:** The central MariaDB `merchsys_central` has been wiped to a pristine "factory-reset baseline" (0 purchase orders, 0 receipts, 0 stock batches, 0 transactions). The reference data (20 products, 3 vendors, 3 credit accounts, VAT singleton configuration) is fully seeded, and default credential hashes are restored, waiting for the first login setup.

---

## Part 0: First-Login Password Setup & Security (DA6)

### Test 0.1: Mandatory Password Change on First Login
*Verifies that the system detects a NULL password change date and enforces a secure password change before launching the workspace.*

**Step-by-Step Actions:**
1. Launch the VISTA application (press **F5** in Visual Studio or execute `dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App`).
2. At the login window, enter:
   - **Username:** `manager`
   - **Password:** `Vista2026!`
3. Click the **Login** button.
4. **Observe:** The application blocks opening the main dashboard and instead displays the **Mandatory Password Change** form.
5. In the password fields, attempt to enter the same password:
   - **New Password:** `Vista2026!`
   - **Confirm Password:** `Vista2026!`
6. Click **Change Password**.
   - **Observe:** The UI rejects this with validation error message: *"New password must differ from the current password."*
7. Now, enter a valid new password:
   - **New Password:** `VistaTest1!`
   - **Confirm Password:** `VistaTest1!`
8. Click **Change Password**.

**Expected Output:**
- [ ] The password change is accepted.
- [ ] The window transitions immediately, opening the VISTA Shell Dashboard with the current user header showing **manager** in bold, with active role **Manager**.
- [ ] Direct MariaDB Audit: Run the following query in the MariaDB CLI:
  ```sql
  SELECT Username, LastPasswordChangeAt FROM Sys_UserAccounts WHERE Username = 'manager';
  ```
  Verify that `LastPasswordChangeAt` is populated with a non-null UTC timestamp.

*Status Check:*
- **Validation error string seen on same-password attempt:** __________________________________
- **LastPasswordChangeAt timestamp in DB:** __________________________________

---

## Part 1: Sidebar Navigation & Shell Header Verification (INFRA-30)

### Test 1.1: Activity Rail & Module Detail Panel Navigation
*Verifies that the Manager has unrestricted access to all 4 business modules + Developer Tools.*

**Step-by-Step Actions:**
1. Focus on the left-most sidebar of the application window.
2. Verify the **60px Activity Rail** contains five vertical module icons: **PUR** (Purchasing), **INV** (Inventory), **POS** (Point of Sale), **ACC** (Accounting), and **DEV** (Developer Tools).
3. Switch between modules using the keyboard shortcuts and verify the **220px Module Detail Panel** adjacent to the rail updates instantly:
   - Press `Ctrl+1` -> Panel switches to **Purchasing**.
   - Press `Ctrl+2` -> Panel switches to **Inventory**.
   - Press `Ctrl+3` -> Panel switches to **POS**.
   - Press `Ctrl+4` -> Panel switches to **Accounting**.
   - Press `Ctrl+0` -> Panel switches to **Developer Tools**.

**Expected Output:**
- [ ] **Purchasing Panel** lists exactly: `Purchase Orders`, `Goods Receiving`, `Vendor Directory`, `Accounts Payable`, `Reorder Suggestions`, and `Vendor Product Catalog`.
- [ ] **Inventory Panel** lists exactly: `Stock Dashboard`, `Product Management`, `Expiry Monitor`, and `Shrinkage`.
- [ ] **POS Panel** lists exactly: `Sales Cart`, `Credit Management`, `Transaction History`, `Daily Summary`, and `VAT Settings`.
- [ ] **Accounting Panel** lists exactly: `Financial Overview`, `Income Statement`, `Sales Summary`, `Tamper Audit Report`, `VAT Relief Report`, and `VAT Return (BIR)`.
- [ ] **Developer Tools Panel** (Debug builds only) lists: `Run VAT Schema Harness`.
- [ ] Look at the bottom of the Module Detail Panel: the **Connection Status Badge** shows a solid green dot and reads **Online**, indicating a persistent link to `merchsys_central` (INFRA-28).

*Status Check:*
- **Total sidebar navigation links count:** _________ (Expected: 22)
- **Connection Status Badge color/text:** __________________

---

## Part 2: Factory-Reset Stock Dashboard Audit

### Test 2.1: Default Landing Page Baseline Verification
*Verifies the real-time aggregations show zero values, and the grid handles zero-stock gracefully without division-by-zero errors.*

**Step-by-Step Actions:**
1. Press `Ctrl+2` to switch to **Inventory**, then click **Stock Dashboard** in the menu panel.
2. View the **5 Summary KPI Cards** at the top of the dashboard.
3. Inspect the product grid column headers and rows.

**Expected Output:**
- [ ] **Total Products Card:** Displays **20** (curated reference catalog is active, colored in Blue `#2980B9`).
- [ ] **Total Stock Value Card:** Displays **₱0.00** (clean factory state, colored in Green `#27AE60`).
- [ ] **Low / Out of Stock Card:** Displays **20** (all items are currently at 0, which is below min threshold, colored in Orange `#E67E22`).
- [ ] **Near-Expiry Batches Card:** Displays **0** (no batches received yet, colored in Yellow-Orange `#F39C12`).
- [ ] **Critical Stockout Risk Card:** Displays **0** (no velocity data recorded yet, colored in Red `#E74C3C`).
- [ ] **Product Grid Columns:** Retail Price displays seeded catalog prices (e.g. `₱1,450.00` for Urea 46-0-0), but **Avg Cost** and **FIFO Cost** both display **₱0.00** for all rows (no Division by Zero crashes).
- [ ] **Row Highlighting:** Every single row has a soft red background highlight (`#FDEDEC`) indicating an "Out of Stock" status alert.

*Status Check:*
- **Stock Dashboard landing page title:** ___________________________
- **Complete Fertilizer 14-14-14 initial Avg Cost / FIFO Cost:** ___________________________

---

## Part 3: Vendor Product Catalog Setup (PUR-16)

### Test 3.1: Manual Catalog Associations
*Seeds the agreed vendor catalog pricing relationships. Unit cost starts at ₱0.00 (read-only) and will only write-back upon receiving.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` to open **Purchasing**, then click **Vendor Product Catalog**.
2. From the left panel vendor list, select **AgriChem Supplies** (Vendor ID 1).
3. Click the **Add Product to Catalog** button.
4. In the product search popup, type `Urea` and select **Urea 46-0-0** (Sku: `FERT-002`).
5. Observe the agreed unit cost field is removed/disabled in the catalog view (cost updates automatically via GR). Click **Add Product**.
6. Repeat the process to add a second product to **AgriChem Supplies**:
   - Click **Add Product to Catalog**, search `Complete`, select **Complete Fertilizer 14-14-14** (Sku: `FERT-001`), and click **Add Product**.
7. Now select **FarmFresh Seeds Corp.** (Vendor ID 2) on the left vendor panel.
8. Click **Add Product to Catalog**, search `Rice`, select **Hybrid Rice RC222** (Sku: `SEED-001`), and click **Add Product**.

**Expected Output:**
- [ ] Selecting **AgriChem Supplies** displays exactly **2** catalog items: `Urea 46-0-0` and `Complete Fertilizer 14-14-14`.
- [ ] Both products show **Agreed Cost: ₱0.00** (or `LastUnitCost: ₱0.00`).
- [ ] Selecting **FarmFresh Seeds Corp.** displays exactly **1** catalog item: `Hybrid Rice RC222` with cost **₱0.00**.
- [ ] Navigate away (e.g. click Stock Dashboard) and return. Verify the catalog associations persist.

*Status Check:*
- **AgriChem Supplies catalog product list count:** _________ (Expected: 2)
- **FarmFresh Seeds catalog product list count:** _________ (Expected: 1)

---

## Part 4: Purchase Order Validation & Creation

### Test 4.1: Validation Guards on Expected Delivery Date & Empty Product Lines
*Verifies that invalid Expected Delivery dates and incomplete line items are blocked before database commit.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` (Purchasing), click **Purchase Orders**, then click the **New PO** button.
2. In the PO Editor:
   - Select **Vendor:** `AgriChem Supplies`.
   - Leave **Expected Delivery Date** blank.
   - Leave **Notes** empty.
3. Click the **+ Add Line** button.
4. Click **Submit PO** (top right of editor).
   - **Observe:** The UI blocks submit. Look at the status bar at the top of the editor.
   - **Expected Status Message:** *"Expected delivery date is required."*
5. Now, select a past expected delivery date (Yesterday) using the calendar selector.
6. Click **Submit PO**.
   - **Observe:** The UI blocks submit.
   - **Expected Status Message:** *"Expected delivery date cannot be in the past."*
7. Set the **Expected Delivery Date** to **Tomorrow's Date** (e.g., if today is May 29, 2026, set to `05/30/2026`).
8. The line item we added is currently blank. Do **not** select any product.
9. Click **Submit PO**.
   - **Observe:** A Wpf Toast Notification popup flashes.
   - **Expected Toast Error Message:** *"Cannot save: Lines 1 have no product selected (Product ID is 0)."*

*Status Check:*
- **Expected Delivery Date requirement block verified:** [ ] Yes / [ ] No
- **Past Date restriction block verified:** [ ] Yes / [ ] No
- **Empty line item block toast message verified:** [ ] Yes / [ ] No

---

### Test 4.2: Successful Purchase Order Submission
*Verifies the catalog-filtered product ComboBox works and manually overridden unit costs calculate totals.*

**Step-by-Step Actions:**
1. In the open PO Editor with **AgriChem Supplies** selected:
2. Focus on Line 1. Click the **Product** cell dropdown.
   - **Observe:** Open the dropdown and inspect the items list. Only the **2** products previously associated with AgriChem supplies (`Urea 46-0-0` and `Complete Fertilizer 14-14-14`) should be visible. Seeded products belonging to other vendors (like Hybrid Rice RC222) must **not** appear.
3. Select **Urea 46-0-0** from the dropdown.
   - **Observe:** The column auto-populates **ProductName** as `Urea 46-0-0`, and **Unit Cost** auto-fills as `₱0.00` (Agreed catalog cost).
4. Edit the line fields:
   - Double-click the **Qty** cell, type **10**, and press Enter.
   - Double-click the **Unit Cost** cell, type **1500.00**, and press Enter.
   - **Observe:** The **Line Total** column auto-calculates to **₱15,000.00**, and the top-right **Running Total** updates to **Running Total: ₱15,000.00**.
5. Click **+ Add Line** to create Line 2.
6. Click the Product dropdown on Line 2, select **Complete Fertilizer 14-14-14**.
   - **Observe:** Unit Cost defaults to `₱0.00`.
7. Edit Line 2 fields:
   - Set **Qty:** **10**
   - Set **Unit Cost:** **1200.00**
   - **Observe:** Line 2 Total shows **₱12,000.00**. Running Total shows **₱27,000.00**.
8. In the **Notes** textbox, type: `"Initial bulk replenishment for rainy season cropping."`
9. Click the **Submit PO** button.

**Expected Output:**
- [ ] The editor panel closes.
- [ ] The main grid refreshes. A new PO row appears with a unique Order Number (e.g. `PO-2026-0001`), Status showing **Submitted** (highlighted in soft blue `#EBF5FB`), Vendor **AgriChem Supplies**, and Total Amount **₱27,000.00**.
- [ ] **Catalog Cost Check:** Press `Ctrl+1` (Purchasing), click **Vendor Product Catalog**. Select **AgriChem Supplies**.
  - Verify that the catalog agreed unit costs for both `Urea 46-0-0` and `Complete Fertilizer 14-14-14` are **still ₱0.00** (prices do not write back upon PO submit, only on confirmed goods receipt).

*Status Check:*
- **Generated Purchase Order Number:** __________________
- **Grid status display:** __________________ (Expected: `Submitted`)
- **AgriChem catalog prices post-submit (Urea / Complete):** ₱_________ / ₱_________

---

## Part 5: Goods Receiving & Agreement Cost Write-Backs

### Test 5.1: Validation Guard on Received Quantity Discrepancy Notes
*Verifies that accepting a delivery with a quantity variance forces the user to enter a discrepancy reason.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` (Purchasing), and click **Goods Receiving** in the menu panel.
2. In the **PO Selector** dropdown at the top, select your submitted PO (e.g., `PO-2026-0001 — AgriChem Supplies`).
   - **Observe:** The receiving grid loads both line items with ordered quantity:
     - `Urea 46-0-0`: Qty Ordered = 10 | Qty Received = 10 | Unit Cost = ₱1,500.00
     - `Complete Fertilizer 14-14-14`: Qty Ordered = 10 | Qty Received = 10 | Unit Cost = ₱1,200.00
3. We are going to record a short shipment on Complete Fertilizer:
   - Double-click the **Qty Received** cell on the Complete Fertilizer row, type **9**, and press Enter.
   - **Observe:** The row flag automatically toggles `HasDiscrepancy` to True.
   - Leave the **Discrepancy Notes** cell completely blank.
4. Keep `Urea 46-0-0` received quantity at **10**, but we are going to record an updated unit cost of **1520.00** (supplier invoice price adjustment):
   - Double-click the **Unit Cost** cell on the Urea row, type **1520.00**, and press Enter.
5. Click the **Confirm Receipt** button.

**Expected Output:**
- [ ] The system blocks the confirmation.
- [ ] The status bar displays the validation message:
  *"Discrepancy notes required for: Complete Fertilizer 14-14-14"*

*Status Check:*
- **Discrepancy block validation message verified:** [ ] Yes / [ ] No

---

### Test 5.2: Successful Goods Confirmation and Agreed-Cost Write-back (Option B)
*Completes the Goods Receipt, validating stock batch generation, AP ledgers, and catalog agreed price write-backs.*

**Step-by-Step Actions:**
1. Double-click the **Discrepancy Notes** cell on the Complete Fertilizer row.
2. Type: `"1 bag ruptured during transport; rejected at warehouse dock."` and press Enter.
3. Ensure the received details are exactly:
   - Row 1: `Urea 46-0-0` | Qty Received = **10** | Unit Cost = **1520.00** | Expiry Date = None
   - Row 2: `Complete Fertilizer 14-14-14` | Qty Received = **9** | Unit Cost = **1200.00** | Expiry Date = None | Discrepancy Notes = `"1 bag ruptured during transport; rejected..."`
4. Click **Confirm Receipt**.

**Expected Output:**
- [ ] The receiving grid clears, and the PO Selector returns to empty.
- [ ] The status bar displays: *"Receipt GR-2026-0001 confirmed — PO marked Received."* (or similar GR receipt sequence number).
- [ ] **Catalog Cost Check (Option B):** Navigate to **Vendor Product Catalog**, select **AgriChem Supplies**.
  - **Observe:** `Urea 46-0-0` agreed unit cost has updated to **₱1,520.00** (receipt override wrote back to catalog!).
  - **Observe:** `Complete Fertilizer 14-14-14` agreed unit cost has updated to **₱1,200.00** (receipt confirmed price!).
- [ ] **MariaDB Ledger Verification:** Run the following validation queries:
  ```sql
  SELECT 'Pur_GoodsReceipts' AS TableName, COUNT(*) AS Rows FROM Pur_GoodsReceipts UNION ALL
  SELECT 'Pur_GoodsReceiptLines', COUNT(*) FROM Pur_GoodsReceiptLines UNION ALL
  SELECT 'Pur_AccountsPayable', COUNT(*) FROM Pur_AccountsPayable UNION ALL
  SELECT 'Inv_StockBatches', COUNT(*) FROM Inv_StockBatches UNION ALL
  SELECT 'Inv_StockMovements', COUNT(*) FROM Inv_StockMovements;
  ```
  Verify that:
  - `Pur_GoodsReceipts` count = **1**.
  - `Pur_GoodsReceiptLines` count = **2**.
  - `Pur_AccountsPayable` count = **1** (AP record for the exact received invoice value: 10 × 1520 + 9 × 1200 = ₱26,000.00).
  - `Inv_StockBatches` count = **2** (Batch 1: 10 units of Urea at ₱1,520.00; Batch 2: 9 units of Complete Fertilizer at ₱1,200.00).
  - `Inv_StockMovements` count = **2** (Both show positive quantities, `MovementType = 'Receipt'`).

*Status Check:*
- **AgriChem Urea 46-0-0 Agreed Cost post-receipt:** ₱_________ (Expected: 1,520.00)
- **AgriChem Complete Fertilizer Agreed Cost post-receipt:** ₱_________ (Expected: 1,200.00)
- **Accounts Payable Invoice Balance:** ₱_________ (Expected: 26,000.00)
- **Inv_StockBatches count in DB:** _________ (Expected: 2)

---

## Part 6: Retail Price & Margin Safety Guards (INV-14)

### Test 6.1: Costing Helper Panel & Below-Cost Price Block
*Verifies the system reads the live FIFO cost batch and blocks retail price changes that result in negative profitability.*

**Step-by-Step Actions:**
1. Press `Ctrl+2` (Inventory), and click **Product Management** in the menu panel.
2. Observe the **Retail Price** column for **Urea 46-0-0** in the grid.
   - **Observe:** It is already **₱1,824.00**. The system automatically adjusted it during Goods Receiving to maintain profitability against the new ₱1,520.00 FIFO cost.
3. Select **Urea 46-0-0** from the product grid, and click **Edit** (or double-click the row).
4. Focus on the **Retail Price** section of the edit popup.
   - **Observe:** The **FIFO Costing Helper Panel** is displayed. Verify the values shown:
     - `Current FIFO Cost:` **₱1,520.00** (read directly from our newly received stock batch).
     - `Suggested Price (20%):` **₱1,824.00** (auto-calculates 1.20 × 1,520.00).
   - **Observe:** The **Retail Price** textbox natively shows **1824.00**.
5. Attempt to edit the Retail Price to an unprofitable value (below FIFO cost) to verify safety guards:
   - In the **Retail Price** textbox, manually type **1450.00**.
   - **Observe:** The margin preview label immediately updates to show a negative margin and margin percentage (e.g. `Margin: -₱70.00 (-4.6%)`).
6. Click **Save** inside the editor.

**Expected Output:**
- [ ] The save action is **blocked**; the editor does not close, and no database commit occurs.
- [ ] An error message is displayed inside the popup (`EditorError` label):
  *"Retail price (₱1450.00) cannot be less than the current vendor cost (₱1520.00), resulting in negative profitability."* (matching the actual parsed decimal formatting).

*Status Check:*
- **Current FIFO Cost shown in Helper:** ₱_________ (Expected: 1,520.00)
- **Suggested Retail Price shown in Helper:** ₱_________ (Expected: 1,824.00)
- **EditorError string shown on unprofitable save:** __________________________________

---

### Test 6.2: Profitable Price Adjustments & Reason Auditing
*Performs a successful retail price increase, verifying the change reason is recorded in price history logs.*

**Step-by-Step Actions:**
1. While still inside the **Urea 46-0-0** Product Editor:
2. Double-click the **Retail Price** textbox, type **1820.00**, and press Enter.
   - **Observe:** Margin helper updates to: `Margin: ₱300.00 (19.7%)`.
3. In the **Price Change Reason** textbox, type: `"Adjusted markup to match increased supplier raw material costs."`
4. Click the **Save** button.
   - **Observe:** The editor closes successfully, and the main product grid refreshes showing Urea 46-0-0 with updated Retail Price **₱1,820.00**.
5. Select **Urea 46-0-0** on the grid again, and click the **View Price History** button.
   - **Observe:** The price history window opens with a read-only grid. Verify you see **two** recent entries:
     - The **System** entry auto-created during Goods Receiving changing it from ₱1,450 to ₱1,824.
     - Your manual entry changing it from ₱1,824 to ₱1,820 with your reason.

**Expected Output:**
- [ ] The history grid contains **2 rows** for today showing:
  - **Row 1 (Manual):**
    - `Old Price:` **₱1,824.00** (auto-adjusted price).
    - `New Price:` **₱1,820.00** (saved retail price).
    - `Delta (Δ):` **-4.00** (colored in Red indicating a price decrease from the auto-suggested price).
    - `Changed By:` **manager** (your current logged-in user).
    - `Reason:` *"Adjusted markup to match increased supplier raw material costs."*
  - **Row 2 (System):**
    - `Old Price:` **₱1,450.00** (seeded retail price).
    - `New Price:` **₱1,824.00** (system auto-adjusted price).
    - `Delta (Δ):` **+374.00** (colored in Green indicating a price increase).
    - `Changed By:` **System**.
    - `Reason:` *"Auto-adjusted margin to 20% due to new FIFO cost (₱1520.00)"*
- [ ] Close the Price History window.

*Status Check:*
- **Urea grid Retail Price:** ₱_________ (Expected: 1,820.00)
- **Manual Price History Delta (Δ) value:** ₱_________ (Expected: -4.00)
- **Manual Price History Changed By username:** __________________ (Expected: manager)

---

### Test 6.3: No-Op Price Edits
*Verifies that updating a product's non-price metadata does not record a false price history record.*

**Step-by-Step Actions:**
1. Select **Urea 46-0-0** again, and click **Edit**.
2. Do **not** touch the Retail Price textbox (leave it at `1820.00`).
3. Focus on the **Description** textbox, and type: `"Premium agricultural grade granular Urea (46-0-0) high-nitrogen fertilizer."`
4. Click **Save**.
5. Select **Urea 46-0-0** once more, and click **View Price History**.

**Expected Output:**
- [ ] The history window opens showing exactly **1 row** (the record from Test 6.2).
- [ ] No new price change history entries were added for a non-price edit.
- [ ] Close the history window.

*Status Check:*
- **Price History rows count:** _________ (Expected: 1)

---

## Part 7: Multi-Batch POS Checkout & FIFO COGS Verification

### Test 7.1: Multi-Batch Inventory Setup
*Creates a second stock batch at a different unit cost to prepare for multi-batch FIFO sales testing.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` (Purchasing), and click **Purchase Orders**.
2. Click **New PO** and enter details:
   - **Vendor:** `AgriChem Supplies`.
   - **Expected Delivery Date:** Set to **Tomorrow's Date**.
3. Click **+ Add Line**, select **Urea 46-0-0** from the dropdown.
4. Edit the line fields to establish a second batch at a lower cost:
   - **Qty:** **5**
   - **Unit Cost:** **1400.00** (lower cost than Batch 1's ₱1,520.00!).
5. Click **Submit PO**.
6. Navigate to **Goods Receiving**, select this new PO (e.g. `PO-2026-0002`).
7. Leave Received Qty as **5** and Unit Cost as **1400.00**. Click **Confirm Receipt**.

**Expected Output:**
- [ ] Goods receipt is processed successfully.
- [ ] Go to **Inventory → Stock Dashboard**. Locate **Urea 46-0-0** in the grid. Verify cost metrics:
  - **Stock:** **15** (10 from Batch 1 + 5 from Batch 2).
  - **Stock Value:** **₱22,200.00** (= 10 × 1520 + 5 × 1400).
  - **Avg Cost:** **₱1,480.00** (= ₱22,200 / 15).
  - **FIFO Cost:** **₱1,520.00** (correctly points to Batch 1, which is the oldest unconsumed batch!).
- [ ] The row color highlights for Urea 46-0-0 has changed to standard white/light grey (Normal Stock Alert).

*Status Check:*
- **Urea 46-0-0 Total Stock on Dashboard:** _________ (Expected: 15)
- **Urea 46-0-0 Avg Cost:** ₱_________ (Expected: 1,480.00)
- **Urea 46-0-0 FIFO Cost:** ₱_________ (Expected: 1,520.00)

---

### Test 7.2: POS Shopping Cart and Multi-Batch Sales Execution
*Performs a cash checkout for 12 units of Urea, which must exhaust Batch 1 (10 units) and draw the remaining 2 units from Batch 2.*

**Step-by-Step Actions:**
1. Press `Ctrl+3` to open **Point of Sale**, then click **Sales Cart** in the menu panel.
2. In the **Product Search** textbox, type `Urea` and press Enter (or click Search).
   - Verify **Urea 46-0-0** appears in the results with `Available Stock: 15` and `Unit Price: ₱1,820.00`.
3. Click the result row to add **1 unit** of Urea to the cart.
4. Focus on the Cart grid in the middle. Double-click the **Quantity** cell of Urea 46-0-0.
5. Type **12** and press Enter.
   - **Observe:** The POS panel auto-calculates figures:
     - `Subtotal:` **₱21,840.00** (12 × ₱1,820.00)
     - `Vat Amount:` **₱0.00** (Business is Non-VAT registered by default, 0% Output VAT).
     - `Grand Total:` **₱21,840.00**
6. Select **Payment Method:** **Cash** (Click the Cash selector).
7. In the **Amount Tendered** textbox, type **22000.00**.
   - **Observe:** The **Change** label updates to: **₱160.00**.
8. Click the **Pay** button.

**Expected Output:**
- [ ] A success popup triggers confirming transaction completion.
- [ ] The POS print preview modal opens displaying **Official Receipt OR-2026-0001** (or matching OR sequence).
- [ ] The OR details show: Sold **12** Urea 46-0-0 @ ₱1,820.00 | Total Sales = **₱21,840.00** | Tendered = **₱22,000.00** | Change = **₱160.00** | BIR compliant Non-VAT legend.
- [ ] Click the **New Transaction** button to clear the receipt and reset the cart.

*Status Check:*
- **Generated POS Official Receipt (OR) Number:** __________________
- **Subtotal / Grand Total:** ₱_________ (Expected: 21,840.00)
- **Change Amount:** ₱_________ (Expected: 160.00)

---

### Test 7.3: MariaDB Multi-Batch FIFO COGS Verification
*Audits the central MariaDB tables to verify the FIFO allocation and accounting ledgers match perfectly (ACC-21 & ACC-22).*

**Step-by-Step Actions:**
1. Open your database command tool and run these exact SQL queries to audit the checkout transaction.

#### Query A: FIFO COGS Batch Allocations (ACC-21)
```sql
SELECT BatchId, QuantityDeducted, UnitCost, Cogs 
FROM Inv_SaleCogs 
WHERE TransactionId = (SELECT MAX(Id) FROM Pos_SalesTransactions)
ORDER BY BatchId;
```
*Expected SQL Output:*
Exactly **2 rows** representing the split-batch drawing:
- Row 1: `BatchId = [Batch 1 ID]` | `QuantityDeducted = 10` | `UnitCost = 1520.0000` | `Cogs = 15200.0000`
- Row 2: `BatchId = [Batch 2 ID]` | `QuantityDeducted = 2` | `UnitCost = 1400.0000` | `Cogs = 2800.0000`
- [ ] Verified split-batch matches exactly.

#### Query B: Single-Revenue Row Deduplication (ACC-22)
```sql
SELECT ProductId, QuantitySold, NetAmount, COGS, GrossProfit 
FROM Acc_RevenueRecords 
WHERE SourceTransactionId = (SELECT MAX(Id) FROM Pos_SalesTransactions);
```
*Expected SQL Output:*
Exactly **1 row** (deduplicated! No duplicate VAT handler rows):
- `QuantitySold = 12`
- `NetAmount = 21840.0000`
- `COGS = 18000.0000` (10 × 1520 + 2 × 1400 = ₱15,200 + ₱2,800)
- `GrossProfit = 3840.0000` (₱21,840.00 - ₱18,000.00)
- [ ] Verified single row matches.

#### Query C: Single COGS Expense Ledger
```sql
SELECT Category, Amount, SourceModule, SourceReferenceId 
FROM Acc_ExpenseRecords 
WHERE SourceReferenceId = (SELECT MAX(Id) FROM Pos_SalesTransactions);
```
*Expected SQL Output:*
Exactly **1 row** with:
- `Category = 'COGS'`
- `Amount = 18000.0000`
- `SourceModule = 'POS'`
- [ ] Verified single expense row.

#### Query D: Remaining Stock Batch Balances
```sql
SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost 
FROM Inv_StockBatches 
WHERE ProductId = 2;
```
*Expected SQL Output:*
Verify that:
- Batch 1 (oldest Urea batch at ₱1,520): `QuantityRemaining = 0`.
- Batch 2 (newest Urea batch at ₱1,400): `QuantityRemaining = 3` (5 received - 2 consumed).
- [ ] Verified inventory remaining balances.

*Status Check:*
- **Inv_SaleCogs Row 1 Qty / COGS:** _________ / ₱_________
- **Inv_SaleCogs Row 2 Qty / COGS:** _________ / ₱_________
- **Acc_RevenueRecords COGS Value:** ₱_________ (Expected: 18,000.00)
- **Acc_RevenueRecords GrossProfit:** ₱_________ (Expected: 3,840.00)
- **Batch 1 (₱1,520) QuantityRemaining:** _________ (Expected: 0)
- **Batch 2 (₱1,400) QuantityRemaining:** _________ (Expected: 3)

---

### Test 7.4: Stock Dashboard Cost Roll-Forward
*Verifies the Stock Dashboard updates immediately and shifts FIFO Cost to the next active batch.*

**Step-by-Step Actions:**
1. Navigate to **Inventory → Stock Dashboard**.
2. Find the row for **Urea 46-0-0** and inspect cost columns.

**Expected Output:**
- [ ] **Stock:** **3** (Batch 2 remaining).
- [ ] **Stock Value:** **₱4,200.00** (3 × 1,400.00).
- [ ] **Avg Cost:** **₱1,400.00** (since only Batch 2 remains).
- [ ] **FIFO Cost:** **₱1,400.00** (correctly rolled forward to Batch 2 now that Batch 1 is exhausted!).
- [ ] The row has a soft orange background alert because Stock (3) is below the minimum threshold (10) (Low Stock Alert).

*Status Check:*
- **Urea 46-0-0 stock post-sale:** _________ (Expected: 3)
- **FIFO Cost post-sale:** ₱_________ (Expected: 1,400.00)

---

## Part 8: Alternative Payment Methods & Financial Reporting

### Test 8.1: Credit Checkout and Credit Account Blocks
*Verifies POS integration with Credit limits and credit-blocked customers.*

**Step-by-Step Actions:**
1. Press `Ctrl+3` (POS), and click **Sales Cart**.
2. Search `Complete` and add **1 bag** of **Complete Fertilizer 14-14-14** to the cart (`Retail Price: ₱1,250.00`).
3. Click the **Credit** payment method button.
   - **Observe:** A customer search panel opens.
4. Select customer **Maria Santos** (Account ID 2) from the list.
   - **Observe:** The UI blocks payment. A red alert message is displayed:
     *"⛔ Customer has outstanding balance of ₱500.00"* (since Maria Santos is blocked by default in seeds).
   - **Observe:** The **Pay** button is disabled.
5. Now, select customer **Juan Dela Cruz** (Account ID 1) from the customer list.
   - **Observe:** The alert clears because Juan Dela Cruz is active with a `₱0.00` outstanding balance.
   - **Observe:** The **Pay** button enables.
6. Click **Pay**.

**Expected Output:**
- [ ] Checkout succeeds. An Official Receipt is generated for the credit sale of ₱1,250.00.
- [ ] **MariaDB Ledger Verification:** Run the query:
  ```sql
  SELECT CustomerName, CurrentBalance, TotalCreditExtended FROM Pos_CreditAccounts WHERE Id = 1;
  ```
  Verify that for Juan Dela Cruz:
  - `CurrentBalance` = **1250.0000**.
  - `TotalCreditExtended` = **1250.0000**.
- [ ] Click **New Transaction** to reset the cart.

*Status Check:*
- **Maria Santos Blocked message verified:** [ ] Yes / [ ] No
- **Juan Dela Cruz outstanding balance post-checkout:** ₱_________ (Expected: 1,250.00)

---

### Test 8.2: GCash & Bank Transfer Automatic Pay Gates
*Verifies GCash and Bank Transfer checkout modes bypass cash tender input and finalize immediately.*

**Step-by-Step Actions:**
1. In POS **Sales Cart**:
2. Add **1 bag** of **Complete Fertilizer 14-14-14** to the cart (`Grand Total: ₱1,250.00`).
3. Click the **GCash** payment method button.
   - **Observe:** The **Amount Tendered** field is disabled and set to the exact grand total **₱1,250.00**.
   - The **Pay** button is enabled immediately.
4. Click **Pay**.
   - **Observe:** GCash sale is committed instantly, generating an OR.
5. Click **New Transaction**.
6. Add another product to the cart, select **Bank** payment method.
   - **Observe:** Bypasses cash tender requirements. The **Pay** button is enabled instantly.
7. Click **Pay**.
   - **Observe:** Bank transfer sale commits successfully.

*Status Check:*
- **GCash automatic pay gate verified:** [ ] Yes / [ ] No
- **Bank Transfer automatic pay gate verified:** [ ] Yes / [ ] No

---

### Test 8.3: Monthly Income Statement Audit
*Verifies that the financial reports aggregate transaction ledgers based on correct per-batch COGS.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **Income Statement**.
2. Select the current month and year period.
3. Observe the generated summary report.

**Expected Output:**
- [ ] **Total Revenue:** Aggregates all checkout transactions completed in Part 7 and Part 8.
- [ ] **Total COGS:** Accurately reflects the sum of the actual FIFO batch costs loaded from `Inv_SaleCogs` (Urea cost of ₱18,000.00 + Complete Fertilizer costs of ₱1,200.00 per unit). No phantom overstatements are present.
- [ ] **Gross Profit Margin:** Correctly computed based on `Revenue - COGS`.

*Status Check:*
- **Income Statement Gross Profit Margin:** _________%

---

### Test 8.4: Developer Tools Schema Harness (DEV)
*Verifies the Developer module can run BIR Schema and Tamper audits.*

**Step-by-Step Actions:**
1. Press `Ctrl+0` to open **Developer Tools** (Debug builds only).
2. Click the **Run VAT Schema Harness** button.
3. Wait for the completion success message popup.
4. Open File Explorer, type `%TEMP%` in the address bar, and sort files by date.

**Expected Output:**
- [ ] A success message box pops up confirming: *"VAT schema harness completed successfully."*
- [ ] Locate the newest report file in `%TEMP%` starting with `vat-ledger-schema-report-`.
- [ ] Open the report file and verify that the schema audit ledger checks return **PASS**.

*Status Check:*
- **Harness execution status popup verified:** [ ] Yes / [ ] No
- **Vat Ledger Report successfully generated in `%TEMP%`:** [ ] Yes / [ ] No

---

## Session Summary Notes

**Overall Testing Verdict:** [ ] PASS / [ ] FAIL

*(Write down any observed deviations, unexpected popup messages, or database inconsistencies discovered during testing below.)*
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
__________________________________________________________________________________________
