---
module: Security, Roles & E2E Capabilities (Phase 2)
source: WPF_Applications
originally-generated: 2026-05-31
last-synced: 2026-05-31
infra-migration: INFRA-23 to INFRA-30 (Pure MariaDB client-server; Activity Rail sidebar; Multi-batch FIFO; Bir VAT)
reset: 2026-05-29 (Database factory-reset baseline applied)
verified: (pending)
verified-by: (pending)
verdict: (pending)
---

# Operator Verification Checklist 2 — Manager Role Untested & Partially Tested Modules

> **Target Role:** Manager (`manager`)
> **Credentials (Post Password Reset):** Username: `manager` | Password: `VistaTest1!` *(Created during Part 0 of Checklist 1)*
> **Focus:** Rigorous E2E testing of the previously untested and partially tested functional modules: **Vendor Directory**, **Expiry Monitor with Expired Write-offs**, **POS Daily Summary**, **Accounts Payable Overdue Filtration**, **Financial Overview Dashboard**, **Sales Summary**, **Tamper Audit Reports**, **VAT Relief Report**, and **BIR VAT Return Filing (Form 2551Q & Form 2550M)**.
>
> **Prerequisite State:** The system is in the state left after running the first checklist. The database contains 3 purchase orders, 2 goods receipts, 3 stock batches, and 6 POS retail transactions. Credit accounts for Juan Dela Cruz and Pedro Reyes carry outstanding balances of ₱1,250.00 each, and are marked blocked.

---

## Part 1: Vendor Directory & AP Management

### Test 1.1: Vendor Creation Validation & Lead-Time Guards
*Verifies that adding a new vendor enforces required field validation, and successful submission correctly populates the database and lists details in the sidebar panel.*

**Step-by-Step Actions:**
1. Focus on the left sidebar. Press `Ctrl+1` to open **Purchasing**, and click **Vendor Directory** in the menu panel.
2. In the toolbar, click **Add Vendor** (only visible when logged in as Manager).
3. Observe that the **Editor Panel** opens at the bottom.
4. Leave all fields blank and click **Save** in the editor.
   - **Observe:** The UI blocks the save and displays a validation error in the editor.
   - **Expected validation message:** *"Name is required."* (or similar name validation).
5. Type `"Southern Agritech"` in the Name field. Leave the Lead Time field blank. Click **Save**.
   - **Observe:** Save is blocked.
   - **Expected validation message:** *"Default lead time must be greater than zero."*
6. Enter `0` or `-5` in the **Lead Time (days)** field. Click **Save**.
   - **Observe:** Save is blocked.
7. Enter a valid new vendor profile:
   - **Name:** `Southern Agritech`
   - **Contact Person:** `Mark Solis`
   - **Lead Time (days):** `4`
   - **Phone:** `09190001111`
   - **Email:** `orders@southernagritech.ph`
   - **Address:** `Kidapawan City, Cotabato`
   - **Notes:** `"Preferred supplier for organic fertilizers and soil conditioners."`
8. Click **Save**.

**Expected Output:**
- [ ] The Editor Panel closes.
- [ ] A new row for **Southern Agritech** appears in the main grid showing `Contact Person: Mark Solis`, `Phone: 09190001111`, and `Lead Time: 4`.
- [ ] Select **Southern Agritech** in the grid.
  - **Observe:** The right-hand **Detail Panel** opens showing `Southern Agritech`, `Total POs: 0`, `Total Spent: ₱0`, and `Avg Lead Time: 0.0d`.
- [ ] Direct MariaDB Audit: Run the following query:
  ```sql
  SELECT Name, ContactPerson, DefaultLeadTimeDays, Phone, Address FROM Pur_Vendors WHERE Name = 'Southern Agritech';
  ```
  Verify that the record is successfully written with `IsDeleted = 0`.

*Status Check:*
- **Validation error string seen on blank submit:** __________________________________ (Expected: "Vendor name is required.")
- **Phone validation error string seen on blank phone:** __________________________________ (Expected: "Phone number is required.")
- **Lead time error string seen on invalid value (0 or -5):** __________________________________ (Expected: "Lead time must be greater than 0 days.")
- **Southern Agritech primary key ID in DB:** _________ (Expected: 4)

---

