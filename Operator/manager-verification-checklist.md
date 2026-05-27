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
- **Total sidebar items visible:** _______________ (expected: 21 — POS×5 + Purchasing×5 + Inventory×4 + Accounting×6 + Dev Tools×1)
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
1. Click the column headers of the Product Grid: **Product Name**, **Stock**, **Price**, **Stock Value**, **Days to Stockout**.
2. Click once to sort ascending, and click a second time to sort descending.

**What you should see:**
- [ ] The grid re-orders rows correctly based on the selected column.
- [ ] **Product Name Sort:** Alphabetical order ascending/descending.
- [ ] **Stock Sort:** All rows are 0 — order is stable (no reordering needed but no crash).
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

## Session Notes

**Overall Verdict:** (pending)

*(Record any bugs found, unexpected behavior, or deviations from expected values here.)*
