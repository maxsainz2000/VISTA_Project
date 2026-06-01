---
module: Security & Roles
source: Dashboard-Feature-Analysis-2026-05-27.md
originally-generated: 2026-05-27
last-synced: 2026-05-28
infra-migration: INFRA-23 to INFRA-30 (2026-05-28) — SQLite/sync layer decommissioned; pure MariaDB client-server architecture; Activity Rail sidebar
reset: 2026-05-27
verified: (pending)
verified-by: (pending)
verdict: (pending)
---

# Operator Verification Checklist — Owner Role & KPI Dashboard

> **Target Role:** Owner (`owner`)
> **Credentials:** Username: `owner` | Password: `Vista2026!` *(DA6 first-login — must set a new password before the app opens)*
> **Focus:** Full read-only dashboard verification, restricted menu access, shared view edit-locks, and transaction-level write rejections.
>
> **Factory-reset baseline:** The database has been wiped. On first launch the app applies all migrations and seeds 20 products, 3 vendors, and 3 credit accounts. No stock batches, no transactions, and no purchase orders exist yet. Both accounts start with password `Vista2026!` and require DA6 first-login setup.
>
> **How to use:** Follow each test block in sequence. Perform the actions in the "What to do" section, verify they match the "What you should see" section, and tick the checkbox when verified. Use the notes line next to each item to write down observed details.

