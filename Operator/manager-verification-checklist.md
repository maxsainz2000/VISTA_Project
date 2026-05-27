---
module: Security & Roles
source: Dashboard-Feature-Analysis-2026-05-27.md
originally-generated: 2026-05-27
last-synced: 2026-05-27
reset: 2026-05-27
verified: (pending)
verified-by: (pending)
verdict: (pending)
---

# Operator Verification Checklist — Manager Role & Stock Dashboard

> **Target Role:** Manager (`manager`)
> **Credentials:** Username: `manager` | Password: `Vista2026!` *(DA6 first-login — must set a new password before the app opens)*
> **Focus:** Full operational authorization and E2E verification of the Stock Dashboard (Manager landing page).
>
> **Factory-reset baseline:** The database has been wiped. On first launch the app applies all migrations and seeds 20 products, 3 vendors, and 3 credit accounts. No stock batches, no transactions, and no purchase orders exist yet.
>
> **How to use:** Follow each test block in sequence. Perform the actions in the "What to do" section, verify they match the "What you should see" section, and tick the checkbox when verified. Use the notes line next to each item to write down observed details.

### Key File Locations
| Component | Path |
|:---|:---|
| Stock Dashboard View (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Inventory\StockDashboardView.xaml` |
| Stock Dashboard View (Code-Behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Inventory\StockDashboardView.xaml.vb` |
| Stock Dashboard ViewModel | `WPF_Applications\MerchSys\src\MerchSys.Inventory\ViewModels\StockDashboardViewModel.vb` |
| Stock Dashboard Service | `WPF_Applications\MerchSys\src\MerchSys.Inventory\Services\StockDashboardService.vb` |
| Main Sidebar View Model (Nav) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` |
| SQLite Database File | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| Central MariaDB | `localhost:3306/merchsys_central` (XAMPP) — query via `C:\xampp\mysql\bin\mysql.exe -u root merchsys_central` |
| Sync Probe Interval | 30 s (`SyncSettings.ProbeIntervalSeconds`) — wait ~45–60 s after any write before querying MariaDB |

---

## Part 0: First-Login Password Setup (DA6)

### Test 0.1: Mandatory Password Change on First Login
*Verifies the DA6 first-login flow fires when `LastPasswordChangeAt IS NULL`.*

**What to do:**
1. Launch the application (press **F5** in Visual Studio or run `dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App`).
2. Enter Username: `manager` and Password: `Vista2026!` and click **Login**.
3. Observe — the app should NOT open the main window yet.

**What you should see:**
- [ ] The login screen shows a "Please set a new password before continuing." message (or a dedicated password-change form).
- [ ] Attempting to set the same password (`Vista2026!`) returns a validation error: *"New password must differ from the current password."*
- [ ] Setting a new password (e.g., `VistaTest1!`) succeeds and the main window opens.

*Status Check:*
- **Prompt text shown:** _______________
- **New password set to:** _______________

---

## Part 1: Header Display & Role Verification

### Test 1.1: Shell Header Display
*Verifies that the shell header accurately identifies the authenticated user and their active role.*

**What to do:**
1. After completing the DA6 password setup above, look at the upper-right or sidebar header area of the main window.
2. Verify the username and role display.

**What you should see:**
- [ ] The header shows `manager` (bold or prominent text) representing the authenticated username.
- [ ] Below or next to the username, the active role is displayed as `Manager` (often styled as muted text).

*Status Check:*
- **Username display text:** _______________
- **Role display text:** _______________

---

## Part 2: Authorization & Sidebar Navigation Verification

### Test 2.1: Full Operational Sidebar Access
*Verifies that the Manager role has full operational privileges across all system areas, as specified in the access control matrix.*

**What to do:**
1. While logged in as `manager`, inspect the sidebar navigation panel.
2. Go through each group and check that all operational, CRUD, and setup views are present.

**What you should see:**
- [ ] **Point of Sale** group contains:
  - `Sales Cart` (operational POS terminal)
  - `Credit Management` (customer credit/utang lifecycle)
  - `Transaction History` (searchable historical list of sales and returns)
  - `Daily Summary` (daily sales overview)
  - `VAT Settings` (BIR tax configuration)
- [ ] **Purchasing** group contains:
  - `Purchase Orders` (PO lifecycle management)
  - `Goods Receiving` (receiving shipments against POs)
  - `Vendor Directory` (CRUD management of vendors)
  - `Accounts Payable` (AP tracking and payouts)
  - `Reorder Suggestions` (inventory reorder planning engine)
  - `Vendor Product Catalog` (vendor-product relationships and catalog pricing — PUR-16)
- [ ] **Inventory** group contains:
  - `Stock Dashboard` (real-time stock status list)
  - `Product Management` (CRUD control for all product details)
  - `Expiry Monitor` (batch expiry tracking)
  - `Shrinkage` (shrinkage recording and reporting)
- [ ] **Accounting** group contains:
  - `Financial Overview` (P&L KPIs and Vat tile)
  - `Income Statement` (monthly financial reporting summaries)
  - `Sales Summary` (sales distribution reviews)
  - `Tamper Audit Report` (financial transaction tamper detection log)
  - `VAT Relief Report` (BIR VAT relief/exemption report — read-only, visible to both roles)
  - `VAT Return (BIR)` (VAT computation and BIR tax form submission)
- [ ] **Developer Tools** (visible only in **Debug** builds) contains:
  - `Run VAT Schema Harness`

*Status Check:*
- **Total sidebar items visible:** _______________ (expected: 22 — POS×5 + Purchasing×6 + Inventory×4 + Accounting×6 + Dev Tools×1)
- **Confirm presence of VAT Settings & VAT Return:** _______________
- **Confirm presence of VAT Relief Report (Accounting):** _______________

---

## Part 3: Stock Dashboard (Manager Dashboard) E2E Verification

### Test 3.1: Default Landing Page
*Verifies the landing page configuration for the Manager role.*

**What to do:**
1. Log out of the application (click **Log Out** at the bottom of the sidebar) and log back in as `manager` using the password you set in Test 0.1.
2. Observe which screen is automatically shown in the central content area on startup.

**What you should see:**
- [ ] The app automatically loads the **Stock Dashboard** view (`StockDashboardView`) as the landing page.
- [ ] The dashboard content area is visible and begins loading products immediately.

*Status Check:*
- **Landing screen name displayed on header:** _______________

---

### Test 3.2: 5 Summary KPI Cards Verification
*Verifies the real-time aggregation metrics at the top of the Stock Dashboard.*

> **Factory-reset baseline:** No stock batches exist. All 20 seeded products have 0 units. Total Stock Value is ₱0.

**What to do:**
1. Observe the five card panels aligned horizontally at the top of the dashboard.
2. Check the color coding, label, and values displayed on each.

**What you should see:**
- [ ] **Total Products Card:** Shows `20` (20 active SKUs from seed data — styled in **Blue** `#2980B9`).
- [ ] **Total Stock Value Card:** Shows `₱0.00` (no batches received yet — styled in **Green** `#27AE60`, prefixed with `₱`).
- [ ] **Low / Out of Stock Card:** Shows `20` (all products are at 0 stock, which is below every product's minimum threshold — styled in **Orange** `#E67E22`).
- [ ] **Near-Expiry Batches Card:** Shows `0` (no batches exist — styled in **Yellow-Orange** `#F39C12`).
- [ ] **Critical Stockout Risk Card:** Shows `0` (no velocity data → no stockout prediction possible — styled in **Red** `#E74C3C`).

*Status Check:*
- **Total Products count:** _______________
- **Total Stock Value:** _______________
- **Low / Out of Stock count:** _______________
- **Near-Expiry Batches count:** _______________
- **Critical Stockout count:** _______________

---

### Test 3.3: Live Filtering & Text Search
*Verifies the filtering responsiveness and text matching behavior on the product grid.*

> **Factory-reset baseline:** All 20 seeded products exist. All have 0 stock. No "Urea 50kg" variant yet (that was a manually added product).

**What to do:**
1. Click the **Category** ComboBox and select a specific category (e.g., `Fertilizers`).
2. Click the **Status** ComboBox and select a status (e.g., `Out`).
3. Type a text pattern in the **Search** TextBox (e.g., `Urea`).
4. Clear the text search and return filters to `All` to check recovery.

**What you should see:**
- [ ] **Category filter:** Only products belonging to the selected category remain in the grid. The dropdown lists only distinct active categories present in the DB (4 categories: Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds).
- [ ] **Status filter:** Grid updates instantly to display only matching stock states (`Out`, `Low`, or `Normal`). With no stock, all 20 products show as `Out`.
- [ ] **Text search:** Results filter as you type (case-insensitive) based on Product Name or Category.
- [ ] **Combined filtering:** Selecting `Fertilizers` + `Out` + `Urea` narrows down the list to matching rows (only `Urea 46-0-0`, 1 row).
- [ ] Clearing the search text and resetting dropdowns to `All` immediately restores the full 20-product inventory.

*Status Check:*
- **Selected Category test:** Fertilizers → Rows remaining: _______________ (expected: 5)
- **Selected Status test:** Out → Rows remaining: _______________ (expected: 20)
- **Search string `Urea` typed:** Rows remaining: _______________ (expected: 1 — only `Urea 46-0-0`)

---

### Test 3.4: DataGrid Row Status Color Coding
*Verifies that the grid rows apply appropriate visual alerts based on critical metrics.*

> **Factory-reset baseline:** All products have 0 stock. Every row should be styled **Out of Stock** (soft red). No orange (low stock above 0), no green (no stock at all), no yellow (no batches = no expiry).

**What to do:**
1. Review the background highlights of the rows in the product grid.
2. Identify products in various stock and expiration states and confirm color styling.

**What you should see:**
- [ ] **Out of Stock:** All 20 products have 0 stock → every row shows a soft **Red** background (`#FDEDEC`).
- [ ] **Low Stock:** Not applicable until stock is received.
- [ ] **Near-Expiry / Expired:** Not applicable until expiry-flagged batches are received.
- [ ] **Normal:** Not applicable until stock is received above the minimum threshold.
- [ ] **Selection Highlight:** Clicking any row highlights it in soft **Blue** (`#D6EAF8`) with a matching blue border (`#2980B9`).

*Status Check:*
- **Red Row verified (Out of Stock):** _______________
- **Selection Highlight verified:** _______________

---

### Test 3.5: Product Detail Panel Drill-Down (Batches & Movements)
*Verifies the master-detail interaction and data retrieval for specific product selection.*

> **Factory-reset baseline:** No stock batches or movements exist. The batch and movements grids will be empty.

**What to do:**
1. Click on any product row in the main grid (e.g., `Complete Fertilizer 14-14-14`).
2. Observe the bottom of the screen to verify that the **Product Detail Panel** appears.
3. Review the two sub-grids inside the detail panel.

**What you should see:**
- [ ] The detail panel slides up or becomes visible (`IsDetailVisible = True`).
- [ ] The title shows: *"Product Detail — [Product Name] ([Category])"* in blue.
- [ ] **Stock Batches Grid:** Loads with **0 rows** (no batches received yet). The grid is visible but empty.
- [ ] **Recent Movements Grid:** Loads with **0 rows** (no movements recorded yet). The grid is visible but empty.

*Status Check:*
- **Selected product name:** _______________
- **Number of batches loaded in detail:** _______________ (expected: 0)
- **Number of movements shown:** _______________ (expected: 0)

---

### Test 3.6: Grid Column Sorting
*Verifies the DataGrid sorting engine.*

**What to do:**
1. Click the column headers of the Product Grid: **Product Name**, **Stock**, **Retail Price**, **Avg Cost**, **FIFO Cost**, **Stock Value**, **Days to Stockout**.
2. Click once to sort ascending, and click a second time to sort descending.

**What you should see:**
- [ ] The grid re-orders rows correctly based on the selected column.
- [ ] **Product Name Sort:** Alphabetical order ascending/descending.
- [ ] **Stock Sort:** All rows are 0 — order is stable (no reordering needed but no crash).
- [ ] **Retail Price / Avg Cost / FIFO Cost columns are present** (replacing the legacy single "Price" column — INV-15). Sorting each does not crash even when most values are `₱0.00` (no stock yet).
- [ ] **Days to Stockout Sort:** All values show `—` (no velocity data). No crash or exception when sorting a column of all-null display values.

*Status Check:*
- **Ascending Product Name sort verified:** _______________
- **Days to Stockout sort (all `—`) verified without crash:** _______________

---

### Test 3.7: Refresh Operations (Manual and 60-Second Auto)
*Verifies real-time background sync and UI updates.*

**What to do:**
1. Look at the toolbar row and note the **Last Refreshed** text (e.g., `Refreshed 11:07:05`).
2. Click the **Refresh** button.
3. Keep the dashboard open for 90 seconds without touching the application. Watch the timestamp.

**What you should see:**
- [ ] Clicking the manual **Refresh** button triggers `LoadDataAsync`. The UI displays `Loading...` briefly, then updates the timestamp immediately.
- [ ] After 60 seconds of idle time, the background timer tick executes.
- [ ] The grid fetches the latest database status and silently refreshes the data without shifting scroll focus.
- [ ] The "Last Refreshed" timestamp updates to show the new successful execution time.

*Status Check:*
- **Original timestamp:** _______________
- **Timestamp after manual refresh:** _______________
- **Timestamp after 60s idle (Auto-refresh):** _______________

---

## Part 4: Manager Write Authorization & Integration Checks

### Test 4.1: Database Write Capability (CRUD)
*Verifies that the Manager has real write permissions to perform actions (unlike the restricted Owner role).*

> **Prerequisite:** Stock must exist before a sale can be completed. Before running this test, go to **Purchasing → Purchase Orders**, create a PO for at least 10 units of any product, then go to **Purchasing → Goods Receiving** and receive those units. Confirm the stock count updates on the Stock Dashboard. Then proceed with the steps below.

**What to do:**
1. Go to the **Sales Cart** in the POS section.
2. Add at least 1 unit of the product you just received to the cart.
3. Finish the checkout with a cash sale and confirm it creates an Official Receipt (OR).
4. Go back to the **Stock Dashboard** and check the stock count of the product sold.

**What you should see:**
- [ ] The sale is processed without authorization errors.
- [ ] A success notification is shown, and the transaction is committed to the database.
- [ ] The product's stock count on the dashboard decrements by the sold quantity.
- [ ] Querying `Sync_Journal` in DB Browser for SQLite shows new rows representing `Pos_SalesTransactions`, `Inv_StockMovements`, and `Pos_OfficialReceipts` (verifies INFRA-13 journal creation).

*Status Check:*
- **OR number generated:** _______________
- **Product stock decremented successfully:** _______________
- **Sync Journal entries verified in SQLite:** _______________

**MariaDB sync sub-check (wait ~45 s after the sale, then run):**
```sql
-- Run inside: mysql -u root merchsys_central
SELECT 'Pos_SalesTransactions' AS t, COUNT(*) AS c FROM Pos_SalesTransactions UNION ALL
SELECT 'Pos_SalesTransactionLines', COUNT(*) FROM Pos_SalesTransactionLines UNION ALL
SELECT 'Pos_OfficialReceipts', COUNT(*) FROM Pos_OfficialReceipts UNION ALL
SELECT 'Pos_ReceiptIntegrity', COUNT(*) FROM Pos_ReceiptIntegrity UNION ALL
SELECT 'Inv_StockMovements', COUNT(*) FROM Inv_StockMovements UNION ALL
SELECT 'Inv_SaleCogs', COUNT(*) FROM Inv_SaleCogs UNION ALL
SELECT 'Acc_RevenueRecords', COUNT(*) FROM Acc_RevenueRecords UNION ALL
SELECT 'Acc_ExpenseRecords', COUNT(*) FROM Acc_ExpenseRecords;
```
- [ ] `Pos_SalesTransactions` count increased by **1** (matches local SQLite count).
- [ ] `Pos_SalesTransactionLines` count increased by **1**.
- [ ] `Pos_OfficialReceipts` count increased by **1**, with `IntegrityHash` populated.
- [ ] `Pos_ReceiptIntegrity` count increased by **1** (hash chain row).
- [ ] `Inv_StockMovements` count increased by **1** (`MovementType=Sale`, negative `Quantity`).
- [ ] `Inv_SaleCogs` count increased by **1** (only one batch was consumed in a 1-unit sale).
- [ ] `Acc_RevenueRecords` count increased by exactly **1** (no duplicate — ACC-22 invariant).
- [ ] `Acc_ExpenseRecords` count increased by exactly **1** (`Category='COGS'`).
- [ ] `Inv_StockBatches` row for the consumed batch shows updated `QuantityRemaining` (the row is **updated**, not duplicated; `LastWriteWins` policy).

*Status Check:*
- **MariaDB row counts match local SQLite within 60 s:** _______________
- **Inv_SaleCogs row visible on central DB (INFRA-22 working):** _______________

---

### Test 4.2: Developer Tools & VAT Schema Harness
*Verifies that the Manager can execute debugging features.*

**What to do:**
1. In the sidebar menu, click **Developer Tools** (only visible in Debug builds).
2. Click **Run VAT Schema Harness**.
3. Wait for the completion popup.

**What you should see:**
- [ ] A message box pops up confirming the harness executed successfully.
- [ ] Go to `%TEMP%` in File Explorer and look for the newest file starting with `vat-ledger-schema-report-`.
- [ ] Open the report file and verify all 4 schema audit checks show a **PASS** status.

*Status Check:*
- **Message box text shown:** _______________
- **Report generated in `%TEMP%`:** _______________
- **Confirm 4/4 checks passed:** _______________

---

## Part 5: PUR-16 — Vendor Product Catalog & PO Auto-configuration

### Test 5.1: Vendor Product Catalog Navigation
*Verifies the Vendor Product Catalog view is accessible and loads the seeded vendors.*

**What to do:**
1. While logged in as `manager`, click **Vendor Product Catalog** in the Purchasing sidebar group.

**What you should see:**
- [ ] A master-detail view loads: vendor list on the left, catalog rows for the selected vendor on the right.
- [ ] The 3 seeded vendors appear in the left panel.
- [ ] Selecting any vendor shows an empty catalog grid (factory-reset baseline — no products added yet).

*Status Check:*
- **Vendor list loaded (count):** _______________ (expected: 3)
- **Initial catalog row count for any vendor:** _______________ (expected: 0)

---

### Test 5.2: Add Product to Vendor Catalog
*Verifies a Manager can add a product-vendor link with a remembered unit cost.*

**What to do:**
1. Select any vendor on the left panel.
2. Click **Add Product**.
3. In the product search dialog, search for a product (e.g., `Urea`) and select `Urea 46-0-0`.
4. Enter a **Unit Cost** (e.g., `1200`) and click **Save**.

**What you should see:**
- [ ] The product search dialog opens and returns matching inventory products.
- [ ] After saving, the vendor's catalog shows one row with the product name and unit cost entered.
- [ ] The entry persists after navigating away and returning to the Vendor Product Catalog.

*Status Check:*
- **Vendor selected:** _______________
- **Product added:** _______________
- **Unit cost entered:** _______________
- **Row persists after navigation:** _______________

---

### Test 5.3: PO Editor — Vendor-Filtered Product ComboBox & UnitCost Auto-fill
*Verifies the PO line product dropdown is filtered to the selected vendor's catalog and auto-populates UnitCost.*

**Prerequisite:** Test 5.2 complete — at least one product in a vendor's catalog.

**What to do:**
1. Navigate to **Purchasing → Purchase Orders** and create a new PO.
2. Select the same vendor used in Test 5.2.
3. Click **Add Line**.
4. Click the **Product** dropdown cell on the new line and open the dropdown.

**What you should see:**
- [ ] The product dropdown lists only the products in that vendor's catalog (not all 20 products).
- [ ] Selecting the catalog product auto-fills **ProductName** in that column.
- [ ] **UnitCost** auto-fills with the `LastUnitCost` recorded in the catalog (e.g., `1200.00` from Test 5.2).
- [ ] Manually overriding the UnitCost to a different value is allowed.

*Status Check:*
- **Dropdown item count matches vendor catalog size:** _______________
- **UnitCost auto-filled value:** _______________
- **Manual UnitCost override accepted:** _______________

---

### Test 5.4: Pre-Save Validator Blocks Empty Product Line (ProductId = 0 Fix)
*Verifies the save is blocked when any PO line has no product selected.*

**What to do:**
1. Create or edit a PO with any vendor.
2. Click **Add Line** — **do not select a product** from the dropdown.
3. Attempt to **Save** / **Submit** the PO.

**What you should see:**
- [ ] The save is **blocked** — the PO does not commit to the database.
- [ ] A `Notification.Wpf` error toast appears identifying the offending line (e.g., *"Line 1: Product must be selected before saving."* or equivalent wording).
- [ ] After selecting a valid product on the empty line, the PO saves successfully.

*Status Check:*
- **Save blocked with empty product line:** _______________
- **Error notification text shown:** _______________
- **PO saves successfully after product is selected:** _______________

---

### Test 5.5: UnitCost Write-Back to Catalog on Save
*Verifies an overridden UnitCost on a PO line updates the vendor catalog's LastUnitCost.*

**What to do:**
1. Create a PO for the vendor from Test 5.2. Add a line for the same catalog product.
2. Override the **UnitCost** to a different value (e.g., `1350`).
3. Save / Submit the PO.
4. Navigate back to **Vendor Product Catalog**, select the same vendor and product.

**What you should see:**
- [ ] The vendor catalog's **LastUnitCost** for that product now shows the overridden value (`1350`), not the original value.

*Status Check:*
- **Original LastUnitCost:** _______________
- **Overridden UnitCost saved on PO:** _______________
- **Catalog LastUnitCost updated to new value:** _______________

---

## Part 6: INV-14 — Product Retail Price Change History

### Test 6.1: Price Change Logged on Product Edit
*Verifies that changing RetailPrice in Product Management writes an append-only history row.*

**What to do:**
1. Navigate to **Inventory → Product Management**.
2. Select any product (e.g., `Urea 46-0-0`).
3. Note the current **Retail Price**.
4. Change the Retail Price to a new value (e.g., if it is `₱1,500.00`, change it to `₱1,600.00`).
5. Optionally fill in the **Price Change Reason** field (e.g., `Supplier cost increase`).
6. Click **Save**.

**What you should see:**
- [ ] The product saves successfully with the new Retail Price.
- [ ] A success notification is shown.

*Status Check:*
- **Product selected:** _______________
- **Old price noted:** _______________
- **New price entered:** _______________
- **Reason entered (optional):** _______________

---

### Test 6.2: Price History Popup Reflects the Change
*Verifies the read-only price history popup shows the entry recorded in Test 6.1.*

**What to do:**
1. With the same product selected in Product Management, click the **View Price History** button (in the toolbar or product detail area).
2. Review the history popup that opens.

**What you should see:**
- [ ] A popup window opens with a read-only data grid.
- [ ] At least one row is displayed showing: `ChangedAt` (recent timestamp), `OldPrice` → `NewPrice` (matching Test 6.1 values), `Δ` (delta, colored **green** for increase, **red** for decrease), `ChangedBy` (the logged-in username), and `Reason` (if entered in Test 6.1).
- [ ] No edit, add, or delete controls exist anywhere in the popup.

*Status Check:*
- **History row count:** _______________ (expected: ≥ 1)
- **OldPrice displayed:** _______________
- **NewPrice displayed:** _______________
- **Delta color (green for price increase):** _______________
- **ChangedBy shows current username:** _______________

---

### Test 6.3: No-Op Edit Does Not Create a History Row
*Verifies that saving a product without changing RetailPrice does not add a spurious history entry.*

**What to do:**
1. Select any product in Product Management. Note the current row count in its price history (click **View Price History** to check, then close the popup).
2. Edit any field **other than** Retail Price (e.g., the product's Notes or Description).
3. Click **Save**.
4. Click **View Price History** again and count the rows.

**What you should see:**
- [ ] The product saves successfully.
- [ ] The history popup row count is **unchanged** — no new row was created for a price-unchanged save.

*Status Check:*
- **Row count before no-op edit:** _______________
- **Row count after no-op edit:** _______________ (expected: same as before)

---

## Part 7: INV-15, ACC-22, ACC-21 — Per-Batch FIFO COGS Accuracy & Dashboard Cost Visibility

> **Goal:** Verify the three 2026-05-27 bug-fix plans end-to-end:
> - **INV-15** — Stock Dashboard shows Retail Price / Avg Cost / FIFO Cost columns
> - **ACC-22** — Exactly one `Acc_RevenueRecords` row per `(SourceTransactionId, ProductId)` (no duplicate from legacy + VAT handler race)
> - **ACC-21** — `Acc_RevenueRecords.COGS` reflects the **actual per-batch** FIFO cost summed across consumed batches, recorded in the new `Inv_SaleCogs` ledger.
>
> **Prerequisite for all tests in Part 7:** Tests 5.1–5.3 (Vendor Catalog seeding) completed. Tests assume a fresh test product (recommended: `Urea 46-0-0`, ProductId visible in Product Management).

### Test 7.1: Stock Dashboard Renders Three Cost Columns (INV-15)
*Verifies that the legacy single "Price" column has been replaced by Retail Price + Avg Cost + FIFO Cost columns and that tooltips render.*

**What to do:**
1. Navigate to **Inventory → Stock Dashboard**.
2. Inspect the product grid headers.
3. Hover the cursor over each new column header to read the tooltip.

**What you should see:**
- [ ] Three contiguous currency columns are visible: **Retail Price**, **Avg Cost**, **FIFO Cost** (in that order).
- [ ] Hovering **Retail Price** shows tooltip: *"Selling price set in Product Management."*
- [ ] Hovering **Avg Cost** shows tooltip: *"Weighted-average purchase cost across remaining non-expired batches."*
- [ ] Hovering **FIFO Cost** shows tooltip: *"Unit cost of the oldest batch — the cost the next sale will draw from."*
- [ ] All three columns are right-aligned and formatted with `₱` and two decimals.
- [ ] On the factory-reset baseline (no stock), Avg Cost and FIFO Cost display `₱0.00` for every product (no division-by-zero error).

*Status Check:*
- **Three cost columns visible:** _______________
- **Tooltips display correctly:** _______________
- **Zero-stock rows render ₱0.00 (no crash):** _______________

---

### Test 7.2: Multi-Vendor Receipt Populates Avg Cost & FIFO Cost (INV-15)
*Verifies the weighted-average and FIFO-oldest computations after receiving 3 batches at different unit costs.*

**What to do:**
1. Navigate to **Purchasing → Purchase Orders** and create **three** POs for the same product (e.g., `Urea 46-0-0`), one per vendor, 10 units each:
   - Vendor 1 — UnitCost `₱1,200.00`
   - Vendor 2 — UnitCost `₱1,100.00`
   - Vendor 3 — UnitCost `₱1,000.00`
2. Go to **Purchasing → Goods Receiving** and receive all three POs in the order above (so the ₱1,200 batch is the FIFO-oldest).
3. Navigate back to **Inventory → Stock Dashboard** and find that product's row.

**What you should see:**
- [ ] **Stock** = `30` (10 + 10 + 10).
- [ ] **Stock Value** = `₱33,000.00` (= 10×1200 + 10×1100 + 10×1000).
- [ ] **Retail Price** = the value set in Product Management (unchanged by receiving).
- [ ] **Avg Cost** = `₱1,100.00` exactly (₱33,000 / 30).
- [ ] **FIFO Cost** = `₱1,200.00` exactly (oldest batch — Vendor 1's PO received first).

*Status Check:*
- **Stock count:** _______________ (expected: 30)
- **Stock Value:** _______________ (expected: ₱33,000.00)
- **Avg Cost:** _______________ (expected: ₱1,100.00)
- **FIFO Cost:** _______________ (expected: ₱1,200.00)

**MariaDB sync sub-check (wait ~45 s after the 3rd Goods Receiving commit):**
```sql
SELECT 'Pur_PurchaseOrders' AS t, COUNT(*) AS c FROM Pur_PurchaseOrders UNION ALL
SELECT 'Pur_PurchaseOrderLines', COUNT(*) FROM Pur_PurchaseOrderLines UNION ALL
SELECT 'Pur_GoodsReceipts', COUNT(*) FROM Pur_GoodsReceipts UNION ALL
SELECT 'Pur_GoodsReceiptLines', COUNT(*) FROM Pur_GoodsReceiptLines UNION ALL
SELECT 'Pur_AccountsPayable', COUNT(*) FROM Pur_AccountsPayable UNION ALL
SELECT 'Inv_StockBatches', COUNT(*) FROM Inv_StockBatches UNION ALL
SELECT 'Inv_StockMovements', COUNT(*) FROM Inv_StockMovements;
```
- [ ] `Pur_PurchaseOrders` count = **3** (one per vendor PO).
- [ ] `Pur_PurchaseOrderLines` count = **3** (one line per PO).
- [ ] `Pur_GoodsReceipts` count = **3**.
- [ ] `Pur_GoodsReceiptLines` count = **3**.
- [ ] `Pur_AccountsPayable` count = **3** (one AP row per received PO).
- [ ] `Inv_StockBatches` count = **3** (one batch per received PO) with UnitCost values `1000.00`, `1100.00`, `1200.00`.
- [ ] `Inv_StockMovements` count = **3** (`MovementType=Receipt`, positive `Quantity=10`).
- [ ] **Note:** `Pur_VendorProducts` is **not** in any sync map at this writing — its central-DB row count will stay at `0` regardless of how many catalog entries you added in Tests 5.1–5.5. Flag this as a known gap (candidate INFRA-23) if it matters to you.

*Status Check:*
- **MariaDB row counts after receiving:** _______________
- **Inv_StockBatches UnitCost values match (1000/1100/1200):** _______________

---

### Test 7.3: Multi-Batch Sale Records Accurate COGS & Single Revenue Row (ACC-21 + ACC-22)
*Reproduces the 2026-05-27 bug-report scenario end-to-end.*

**Prerequisite:** Test 7.2 complete — 30 units across three batches at ₱1,200 / ₱1,100 / ₱1,000.

**What to do:**
1. Navigate to **Inventory → Product Management** and set the **Retail Price** of the test product to `₱1,100.00` (so revenue = average cost = break-even).
2. Navigate to **Point of Sale → Sales Cart** and sell **all 30 units** of the test product in a **single transaction** (cash sale). Note the OR number printed.
3. Open SQLite (via DB Browser or the `sqlite3` CLI noted in `CLAUDE.md`) on `%LOCALAPPDATA%\MerchSys\merchsys.db` and run the queries listed below.

**SQL queries to run:**
```sql
-- ACC-21: per-batch COGS breakdown should have exactly 3 rows
SELECT BatchId, QuantityDeducted, UnitCost, Cogs
FROM Inv_SaleCogs
WHERE TransactionId = (SELECT Id FROM Pos_SalesTransactions ORDER BY Id DESC LIMIT 1)
  AND ProductId = (SELECT ProductId FROM Inv_SaleCogs ORDER BY Id DESC LIMIT 1)
ORDER BY BatchId;

-- ACC-22: exactly one revenue row per (Tx, Product)
SELECT SourceTransactionId, ProductId, COUNT(*) AS rows
FROM Acc_RevenueRecords
GROUP BY SourceTransactionId, ProductId
HAVING COUNT(*) > 1;

-- ACC-21: COGS on the revenue row should equal SUM of Inv_SaleCogs.Cogs
SELECT QuantitySold, NetAmount, COGS, GrossProfit
FROM Acc_RevenueRecords
WHERE SourceTransactionId = (SELECT Id FROM Pos_SalesTransactions ORDER BY Id DESC LIMIT 1);

-- ACC-22: exactly one COGS expense row per transaction/product
SELECT Category, COUNT(*) AS rows
FROM Acc_ExpenseRecords
WHERE Category = 'COGS' AND SourceReferenceId = (SELECT Id FROM Pos_SalesTransactions ORDER BY Id DESC LIMIT 1)
GROUP BY Category;
```

**What you should see:**
- [ ] `Inv_SaleCogs` query returns **exactly 3 rows** with `QuantityDeducted=10` and `Cogs` values `12000.00`, `11000.00`, `10000.00` (one per batch).
- [ ] **Duplicate-row query returns 0 rows** (i.e. no `(SourceTransactionId, ProductId)` group has `COUNT(*) > 1`) — confirms ACC-22's deduplication.
- [ ] `Acc_RevenueRecords` row for this sale shows: `QuantitySold = 30`, `NetAmount = 33000.00`, `COGS = 33000.00`, `GrossProfit = 0.00` (break-even, **NOT** the pre-fix `COGS = 36000` / `GrossProfit = -3000`).
- [ ] `Acc_ExpenseRecords` COGS row count = `1` for this transaction (no duplicate COGS expense).

*Status Check:*
- **Inv_SaleCogs row count:** _______________ (expected: 3)
- **Inv_SaleCogs Cogs values:** _______________ (expected: 12000 / 11000 / 10000)
- **Duplicate RevenueRecord rows:** _______________ (expected: 0)
- **Acc_RevenueRecords.COGS:** _______________ (expected: 33000.00)
- **Acc_RevenueRecords.GrossProfit:** _______________ (expected: 0.00)
- **COGS ExpenseRecord row count for this Tx:** _______________ (expected: 1)

**MariaDB sync sub-check (wait ~45 s after the multi-batch sale — this is the INFRA-22 critical test):**
```sql
-- Inv_SaleCogs central rows must mirror local — INFRA-22's whole reason for existing
SELECT BatchId, QuantityDeducted, UnitCost, Cogs FROM Inv_SaleCogs
WHERE TransactionId = (SELECT MAX(SourceTransactionId) FROM Acc_RevenueRecords)
ORDER BY BatchId;

-- Single revenue row (ACC-22 invariant) on central DB
SELECT SourceTransactionId, ProductId, QuantitySold, NetAmount, COGS, GrossProfit
FROM Acc_RevenueRecords
WHERE SourceTransactionId = (SELECT MAX(SourceTransactionId) FROM Acc_RevenueRecords);

-- All three local batches show updated QuantityRemaining=0 on central DB
SELECT Id, QuantityReceived, QuantityRemaining, UnitCost FROM Inv_StockBatches
ORDER BY ReceiptDate;
```
- [ ] **Central `Inv_SaleCogs` returns 3 rows** with `Cogs` values `12000.0000`, `11000.0000`, `10000.0000` — **identical to local SQLite** (INFRA-22 sync works).
- [ ] Central `Acc_RevenueRecords` row for this Tx shows `COGS = 33000.00`, `GrossProfit = 0.00`, `QuantitySold = 30`.
- [ ] **Only one row** exists in central `Acc_RevenueRecords` for this `(SourceTransactionId, ProductId)` pair (no duplicate writer race on central side either).
- [ ] All three `Inv_StockBatches` rows on central DB show `QuantityRemaining = 0` (UPDATE pushed via sync, not a new INSERT).
- [ ] Sanity: `SELECT COUNT(*) FROM Inv_SaleCogs;` on central = `SELECT COUNT(*) FROM Inv_SaleCogs;` on local SQLite.

*Status Check:*
- **Central Inv_SaleCogs row count matches local:** _______________
- **Inv_SaleCogs Cogs values mirror local exactly:** _______________
- **Central Acc_RevenueRecords.COGS:** _______________ (expected: 33000.00)
- **Central Inv_StockBatches all show QuantityRemaining=0:** _______________

---

### Test 7.4: Stock Dashboard Cost Columns After Stock Depletion (INV-15 edge case)
*Verifies that the FIFO Cost column rolls forward as the oldest batch is depleted, and Avg Cost re-weights when only partial batches remain.*

**What to do:**
1. After Test 7.3 (all 30 units sold), navigate back to **Inventory → Stock Dashboard**.
2. Locate the same test product's row.
3. Receive a **new** PO of 5 units at `₱950` for that product.
4. Return to the Stock Dashboard and re-check the cost columns for that product.

**What you should see:**
- [ ] Immediately after Test 7.3 (stock=0): both **Avg Cost** and **FIFO Cost** display `₱0.00` (no non-expired stock; division-by-zero guard intact).
- [ ] After receiving 5 units at ₱950: **Stock** = `5`, **Stock Value** = `₱4,750.00`, **Avg Cost** = `₱950.00`, **FIFO Cost** = `₱950.00` (only one batch).

*Status Check:*
- **Cost columns post-depletion:** _______________ (expected: ₱0.00 / ₱0.00)
- **Cost columns post-new-receipt:** _______________ (expected: ₱950.00 / ₱950.00)

---

### Test 7.5: Income Statement Reflects Accurate COGS (ACC-21 downstream)
*Verifies the accounting reports consume the corrected COGS.*

**Prerequisite:** Test 7.3 complete — the break-even sale is committed.

**What to do:**
1. Navigate to **Accounting → Income Statement**.
2. Select the current month period.
3. Inspect the Gross Profit / COGS lines.

**What you should see:**
- [ ] Reported **COGS** for the period includes ₱33,000 (matches the `Acc_RevenueRecords.COGS` from Test 7.3 — **not** the phantom ₱36,000).
- [ ] Reported **Gross Profit** for that single transaction = `₱0.00` (no phantom ₱3,000 loss).
- [ ] No "operating at a loss" plain-language warning is shown solely on account of the test transaction.

*Status Check:*
- **COGS shown:** _______________ (expected: includes ₱33,000)
- **Gross Profit for the sale:** _______________ (expected: ₱0.00, no phantom loss)

---

## Part 8: MariaDB Sync — Comprehensive Audit (run last, after all Manager tests above)

> **Goal:** After completing Tests 4.1 through 7.5, the central `merchsys_central` database should mirror the local SQLite for every synced table. This part is a full reconciliation pass — if any row count diverges, sync has a bug.
>
> **Wait:** at least 90 s after the last Manager-side write before running this section. Sync probe runs on a 30 s cadence; a 90 s wait covers one missed cycle plus a retry.

### Test 8.1: Synced-Table Row Count Reconciliation

**What to do:**
1. Note the local SQLite row count for each synced table using `sqlite3 $env:LOCALAPPDATA\MerchSys\merchsys.db ".tables"` then per-table `SELECT COUNT(*) FROM <table>;`.
2. Run the equivalent counts against MariaDB:
```sql
-- Run inside: mysql -u root merchsys_central
SELECT 'Acc_ExpenseRecords'   AS t, COUNT(*) AS c FROM Acc_ExpenseRecords   UNION ALL
SELECT 'Acc_FinancialPeriods',     COUNT(*) FROM Acc_FinancialPeriods       UNION ALL
SELECT 'Acc_FinancialSnapshots',   COUNT(*) FROM Acc_FinancialSnapshots     UNION ALL
SELECT 'Acc_RevenueRecords',       COUNT(*) FROM Acc_RevenueRecords         UNION ALL
SELECT 'Inv_ProductCategories',    COUNT(*) FROM Inv_ProductCategories      UNION ALL
SELECT 'Inv_Products',             COUNT(*) FROM Inv_Products               UNION ALL
SELECT 'Inv_SaleCogs',             COUNT(*) FROM Inv_SaleCogs               UNION ALL
SELECT 'Inv_ShrinkageRecords',     COUNT(*) FROM Inv_ShrinkageRecords       UNION ALL
SELECT 'Inv_StockAlertConfigs',    COUNT(*) FROM Inv_StockAlertConfigs      UNION ALL
SELECT 'Inv_StockAuditRecords',    COUNT(*) FROM Inv_StockAuditRecords      UNION ALL
SELECT 'Inv_StockBatches',         COUNT(*) FROM Inv_StockBatches           UNION ALL
SELECT 'Inv_StockMovements',       COUNT(*) FROM Inv_StockMovements         UNION ALL
SELECT 'Pos_CreditAccounts',       COUNT(*) FROM Pos_CreditAccounts         UNION ALL
SELECT 'Pos_CreditPayments',       COUNT(*) FROM Pos_CreditPayments         UNION ALL
SELECT 'Pos_OfficialReceipts',     COUNT(*) FROM Pos_OfficialReceipts       UNION ALL
SELECT 'Pos_ReceiptIntegrity',     COUNT(*) FROM Pos_ReceiptIntegrity       UNION ALL
SELECT 'Pos_SalesReturns',         COUNT(*) FROM Pos_SalesReturns           UNION ALL
SELECT 'Pos_SalesTransactionLines',COUNT(*) FROM Pos_SalesTransactionLines  UNION ALL
SELECT 'Pos_SalesTransactions',    COUNT(*) FROM Pos_SalesTransactions      UNION ALL
SELECT 'Pur_AccountsPayable',      COUNT(*) FROM Pur_AccountsPayable        UNION ALL
SELECT 'Pur_GoodsReceiptLines',    COUNT(*) FROM Pur_GoodsReceiptLines      UNION ALL
SELECT 'Pur_GoodsReceipts',        COUNT(*) FROM Pur_GoodsReceipts          UNION ALL
SELECT 'Pur_PriceChangeAlerts',    COUNT(*) FROM Pur_PriceChangeAlerts      UNION ALL
SELECT 'Pur_PurchaseOrderLines',   COUNT(*) FROM Pur_PurchaseOrderLines     UNION ALL
SELECT 'Pur_PurchaseOrders',       COUNT(*) FROM Pur_PurchaseOrders         UNION ALL
SELECT 'Pur_ReorderConfigs',       COUNT(*) FROM Pur_ReorderConfigs         UNION ALL
SELECT 'Pur_ReorderSuggestions',   COUNT(*) FROM Pur_ReorderSuggestions     UNION ALL
SELECT 'Pur_Vendors',              COUNT(*) FROM Pur_Vendors;
```
3. Fill out the table below with both counts.

**Expected baseline after the full Manager protocol (Tests 4.1 → 7.5 executed once):**

| Table | Local SQLite | Central MariaDB | Notes |
|---|---|---|---|
| `Pur_Vendors` | 3 (seeded) | **0** | Seeded data is `<NoSync>` — never replicates |
| `Inv_ProductCategories` | 4 (seeded) | **0** | Seed-only, `<NoSync>` |
| `Inv_Products` | 20 (seeded) | **0** | Seed-only, `<NoSync>` |
| `Pos_CreditAccounts` | 3 (seeded) | **0** | Seed-only, `<NoSync>` |
| `Pur_PurchaseOrders` | 4 (1 in T4.1 + 3 in T7.2) | **4** | Equal |
| `Pur_PurchaseOrderLines` | 4 | **4** | Equal |
| `Pur_GoodsReceipts` | 4 | **4** | Equal |
| `Pur_GoodsReceiptLines` | 4 | **4** | Equal |
| `Pur_AccountsPayable` | 4 | **4** | Equal |
| `Pos_SalesTransactions` | 2 (T4.1 + T7.3) | **2** | Equal |
| `Pos_SalesTransactionLines` | 2 | **2** | Equal |
| `Pos_OfficialReceipts` | 2 | **2** | Equal |
| `Pos_ReceiptIntegrity` | 2 | **2** | Equal |
| `Inv_StockBatches` | 4 (1 in T4.1 + 3 in T7.2) | **4** | Equal; multiple UPDATEs pushed (QuantityRemaining decreases) |
| `Inv_StockMovements` | 6 (4 Receipt + 2 Sale) | **6** | Equal |
| `Inv_SaleCogs` | 4 (1 in T4.1 + 3 in T7.3) | **4** | **INFRA-22 critical — must equal local** |
| `Acc_RevenueRecords` | 2 | **2** | ACC-22 — exactly one row per `(Tx, Product)` |
| `Acc_ExpenseRecords` | 2 | **2** | One COGS row per sale |
| `Pos_CreditPayments` | 0 | 0 | Untouched in this protocol |
| `Pos_SalesReturns` | 0 | 0 | Untouched |
| `Pur_PriceChangeAlerts` | 0 | 0 | Untouched |
| `Pur_ReorderConfigs` / `Pur_ReorderSuggestions` | 0 | 0 | Untouched |
| `Inv_ShrinkageRecords` / `Inv_StockAuditRecords` / `Inv_StockAlertConfigs` | 0 | 0 | Untouched |
| `Acc_FinancialPeriods` / `Acc_FinancialSnapshots` | 0 | 0 | Period-close workflow not exercised |

**What you should see:**
- [ ] Every "Equal" row above shows the same value on both sides.
- [ ] All seeded reference tables (`Pur_Vendors`, `Inv_Products`, `Inv_ProductCategories`, `Pos_CreditAccounts`) are **0** on central — confirms `<NoSync>` policy.
- [ ] `Inv_SaleCogs` on central = `Inv_SaleCogs` on local = `4`. If central is `0` or lower than local, INFRA-22 is **broken**.
- [ ] **Note on `Pur_VendorProducts` (PUR-16):** not currently in any `*SyncMap`, so central count stays at `0` regardless of what you added in Tests 5.1–5.5. Document this in Session Notes as a known gap.

*Status Check:*
- **All sync-active tables match between local and central:** _______________
- **Inv_SaleCogs reconciliation (4 / 4):** _______________ / _______________
- **Acc_RevenueRecords reconciliation (2 / 2):** _______________ / _______________
- **Pur_VendorProducts central count (expected 0 — known gap):** _______________

---

### Test 8.2: Sync_Journal Drain Check (local)

**What to do:**
1. Query the local SQLite `Sync_Journal` table for any rows not yet marked synced:
```sql
sqlite3 $env:LOCALAPPDATA\MerchSys\merchsys.db "SELECT TableName, COUNT(*) AS pending FROM Sync_Journal WHERE SyncStatus != 'Synced' GROUP BY TableName ORDER BY TableName;"
```

**What you should see:**
- [ ] **Zero rows returned** — every journal row has been transmitted and acknowledged.
- [ ] If any rows are returned, capture the `TableName` and `pending` count. A pending row for `Inv_SaleCogs` would indicate INFRA-22 sync regression. A pending row for `Pur_VendorProducts` is the expected PUR-16 sync gap.

*Status Check:*
- **Pending journal rows (expected: 0):** _______________
- **Any unexpected non-zero rows (record table name + count):** _______________

---

## Session Notes

**Overall Verdict:** (pending)

*(Record any bugs found, unexpected behavior, or deviations from expected values here.)*