### Test 1.2: Accounts Payable Filter Controls & Overdue Highlighting
*Tests the filtering system of the AP Ledger by Vendor, status flags (Outstanding, Overdue, Paid, All), and verifies status row highlighting.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` (Purchasing), and click **Accounts Payable** in the menu panel.
2. Observe the main grid:
   - **Observe:** The rows are colored based on status (e.g. Paid invoices have a light green background `#EAFAF1`, unpaid invoices have standard white/light grey, and overdue unpaid invoices have an amber background `#FEF5E4` with a warning icon `⚠ Yes`).
3. Click the status filter buttons in the toolbar and count the rows:
   - Click **Paid** -> Grid lists only the settled invoice `GR-2026-0001` (from AgriChem Supplies).
   - Click **Outstanding** -> Grid lists outstanding vendor bills.
   - Click **Overdue** -> Grid lists outstanding bills past their due date.
   - Click **All** -> Grid lists all invoices.
4. Click the **Vendor** combobox dropdown in the toolbar and select **AgriChem Supplies** (the only vendor currently with invoices in the system).
   - **Observe:** The grid instantly filters to show only the 2 invoices belonging to AgriChem Supplies. If you select **FarmFresh Seeds Corp.**, the grid correctly displays an empty state.
5. Restore the Vendor filter to **All Vendors**.

**Expected Output:**
- [ ] Overdue bills are clearly highlighted in amber with a warning symbol.
- [ ] Paid bills are highlighted in soft green with a checkmark indicator.
- [ ] Status filters (`All`, `Outstanding`, `Overdue`, `Paid`) partition records accurately.
- [ ] Vendor dropdown filters the grid in real-time.

*Status Check:*
- **Total Paid invoices count:** _________ (Expected: 1)
- **Total Outstanding invoices count:** _________ (Expected: 1)
- **Amber-highlighted Overdue rows count:** _________ (Expected: 0, as both invoices are due on June 29, 2026, which is in the future relative to the May 31 system date)

---

## Part 2: Batch Expiry Monitor & Write-Off Actions

### Test 2.1: Capturing Expiry Dates at Goods Receiving
*Creates a purchase order and receives it with specific expiration dates to verify correct registration.*

**Step-by-Step Actions:**
1. Press `Ctrl+1` (Purchasing), and click **Purchase Orders**.
2. Click **New PO** and enter details:
   - **Vendor:** `FarmFresh Seeds Corp.`
   - **Expected Delivery Date:** Tomorrow's Date
3. Click **+ Add Line**, select **Hybrid Rice RC222** from the dropdown.
4. Edit the line fields:
   - **Qty:** **50**
   - **Unit Cost:** **800.00**
5. Click **Submit PO** (PO number e.g. `PO-2026-0004` since PO-0003 already exists in baseline data).
6. Navigate to **Goods Receiving**, select this PO from the dropdown.
7. Double-click the **Expiry Date** cell for the Hybrid Rice row.
   - Select a date exactly **5 days from today** (near-expiry testing).
8. Double-click the **VAT Class** cell in the Hybrid Rice row. Select **Exempt** from the dropdown (seeds are VAT-exempt, ensuring correct tax relief calculations in Part 5).
9. Click **Confirm Receipt**.

**Expected Output:**
- [ ] Goods receipt is completed successfully.
- [ ] Navigate to **Inventory → Stock Dashboard**. Locate **Hybrid Rice RC222**. Verify total stock shows **50**, Avg Cost shows **₱800.00**, and Stock Value shows **₱40,000.00**.
- [ ] **MariaDB Verification:** Run the following detailed queries:
  ```sql
  -- Verify the Goods Receipt Line VAT settings
  SELECT ExpiryDate, VatClassification, VatAmount, VatableSales FROM Pur_GoodsReceiptLines WHERE ProductId = 11 ORDER BY Id DESC LIMIT 1;
  
  -- Verify the Inventory Batch is active
  SELECT QuantityRemaining, UnitCost, ExpiryDate FROM Inv_StockBatches WHERE ProductId = 11 ORDER BY Id DESC LIMIT 1;
  
  -- Verify the Accounting Expense record has correct VAT categories (VatableAmount = 0, VatExemptAmount = 40000)
  SELECT Category, Description, Amount, VatableAmount, VatExemptAmount, ZeroRatedAmount, InputVat, VatTreatment FROM Acc_ExpenseRecords WHERE SourceReferenceId = 4;
  ```