### Key File Locations
| Component | Path |
|:---|:---|
| Owner Dashboard View (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\OwnerDashboardView.xaml` |
| Owner Dashboard View (Code-Behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\OwnerDashboardView.xaml.vb` |
| Owner Dashboard ViewModel | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\OwnerDashboardViewModel.vb` |
| Activity Rail ViewModel (Nav) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\Shell\ActivityRailViewModel.vb` |
| Main Window ViewModel (Nav) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` |
| Central MariaDB | `localhost:3306/merchsys_central` (XAMPP) — query via `C:\xampp\mysql\bin\mysql.exe -u root merchsys_central` |
| Connection Status Badge | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Shell\ConnectionStatusIndicator.xaml` (INFRA-28) |
| Security Exception | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Exceptions\UnauthorizedWriteException.vb` (or equivalent data layer rule) |

> **Architecture note (INFRA-23–27, 2026-05-28):** The SQLite offline-first database and Sync Layer (`Sync_Journal`, `SyncOrchestrator`, `ISyncableRepository`) have been fully decommissioned. VISTA now writes **directly** to a single central MariaDB 11.4.x instance. All writes are synchronous. There is no propagation delay, and Sync_Journal is gone.

---

## Part 0: First-Login Password Setup (DA6)

### Test 0.1: Mandatory Password Change on First Login
*Verifies the DA6 first-login flow fires when `LastPasswordChangeAt IS NULL`.*

**What to do:**
1. Launch the application (press **F5** in Visual Studio).
2. Enter Username: `owner` and Password: `Vista2026!` and click **Login**.
3. Observe — the app should NOT open the main window yet.

**What you should see:**
- [ ] The login screen shows a "Please set a new password before continuing." message (or a dedicated password-change form).
- [ ] Attempting to set the same password (`Vista2026!`) returns a validation error: *"New password must differ from the current password."*
- [ ] Setting a new password (e.g., `VistaOwner1!`) succeeds and the Owner Dashboard opens.

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
- [ ] The header shows `owner` (bold or prominent text) representing the authenticated username.
- [ ] Below or next to the username, the active role is displayed as `Owner` (styled as muted text).

*Status Check:*
- **Username display text:** _______________
- **Role display text:** _______________

---

## Part 2: Authorization & Sidebar Navigation Verification

### Test 2.1: Restricted Activity Rail Access
*Verifies that the Owner role's navigation is restricted exclusively to read-only views via the Master-Detail Activity Rail sidebar (INFRA-30).*

**What to do:**
1. While logged in as `owner`, inspect the left-side navigation layout.
2. Verify the **60px Activity Rail** contains only the module icons permitted for Owner.
3. Click each rail icon and verify the **220px Module Detail Panel** shows only the Owner-allowed sub-views.

**What you should see:**
- [ ] The Activity Rail shows the same 4 module icons (PUR, INV, POS, ACC). The **DEV** icon is **completely absent** for Owner regardless of build configuration.
- [ ] **POS module panel** contains:
  - `Transaction History` (read-only historical search)
  - *HIDDEN:* Sales Cart, Credit Management, Daily Summary, and VAT Settings are completely absent.
- [ ] **Purchasing module panel** contains:
  - `Purchase Orders` (read-only list of POs)
  - `Accounts Payable` (read-only AP ledger)
  - *HIDDEN:* Goods Receiving, Vendor Directory, Reorder Suggestions, and Vendor Product Catalog are completely absent.
- [ ] **Inventory module panel** contains:
  - `Stock Dashboard` (read-only inventory status list)
  - *HIDDEN:* Product Management, Expiry Monitor, and Shrinkage are completely absent.
- [ ] **Accounting module panel** contains:
  - `Financial Overview` (P&L metrics and Vat tile)
  - `Income Statement` (monthly profit/loss reports)
  - `Sales Summary` (sales trends review)
  - `VAT Relief Report` (BIR VAT relief/exemption report — read-only, visible to both roles)
  - *HIDDEN:* Tamper Audit Report and VAT Return (BIR) are completely absent.
- [ ] **Connection Status Badge** is visible at the bottom of the Module Detail Panel showing **Online** (INFRA-28).
- [ ] The **Owner Dashboard** (KPI Overview) is accessible as the default landing page (not via the rail but set as the startup view for Owner).

*Status Check:*
- **Activity Rail icon count visible:** 4 (expected: 4 — DEV icon absent for Owner)
- **Total sub-view items accessible (all rail modules):** 9 (expected: 9 — POS×1 + Purchasing×2 + Inventory×1 + Accounting×4 + Owner Dashboard KPI×1)
- **Confirm absolute absence of "Sales Cart":** Confirmed (absent)
- **Confirm absolute absence of "VAT Return (BIR)":** Confirmed (absent)
- **Confirm presence of "VAT Relief Report" in Accounting panel:** Confirmed (present)
- **Connection Status Badge state:** Online

---

## Part 3: Owner KPI Dashboard E2E Verification

### Test 3.1: Default Landing Page
*Verifies the landing page configuration for the Owner role.*

**What to do:**
1. Log out of the application (click **Log Out** in the sidebar) and log back in as `owner` using the password you set in Test 0.1.
2. Observe which screen is automatically shown in the central content area on startup.

**What you should see:**
- [ ] The app automatically loads the **Owner Dashboard** view (`OwnerDashboardView`) as the landing page (instead of the Stock Dashboard).
- [ ] The dashboard content area is visible and begins loading KPI data asynchronously.

*Status Check:*
- **Landing screen name displayed on header:** KPI Overview (or Owner Dashboard)

---

### Test 3.2: 4 KPI Cards & Plain-Language Interpretations
*Verifies the correctness of the KPI metrics and the plain-language interpretations mandatory under academic plan A5.*

> **Populated database state:** Based on the manager E2E tests, the central database contains populated records. Verify that the owner dashboard correctly aggregates and interprets these live values.

**What to do:**
1. While logged in as `owner`, observe the 2×2 grid of KPI cards: Purchasing, Inventory, Sales, and Accounting.
2. Note the values of the numerical indicators.
3. Verify that each card features a green border box labelled **"WHAT THIS MEANS"** with a plain-language summary.

**What you should see:**
- [ ] **📦 PURCHASING CARD:**
  - *Metrics:* Active Vendors: **4** (including newly added Southern Agritech), Open Purchase Orders: **0**, Pending Deliveries: **0**, Overdue Accounts Payable: **₱0**.
  - *What This Means:* `"No open purchase orders. You have ₱47,000 in accounts payable outstanding, but none are overdue."`
- [ ] **📊 INVENTORY CARD:**
  - *Metrics:* Total SKUs: **20**, Total Stock Value: **₱47,800.00** (₱3,600.00 Complete Fertilizer + ₱4,200.00 Urea + ₱40,000.00 Hybrid Rice), Low-Stock Items: **19** (all seeded products except Hybrid Rice), Expiring Within 30 Days: **1** (Hybrid Rice RC222).
  - *What This Means:* `"19 product(s) are below minimum stock level, and 1 product(s) are expiring within 30 days. Both require attention."`
- [ ] **💰 SALES CARD:**
  - *Metrics:* Today's Revenue: **₱0.00**, This Week's Revenue: **₱1,250.00** (TX-2026-0006 on Sunday 2026-05-31), Transactions Today: **0**, Top Product Today: **—**.
  - *What This Means:* `"This week's revenue is ₱1,250. No sales have been recorded today yet."`
- [ ] **📈 ACCOUNTING CARD:**
  - *Metrics:* Current Period Net Income: **₱0.00** (current month is June 2026; E2E test transactions occurred in May 2026), Customer Credit Outstanding (AR): **₱2,500.00** (Juan Dela Cruz ₱1,250.00 + Pedro Reyes ₱1,250.00), Accounts Payable Outstanding: **₱47,000.00** (FarmFresh Seeds ₱7,000.00 + AgriChem Supplies ₱40,000.00).
  - *What This Means:* `"Business is at break-even this period. Review expenses to improve profitability."`

*Status Check:*
- **Purchasing Interpretation Text:** "No open purchase orders. You have ₱47,000 in accounts payable outstanding, but none are overdue."
- **Inventory Interpretation Text:** "19 product(s) are below minimum stock level, and 1 product(s) are expiring within 30 days. Both require attention."
- **Sales Interpretation Text:** "This week's revenue is ₱1,250. No sales have been recorded today yet."
- **Accounting Interpretation Text:** "Business is at break-even this period. Review expenses to improve profitability."
- **Zero net income — "loss" warning shown incorrectly?** No, it correctly identifies break-even status.

---

### Test 3.3: Manual Refresh & 60-Second Auto-Refresh
*Verifies concurrent data aggregation and automatic update states.*

**What to do:**
1. Look at the toolbar header and note the **Last Refreshed** text (e.g., `Last refreshed: 11:07:05`).
2. Click the **↻ Refresh** button.
3. Keep the dashboard open and idle for 90 seconds. Monitor the timestamp.

**What you should see:**
- [ ] Clicking the manual **Refresh** button triggers `RefreshAsync` in the ViewModel.
- [ ] A loading overlay displaying *"Loading KPIs…"* appears briefly over the cards while data is being concurrently fetched.
- [ ] The loading overlay disappears and the "Last refreshed" timestamp updates to the current system time.
- [ ] After 60 seconds of idle time, the `DispatcherTimer` triggers. The dashboard silently updates, reloading fresh KPI sums and updating the timestamp automatically.

*Status Check:*
- **Original timestamp:** _______________
- **Timestamp after manual refresh:** _______________
- **Timestamp after 60s idle (Auto-refresh):** _______________

---

## Part 4: UI Read-Only Enforcement on Shared Views

*Verifies that for screens accessible to both Manager and Owner, the Owner's view is strictly locked down (all edit/submit/delete controls are disabled or hidden).*

### Test 4.1: POS Transaction History
**What to do:**
1. Navigate to **Point of Sale** -> **Transaction History** in the sidebar.
2. Click **Search** to load the available transactions (should load **6 completed transactions**: `TX-2026-0001` through `TX-2026-0006`).
3. Select any transaction (e.g., `TX-2026-0001` for Urea ₱21,840.00) and attempt to click the **Process Return** button.

**What you should see:**
- [ ] The transaction list successfully loads **6 completed transactions** from the database.
- [ ] The **Process Return** button is visually greyed out and is completely **disabled** (`IsEnabled = False`) regardless of whether a row is selected.
- [ ] The *View Receipt* button remains active and usable, opening the receipt archive viewer for the selected transaction.

*Observed:* 6 transactions loaded; Process Return button disabled; View Receipt works.

---

### Test 4.2: Accounts Payable Ledger
**What to do:**
1. Navigate to **Purchasing** -> **Accounts Payable** in the sidebar.
2. Verify that two unpaid accounts payable records are listed.
3. Select one from the grid (e.g., `GR-2026-0003` for **₱40,000.00**).
4. Look for the payment controls (e.g., **Record Payment** button).

**What you should see:**
- [ ] Two unpaid AP entries exist: **₱7,000.00** (FarmFresh Seeds, GR-2026-0002) and **₱40,000.00** (AgriChem Supplies, GR-2026-0003) for a total of **₱47,000.00** outstanding.
- [ ] The **Record Payment** button is visually greyed out and is completely **disabled** (`IsEnabled = False`).
- [ ] The *Refresh* button and filters remain enabled.

*Observed:* Two unpaid records present; Record Payment button disabled.

---

### Test 4.3: Purchase Orders View
**What to do:**
1. Navigate to **Purchasing** -> **Purchase Orders** in the sidebar.
2. Observe the overall screen and verify that **4 purchase orders** are loaded.
3. Click on a purchase order (e.g., `PO-2026-0001` or `PO-2026-0004`).

**What you should see:**
- [ ] The entire action button row (**New PO**, **Edit**, **Submit**, **Delete**, and **Refresh**) is completely **hidden** (`Visibility = Collapsed`) — the whole `StackPanel` is gated by `IsManager`. There is no Refresh button visible to Owner on this screen.
- [ ] The list successfully displays **4 purchase orders** (`PO-2026-0001` through `PO-2026-0004`).
- [ ] The status filter ComboBox, search TextBox, and status bar remain visible and functional.

*Observed:* 4 POs loaded; action buttons hidden.

---

### Test 4.4: Stock Dashboard View
**What to do:**
1. Navigate to **Inventory** -> **Stock Dashboard** in the sidebar.
2. Review the product list and select a product to open details.

**What you should see:**
- [ ] Product grid, filters, and search load and function correctly showing **20 products**.
- [ ] Selecting **Complete Fertilizer** displays **3 units** remaining in Batch 2 (Unit Cost: ₱1,200.00).
- [ ] Selecting **Urea** displays **3 units** remaining in Batch 3 (Unit Cost: ₱1,400.00).
- [ ] Selecting **Hybrid Rice RC222** displays **50 units** remaining in Batch 5 (Unit Cost: ₱800.00, Expiry Date: 2026-06-06).
- [ ] *HIDDEN:* Any action items, reorder suggestion triggers, or stock adjustment buttons are completely absent or disabled.
- [ ] **INV-15 columns visible (read-only):** **Retail Price**, **Avg Cost**, and **FIFO Cost** columns render in the product grid for the Owner exactly as they do for the Manager — no role-gated hiding. Tooltips on each header are readable.

*Observed:* 20 products present; correct stocks displayed; cost columns visible.

---

### Test 4.5: Financial Overview (Accounting Tile Block)
**What to do:**
1. Navigate to **Accounting** -> **Financial Overview** in the sidebar.
2. Find the **VAT Payable KPI Tile**.
3. Attempt to click on the VAT Payable Tile.

**What you should see:**
- [ ] The VAT tile displays correctly with a value of **₱133.93** (representing the output VAT from the VAT-registered transaction `TX-2026-0006`).
- [ ] Clicking on the tile does **nothing** (navigation is suppressed because `VatReturnView` is not present in the Owner's navigation groups, so the lookup returns `Nothing` and the command exits silently).
- [ ] **Note:** The tile still renders with a `Cursor="Hand"` pointer — this is expected behavior (the cursor is hardcoded in `VatPayableTile.xaml` and not role-gated). The hand cursor appears for both roles; only navigation is suppressed for Owner.

*Observed:* VAT displays ₱133.93; tile click is ignored.

---

## Part 5: Database Layer Write Rejection (OWASP DA5 Security Enforcement)

*Verifies robust back-end enforcement that blocks all database writes by an Owner session, ensuring safety even if UI-layer boundaries are bypassed.*

### Test 5.1: Transaction Boundary Write Rejection
**What to do:**
1. Under a developer environment with debugging active, log in as `owner`.
2. Access the Visual Studio Immediate Window (or trigger a debug helper that attempts to write a record to any module context, e.g., inserting a row in `Pur_PurchaseOrders` or `Pos_SalesTransactions`).
3. Execute the database save action.

**What you should see:**
- [ ] The EF Core DbContext or transaction boundary detects that the active session belongs to `UserRole.Owner`.
- [ ] The save action is rejected and throws a `UnauthorizedWriteException` (or equivalent database security exception).
- [ ] The database transaction rolls back, confirming zero rows are written to the database.

*Status Check:*
- **Exception type thrown on database write:** `UnauthorizedWriteException` (expected)
- **Rollback confirmed in MariaDB (zero changes committed to central DB):** Confirmed (expected)

> **Note:** The `RoleGuardInterceptor` fires on both `SavingChanges` and `SavingChangesAsync`. It permits writes only when `WriteContextKind.System` is active, when the session is unauthenticated (startup/login), or when the Manager role is active. Owner-role writes are blocked unless `WriteContextKind.AuthSelfService` is set with a matching `SelfServiceUsername`.

---

### Test 5.2: Owner Self-Service Credentials Update
*Verifies that the Owner can safely update their own login details, which is the only authorized write operation.*

> **Note:** The DA6 first-login flow in Test 0.1 already exercised this path for the initial password set. This test verifies that an additional password change from within an active session also works.

**What to do:**
1. While logged in as `owner`, navigate to the self-service credentials/profile panel (or trigger the profile change flow).
2. Enter the current password (set in Test 0.1) and set a new password.
3. Save the change.

**What you should see:**
- [ ] The password change succeeds since the authentication context allows self-service updates on the owner's own `UserAccount` row.
- [ ] `LastPasswordChangeAt` is updated in `Sys_UserAccounts` (verify in MariaDB).

*Status Check:*
- **Self-service password update succeeds:** Yes (expected)
- **New password set to:** VistaOwner2! (expected)

---

### Test 5.3: Cross-User Credentials Modification Restriction
*Verifies that the Owner is strictly blocked from modifying other user credentials.*

**What to do:**
1. Attempt to trigger a password update request targeting a different user (such as `manager`) by passing the manager's `userId`.
2. Commit the request.

**What you should see:**
- [ ] The update fails and throws a descriptive validation error (e.g. *"Owner accounts are not permitted to change credentials of other users."*).
- [ ] The database does not modify the target user's credentials.

*Status Check:*
- **Cross-user modification request failed:** Yes (expected)
- **Error message returned:** "Owner accounts are not permitted to change credentials of other users." (expected)

---

## Part 6: ACC-21 / ACC-22 / INV-15 — Read-Only Verification of Bug-Fix Correctness

> **Goal:** Confirm that the 2026-05-27 bug-fix trio surfaces correctly through the Owner's read-only views — no duplicated revenue lines, accurate per-batch COGS, and the new Stock Dashboard cost columns visible.
>
> **Prerequisite:** The Manager checklist tests have been successfully executed, populating the database with a 12-unit Urea sale (TX-2026-0001) spanning multiple stock batches (Batch 1 at ₱1,520.00 and Batch 3 at ₱1,400.00).

### Test 6.1: Stock Dashboard Cost Columns Visible to Owner (INV-15 read path)
*Owner is read-only, but the INV-15 columns must still surface so the Owner can compare cost vs. retail at a glance.*

**What to do:**
1. While logged in as `owner`, navigate to **Inventory → Stock Dashboard**.
2. Identify a product that has received stock (e.g., Urea or Complete Fertilizer).
3. Compare what the Owner sees against the Manager's view of the same row.

**What you should see:**
- [ ] **Retail Price**, **Avg Cost**, **FIFO Cost** columns are all visible (none are role-gated).
- [ ] Hovering each column header shows the same tooltip text as on the Manager view.
- [ ] Values match what the Manager sees (read-only does not mutate).
- [ ] No "Edit Price" / "Adjust Cost" / "Override Cost" buttons exist near the new columns (cost data is read-only for Owner; INV-15 introduced no edit affordance for either role).

*Status Check:*
- **Three cost columns visible to Owner:** Yes (Retail Price, Avg Cost, FIFO Cost)
- **Values match Manager-side view:** Yes
- **No edit affordances on cost columns:** Yes

---

### Test 6.2: Financial Overview Reflects Single Accurate Revenue Row (ACC-21 + ACC-22)
*The KPI tiles must not double-count revenue and must reflect the corrected COGS.*

**Prerequisite:** Manager E2E tests complete — the 12-unit Urea sale (`TX-2026-0001` for ₱21,840.00) exists.

**What to do:**
1. Navigate to **Accounting → Financial Overview**.
2. Inspect the period-to-date Revenue, COGS, and Gross Profit tiles for the period containing today's date.
3. Cross-check the values against the raw `acc_revenuerecords` table via MariaDB central.

**What you should see:**
- [ ] The PTD revenue contribution from the test sale equals exactly `₱21,840.00` (NOT double-counted).
- [ ] PTD COGS includes exactly `₱18,000.00` for that sale (10 units from Batch 1 @ ₱1,520 + 2 units from Batch 3 @ ₱1,400).
- [ ] PTD Gross Profit for that sale = `₱3,840.00` (representing `₱21,840.00 - ₱18,000.00`).
- [ ] The tile's plain-language interpretation handles gross profit gracefully without spurious warnings.

*Status Check:*
- **Revenue tile value:** ₱21,840.00 (expected: includes ₱21,840.00 only once)
- **COGS tile value:** ₱18,000.00 (expected: includes ₱18,000.00)
- **Gross Profit:** ₱3,840.00 (expected: ₱3,840.00 from this sale)
- **No phantom "loss" warning:** Yes

---

### Test 6.3: Income Statement & Sales Summary — Same Sale Appears Once (ACC-22)
*The duplicate-writer race would inflate both reports by 2×. Confirm neither does.*

**Prerequisite:** Manager E2E tests complete.

**What to do:**
1. Navigate to **Accounting → Income Statement** for the current month. Note the total Sales Revenue and total COGS lines.
2. Navigate to **Accounting → Sales Summary**. Look at the per-product row for Urea.

**What you should see:**
- [ ] **Income Statement** — Sales Revenue contains the ₱21,840.00 test sale exactly **once**, COGS contains the ₱18,000.00 expense exactly **once**.
- [ ] **Sales Summary** — the Urea product row shows `QuantitySold = 12` (not 24), and the corresponding Gross Profit matches ₱3,840.00.
- [ ] Drilling into the product's per-transaction breakdown (if supported) lists **one** line for the test transaction, not two.

*Status Check:*
- **Income Statement Sales Revenue total looks single-counted:** Yes (expected)
- **Sales Summary QuantitySold for test product:** 12 (expected)
- **Single transaction line per product in drilldown:** Yes (expected)

---

### Test 6.4: Direct MariaDB Central Read — Acc_RevenueRecords Integrity (ACC-22)
*Owner cannot write, but can read the DB to verify integrity.*

**What to do:**
1. Open XAMPP Command Line or query tool and connect to `merchsys_central`.
2. Run:
```sql
SELECT SourceTransactionId, ProductId, COUNT(*) AS row_count
FROM acc_revenuerecords
GROUP BY SourceTransactionId, ProductId
HAVING COUNT(*) > 1;
```
3. Then run:
```sql
SELECT COUNT(*) AS sale_cogs_rows FROM inv_salecogs;
```

**What you should see:**
- [ ] The first query returns **zero rows** — confirms no `(Tx, Product)` pair has more than one revenue record system-wide (ACC-22 invariant holds).
- [ ] The second query returns exactly **7 rows** (representing FIFO stock deductions: 2 for Urea sale TX-2026-0001, and 5 for Complete Fertilizer sales TX-2026-0002 through TX-2026-0006).

*Status Check:*
- **Duplicate `(Tx, Product)` revenue rows:** 0 (expected)
- **`Inv_SaleCogs` row count:** 7 (expected)

---

### Test 6.5: Central MariaDB Row Count Verification (INFRA-23–27 + ACC-21 + ACC-22)
*Owner is read-only on every DB. This test verifies that the central MariaDB contains the correct rows — in the pure client-server architecture, local and central are the same database.*

**Prerequisite:** Manager protocol (Tests 4.1 → 7.5) has been executed.

**What to do:**
1. Run row counts against the central MariaDB:
```sql
SELECT 'Inv_SaleCogs' AS k, COUNT(*) AS c FROM inv_salecogs UNION ALL
SELECT 'Acc_RevenueRecords', COUNT(*) FROM acc_revenuerecords UNION ALL
SELECT 'Pos_SalesTransactions', COUNT(*) FROM pos_salestransactions UNION ALL
SELECT 'Inv_StockBatches', COUNT(*) FROM inv_stockbatches UNION ALL
SELECT 'Inv_StockMovements', COUNT(*) FROM inv_stockmovements;
```
2. Compare row-for-row content on the two highest-stakes tables:
```sql
-- Per-batch COGS for the Urea sale (ACC-21)
SELECT TransactionId, ProductId, BatchId, QuantityDeducted, UnitCost, Cogs
FROM inv_salecogs ORDER BY Id;

-- Revenue ledger (ACC-22 — no duplicates allowed)
SELECT Id, SourceTransactionId, ProductId, QuantitySold, NetAmount, COGS, GrossProfit
FROM acc_revenuerecords ORDER BY Id;
```

**What you should see:**
- [ ] `Inv_SaleCogs` shows **7 rows** total, with 2 rows for Urea (deducting 10 from Batch 1 @ ₱1,520.00 and 2 from Batch 3 @ ₱1,400.00) and 5 rows for Complete Fertilizer (each deducting 1 from Batch 2 @ ₱1,200.00).
- [ ] `acc_revenuerecords` has exactly **6 rows** corresponding to transactions `TX-2026-0001` through `TX-2026-0006`, with no duplicate `(SourceTransactionId, ProductId)` pairs.
- [ ] Seeded reference tables (`inv_products`, `pur_vendors`, `pos_creditaccounts`) show their correct seeded row counts.

*Status Check:*
- **Inv_SaleCogs count:** 7 (expected)
- **Acc_RevenueRecords count:** 6 (expected)
- **Pos_SalesTransactions count:** 6 (expected)
- **Inv_StockBatches count:** 4 (expected)
- **Inv_StockMovements count:** 11 (expected)
- **Inv_Products on central (expected: 20 seeded):** 20 (expected)

---

### Test 6.6: Local Sync_Journal Drain Check
*The Sync_Journal and offline-first sync layer were fully decommissioned by INFRA-27 (2026-05-28). This test is now replaced by the pure MariaDB row-count verification in Test 6.5 above.*

> **Note:** There is no `Sync_Journal` table in the VISTA database as of INFRA-27. If you attempt to query it, the query will fail with a "table not found" error, which is the expected behavior confirming the decommission was successful.

**What to do:**
1. Attempt to query the `Sync_Journal` table:
```sql
SELECT COUNT(*) FROM Sync_Journal;
```

**What you should see:**
- [ ] The query returns an **error** ("Table 'merchsys_central.Sync_Journal' doesn't exist") — confirms INFRA-27 decommission is complete and no legacy sync tables were left behind.

*Status Check:*
- **Sync_Journal table absent (expected: query error):** Yes (expected query error)

---

## Session Notes

**Overall Verdict:** (pending)

*(Record any bugs found, unexpected behavior, or deviations from expected values here.)*