*Status Check:*
- **Generated Goods Receipt (GR) Number:** __________________ (Expected: GR-2026-0003)
- **Recorded Expiry Date in MariaDB:** __________________
- **VatClassification in Pur_GoodsReceiptLines:** _________ (Expected: 1, i.e. Exempt)
- **VatAmount / VatableSales in Goods Receipt Lines:** ₱_________ / ₱_________ (Expected: 0.00 / 40,000.00)
- **VatExemptAmount / InputVat in Acc_ExpenseRecords:** ₱_________ / ₱_________ (Expected: 40,000.00 / 0.00)

---

### Test 2.2: Expiry Monitor Threshold & Urgency Highlighting
*Verifies the Expiry Monitor aggregates expiring batches and shifts urgency indicators dynamically based on threshold days.*

**Step-by-Step Actions:**
1. Press `Ctrl+2` (Inventory), and click **Expiry Monitor** in the menu panel.
2. Inspect the **Near-Expiry** tab grid:
   - **Observe:** The Hybrid Rice RC222 batch is listed showing `Qty Remaining: 50`, `Expiry Date` (matching what you entered), and `Days Remaining: 5`.
   - **Observe:** The row is highlighted in a soft red (`#FDEDEC`) because its expiration is `≤ 7 days` (UrgencyLevel = Red).
3. Focus on the **Near-expiry threshold** input box in the toolbar (currently set to `30` days).
4. Double-click the box and type `3`. Press Enter or click **Refresh**.
   - **Observe:** The Hybrid Rice batch vanishes from the grid (since its 5 days remaining exceeds the 3-day threshold!).
   - **Observe:** The **Near-Expiry Batches** KPI card updates from `1` to `0`.
5. Increase the threshold back to `30` days (using the spinner `▲` buttons or typing). Click **Refresh**.
   - **Observe:** The Hybrid Rice batch reappears with red highlighting.

**Expected Output:**
- [ ] Expiring batches populate in the grid with accurate "Days Remaining" counters.
- [ ] Near-expiry threshold box filters batches dynamically.
- [ ] Row background colors update dynamically based on remaining days:
  - `≤ 7 days` -> Soft Red background (`#FDEDEC`)
  - `8 – 14 days` -> Soft Orange background (`#FEF5E7`)
  - `15 – 30 days` -> Soft Yellow-Orange background (`#FEFCE8`)

*Status Check:*
- **Near-Expiry KPI Card value (Threshold = 30):** _________ (Expected: 1)
- **Near-Expiry KPI Card value (Threshold = 3):** _________ (Expected: 0)
- **Hybrid Rice RC222 Row Highlight Color (Threshold = 30):** __________________ (Expected: Soft Red / #FDEDEC)

---

## Part 3: POS Daily Summary & Transaction History

### Test 3.1: Post-Checkout Daily Summary Statistics & Trends
*Verifies the Daily Summary dashboard aggregates cumulative cashiering metrics after POS sales have occurred.*

**Step-by-Step Actions:**
1. Press `Ctrl+3` (POS), and click **Daily Summary** in the menu panel.
2. In the toolbar, ensure **Daily** mode is active, and select **May 30, 2026** (the date transactions 1 to 5 were checked out). Click **Refresh**.
3. Observe the KPI summary cards:
   - **Observe:** **Total Sales Card** now aggregates today's transactions.
   - **Observe:** **Transactions Card** shows the number of checkouts.
   - **Observe:** **Avg Transaction Card** shows the average value.
4. Inspect the **Payment Method Breakdown** grid:
   - Verify it lists counts and percentages for each method used: `Cash`, `Credit`, `GCash`, `Bank Transfer`.
5. Inspect the **Top 5 Selling Products** grid:
   - Verify **Urea 46-0-0** and **Complete Fertilizer 14-14-14** are listed with correct quantities sold and revenues.
6. Toggle the mode buttons to **Weekly** or **Monthly**.
   - **Observe:** The **Daily Sales Trend** chart appears displaying blue bar trend heights matching transaction revenues.

**Expected Output:**
- [ ] Daily Summary shows correct sales aggregations.
- [ ] Payment breakdown lists accurate counts (Cash: 2, Credit: 2, GCash: 1, Bank: 1).
- [ ] Top products matches actual unit checkouts (Urea: 12, Complete: 4).
- [ ] Trend bars render correctly without graphical errors.

*Status Check:*
- **Total Sales Card value:** ₱_________ (Expected: 26,840.00)
- **Transaction Count Card value:** _________ (Expected: 5 — transactions 1 to 5)
- **Avg Transaction Value:** ₱_________ (Expected: 5,368.00)
- **Cash Sales Total:** ₱_________ (Expected: 21,840.00)
- **Credit Sales Total:** ₱_________ (Expected: 2,500.00)
- **Top Product 1 Revenue (Urea):** ₱_________ (Expected: 21,840.00)

---

### Test 3.2: Transaction History Multi-Filters & Receipt PDF Archival
*Tests searching, filtering, and detail lookups within the Transaction History ledger.*

**Step-by-Step Actions:**
1. Press `Ctrl+3` (POS), and click **Transaction History** in the menu panel.
2. Search and filter records:
   - In the **Search** textbox, type `TX-2026-0001` and press Enter.
     - **Observe:** The grid filters to exactly 1 row.
   - Select **Payment Method:** `Credit` in the filter panel.
     - **Observe:** The grid lists exactly 2 credit transactions (Juan Dela Cruz and Pedro Reyes).
3. Clear all filters.
4. Select the VAT-registered transaction row (`TX-2026-0006`).
   - **Observe:** The right-hand **Details Panel** loads with product line items, payment totals, and VAT breakdowns.
5. Click **View Receipt** in the details panel.
   - **Observe:** The application generates and opens the archived BIR VAT receipt PDF.

**Expected Output:**
- [ ] Multi-filters (Search text, Date, Payment method) partition the transaction history accurately.
- [ ] Details panel displays correct itemization and matching ledger totals.
- [ ] Receipt PDF opens without errors, showing a detailed tax breakdown.

*Status Check:*
- **Credit transactions displayed count:** _________ (Expected: 2)
- **Archived receipt PDF file name pattern:** __________________ (Expected: OR-2026-0006.pdf)

---

## Part 4: Accounting Financial Overview & Sales Summaries

### Test 4.1: Financial Overview KPI Dashboards & Alerts
*Verifies the central Financial Overview aggregates total balance sheets and displays active alerts.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **Financial Overview**.
2. Click **Refresh** to run aggregations.
3. View the **8 KPI Cards** at the top of the dashboard:
   - **Observe:** MTD Revenue, AR/AP Outstanding, Inventory Value, and VAT Payable.
4. Inspect the **What This Means** blue explanation card.
   - **Observe:** The card summarizes the financial status in natural text.
5. View the **Alerts Panel** at the bottom:
   - **Observe:** **Overdue Customer Balances** shows a count of `2` (Juan and Pedro are blocked overdue).
   - **Observe:** **Overdue Supplier Bills** shows a count of `0` (since both AP invoices are not yet due).
   - **Observe:** **Low Stock Products** lists products at/below minimum reorder point.
6. Inspect the **6-Month Revenue Trend** chart (grouped bars showing Revenue vs COGS vs Gross Profit) and **Top 10 Products by Revenue** grid.

**Expected Output:**
- [ ] MTD Revenue aggregates all retail checkout totals.
- [ ] AR Outstanding matches customer credit balances (₱2,500.00).
- [ ] Alerts Panel shows 2 overdue customer balances and 0 overdue supplier bills.
- [ ] Top 10 products grid ranks Urea first and Complete Fertilizer second.

*Status Check:*
- **Financial Overview Today / MTD Revenue:** ₱_________ / ₱_________ (Expected: ₱0.00 / ₱28,090.00)
- **AR Outstanding Card value:** ₱_________ (Expected: 2,500.00)
- **AP Outstanding Card value:** ₱_________ (Expected: 47,000.00, reflecting PO 3 [7,000] and PO 4 [40,000] outstanding)
- **Inventory Value Card value:** ₱_________ (Expected: 53,800.00, reflecting Urea [4,200], Complete [9,600], and Hybrid Rice [40,000])
- **Overdue Customer Balances Alert count:** _________ (Expected: 2)
- **Low Stock Products Alert count:** _________ (Expected: 19, as Hybrid Rice RC222 has 50 bags in stock which exceeds its threshold of 5)

---

### Test 4.2: Accounting Sales Summary by Payment Method
*Tests that the Sales Summary tab correctly breaks down transaction net amounts and sales dates.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **Sales Summary**.
2. Select period **Monthly** (May 2026) and click **Refresh**.
3. View the **Payment Method Breakdown** grid:
   - Verify columns: Payment Method, Transactions, Gross Amount, Net Amount, % of Total.
   - **Observe:** The **Credit** row is highlighted in light yellow (`#FFFDF0`), indicating accounts receivable risk.
4. View the **Daily Breakdown** grid:
   - Verify it shows daily sales figures, discounts, and return amounts.

**Expected Output:**
- [ ] Sales Summary displays correct gross vs net totals.
- [ ] Credit payment row is highlighted in yellow.
- [ ] Daily Breakdown grid populates with records for May 30 and May 31.

*Status Check:*
- **Total Net Sales in Sales Summary:** ₱_________ (Expected: 28,090.00 — includes VAT sale)
- **Credit row Net Amount / % of Total:** ₱_________ / _________% (Expected: 2,500.00 / ~8.9%)

---

### Test 4.3: Tamper Audit Integrity Reports & Exports
*Verifies the Tamper Audit page handles empty states gracefully and export commands function.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **Tamper Audit Report**.
2. Click **Apply Filter** for the default date range.
   - **Observe:** An empty state banner displays: *"No tamper incidents detected in the selected period."*
   - **Observe:** The **Export to CSV** and **Export to PDF** buttons in the toolbar are disabled.
3. To test the logging system, execute the following SQL query to simulate a receipt tampering event (inserting a mismatching signature/hash log):
   ```sql
   INSERT INTO Acc_TamperAuditLog (DetectedAt, ReceiptId, ReceiptNumber, TamperKind, DetectedByService, ExpectedValue, ActualValue, MachineName, OperatingUser, CreatedAt, CreatedBy)
   VALUES (UTC_TIMESTAMP(), 1, 'OR-2026-0001', 'Signature mismatch on POS official receipt archival block.', 'ReceiptIntegrityService', 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'DESKTOP-VISTA', 'VISTA\\Administrator', UTC_TIMESTAMP(), 'TestHarness');
   ```
4. Return to the VISTA screen and click **Apply Filter**.
   - **Observe:** The empty state banner disappears, and a detailed incident row appears in the grid.
   - **Observe:** The **Export to CSV** and **Export to PDF** buttons in the toolbar are now enabled.
5. Click **Export to CSV**. Verify the save dialog prompt.

**Expected Output:**
- [ ] Empty state is handled gracefully with clear banners and disabled action buttons.
- [ ] Simulating an incident populates the grid instantly with matching details (OR-2026-0001, expected/actual hashes, Severity: Critical).
- [ ] Export buttons enable and trigger file exports.

*Status Check:*
- **Tamper incident row displayed grid count:** _________ (Expected: 1)
- **Disabled button status on empty verified:** [ ] Yes / [ ] No

---

## Part 5: VAT Relief & BIR VAT Return Filing

### Test 5.1: Monthly Three-Bucket VAT Relief Summary
*Verifies that the VAT Relief dashboard aggregates and partitions vatable and exempt transactions.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **VAT Relief Report**.
2. In the toolbar, set Year to **2026** and Month to **5** (May). Click **Refresh**.
3. View the **Net VAT Payable** card:
   - **Observe:** Displays a net negative amount highlighted in Green indicating a tax credit.
4. Inspect the **Sales** bucket details:
   - `Vatable Sales:` **₱1,116.07** (reflecting VAT-inclusive transaction OR-2026-0006).
   - `VAT-Exempt Sales:` **₱26,840.00** (reflecting transactions 1 to 5 recorded while Non-VAT).
   - `Output VAT:` **₱133.93**.
5. Inspect the **Purchases** bucket details:
   - `Vatable Purchases:` **₱29,464.27** (reflecting goods receipts under PO 1 and PO 3).
   - `VAT-Exempt Purchases:` **₱40,000.00** (reflecting goods receipt under PO 4 of 50 bags of Hybrid Rice RC222).
   - `Input VAT:` **₱3,535.71** (total input tax captured).
6. Inspect the **Trailing 12 Months** table.
   - **Observe:** Verify the row for May 2026 lists these exact figures.

**Expected Output:**
- [ ] Net VAT Payable is **-₱3,401.78** (excess Input VAT tax credit to be carried forward).
- [ ] Sales and Purchases buckets accurately partition vatable, exempt, and zero-rated values.
- [ ] Trailing 12 Months table lists the May 2026 summary row.

*Status Check:*
- **Report Net VAT Payable value:** -₱_________ (Expected: 3,401.78)
- **Output VAT total:** ₱_________ (Expected: 133.93)
- **Input VAT total:** ₱_________ (Expected: 3,535.71)
- **Total Gross Purchases in Report (Vatable + Exempt):** ₱_________ (Expected: 73,000.00, reflecting PO 1 [26,000] + PO 3 [7,000] + PO 4 [40,000])
- **VAT-Exempt Purchases total:** ₱_________ (Expected: 40,000.00)

---

### Test 5.2: BIR Form 2551Q Filing (Quarterly Percentage Tax)
*Generates and files the Quarterly Percentage Tax return required for Non-VAT registered periods.*

**Step-by-Step Actions:**
1. Press `Ctrl+4` (Accounting), and click **VAT Return (BIR)** in the menu panel.
2. Select Year: **2026** and Period: **2** (Quarter 2 covering April, May, June).
3. Click the **2551Q (3% Tax)** Form radio button.
4. Click the **Generate** button.
5. Inspect the KPI cards and main grid:
   - **Observe:** **Vatable Sales** aggregates all Non-VAT transactions from May 30 (transactions 1 to 5) = **₱26,840.00**.
   - **Observe:** **VAT Payable** shows **₱805.20** (3% percentage tax on ₱26,840.00).
   - **Observe:** **Filing Status** displays *"Generated — not yet filed with BIR"*.
   - **Observe:** The **Source Document Lines** grid lists transactions 1 to 5 as source rows.
6. Click **File with BIR**.
   - **Observe:** The **Filing Status** card updates and displays *"Filed with BIR"*.
   - **Observe:** The status message states: *"Return filed successfully with BIR."*
7. **MariaDB Ledger Verification:** Run this query:
  ```sql
  SELECT FormType, Year, Period, TaxPayable, FilingStatus FROM Acc_VatReturns WHERE FormType = 'Form2551Q';
  ```

**Expected Output:**
- [ ] Generating Form 2551Q calculates net sales of ₱26,840.00 and 3% tax of ₱805.20.
- [ ] Clicking **File with BIR** completes without error and updates the status to Filed.
- [ ] Database shows `FilingStatus = 1` (Filed) and `TaxPayable = 805.2000` for Form 2551Q.

*Status Check:*
- **Generated Percentage Tax Due (Form 2551Q):** ₱_________ (Expected: 805.20)
- **Filing Status Display post-filing:** __________________________________
- **FilingStatus value in MariaDB:** _________ (Expected: 1)

---

### Test 5.3: BIR Form 2550M Filing (Monthly VAT Return)
*Simulates transitioning to VAT Registered mode, generating a monthly VAT return, carrying over tax credits, and filing.*

**Step-by-Step Actions:**
1. Focus on the **VAT Return (BIR)** screen (still open).
2. Select Year: **2026** and Period: **5** (May).
3. Click the **2550M (Monthly VAT)** Form radio button.
4. Click the **Generate** button.
5. Observe the KPI summary cards:
   - **Observe:** **Vatable Sales** shows **₱1,116.07** (transaction OR-2026-0006).
   - **Observe:** **Output VAT** shows **₱133.93**.
   - **Observe:** **Input VAT** shows **₱3,535.71**.
   - **Observe:** **VAT Payable** displays **₱3,401.78 credit (carry forward)**.
   - **Observe:** **Filing Status** shows *"Generated — not yet filed with BIR"*.
   - **Observe:** The **Source Document Lines** grid lists transaction OR-2026-0006 and the three goods receipts as contributing source lines.
6. Click **File with BIR**.
   - **Observe:** Filing completes and Filing Status updates to *"Filed with BIR"*.
7. Click the **Export PDF** button in the toolbar.
   - **Observe:** A dialog prompts, exporting the BIR tax return statement.
8. **MariaDB Ledger Verification:** Run this query:
  ```sql
  SELECT FormType, Year, Period, TotalOutputVat, TotalInputVat, TaxPayable, FilingStatus FROM Acc_VatReturns WHERE FormType = 'Form2550M';
  ```

**Expected Output:**
- [ ] Monthly VAT return generates Output VAT of ₱133.93, Input VAT of ₱3,535.71, and tax credit of -₱3,401.78.
- [ ] Filing return transitions status to Filed.
- [ ] Export PDF exports a complete statement.
- [ ] MariaDB matches return details perfectly (`TaxPayable = -3401.7800`, `FilingStatus = 1`).

*Status Check:*
- **Form 2550M Vatable Sales:** ₱_________ (Expected: 1,116.07)
- **Form 2550M Output VAT / Input VAT:** ₱_________ / ₱_________ (Expected: 133.93 / 3,535.71)
- **Form 2550M VAT Payable:** -₱_________ (Expected: 3,401.78 credit)
- **TaxPayable value in MariaDB:** -_________ (Expected: -3401.7800)

---

## Session Summary Notes

**Overall Phase 2 Verification Verdict:** [ ] PASS / [ ] FAIL

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
