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
- **Activity Rail icon count visible:** _______________ (expected: 4 — DEV icon absent for Owner)
- **Total sub-view items accessible (all rail modules):** _______________ (expected: 9 — POS×1 + Purchasing×2 + Inventory×1 + Accounting×4 + Owner Dashboard KPI×1)
- **Confirm absolute absence of "Sales Cart":** _______________
- **Confirm absolute absence of "VAT Return (BIR)":** _______________
- **Confirm presence of "VAT Relief Report" in Accounting panel:** _______________
- **Connection Status Badge state:** _______________

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
- **Landing screen name displayed on header:** _______________

---

### Test 3.2: 4 KPI Cards & Plain-Language Interpretations
*Verifies the correctness of the KPI metrics and the plain-language interpretations mandatory under academic plan A5.*

> **Factory-reset baseline:** No stock batches, no transactions, no purchase orders exist. All KPI values are zero or empty. Verify that the plain-language interpretations handle the all-zero case gracefully without errors.

**What to do:**
1. While logged in as `owner`, observe the 2×2 grid of KPI cards: Purchasing, Inventory, Sales, and Accounting.
2. Note the values of the numerical indicators.
3. Verify that each card features a green border box labelled **"WHAT THIS MEANS"** with a plain-language summary.

**What you should see:**
- [ ] **📦 PURCHASING CARD:**
  - *Metrics:* Active Vendors: **3**, Open Purchase Orders: **0**, Pending Deliveries: **0**, Overdue Accounts Payable: **₱0**.
  - *What This Means:* *"No open purchase orders. All accounts payable are settled."* (or equivalent zero-state text).
- [ ] **📊 INVENTORY CARD:**
  - *Metrics:* Total SKUs: **20**, Total Stock Value: **₱0**, Low-Stock Items: **20**, Expiring Within 30 Days: **0**.
  - *What This Means:* Should reflect 20 low-stock items (e.g., *"20 product(s) are below minimum stock level. Check reorder suggestions."*).
- [ ] **💰 SALES CARD:**
  - *Metrics:* Today's Revenue: **₱0**, This Week's Revenue: **₱0**, Transactions Today: **0**, Top Product Today: **—**.
  - *What This Means:* *"No sales recorded this week."* (or equivalent zero-state text).
- [ ] **📈 ACCOUNTING CARD:**
  - *Metrics:* Current Period Net Income: **₱0**, Customer Credit Outstanding (AR): **₱0**, Accounts Payable Outstanding: **₱0**.
  - *What This Means:* Should handle zero net income gracefully (no "operating at a loss" false positive for a zero value).

*Status Check:*
- **Purchasing Interpretation Text:** _______________
- **Inventory Interpretation Text:** _______________
- **Sales Interpretation Text:** _______________
- **Accounting Interpretation Text:** _______________
- **Zero net income — "loss" warning shown incorrectly?** _______________

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
2. Click **Search** to load any available transactions (may be empty after factory reset).
3. If any transactions are listed, select one and attempt to click the **Process Return** button.

**What you should see:**
- [ ] The **Process Return** button is visually greyed out and is completely **disabled** (`IsEnabled = False`) regardless of whether a row is selected.
- [ ] The *View Receipt* button remains active and usable (or is gracefully absent when no row is selected).

*Observed:* _______________

---

### Test 4.2: Accounts Payable Ledger
**What to do:**
1. Navigate to **Purchasing** -> **Accounts Payable** in the sidebar.
2. If any AP records exist, select one from the grid.
3. Look for the payment controls (e.g., **Record Payment** button).

**What you should see:**
- [ ] The **Record Payment** button is visually greyed out and is completely **disabled** (`IsEnabled = False`).
- [ ] The *Refresh* button and filters remain enabled.

*Observed:* _______________

---

### Test 4.3: Purchase Orders View
**What to do:**
1. Navigate to **Purchasing** -> **Purchase Orders** in the sidebar.
2. Observe the overall screen (may be empty after factory reset) and click on a purchase order if any exist.

**What you should see:**
- [ ] The entire action button row (**New PO**, **Edit**, **Submit**, **Delete**, and **Refresh**) is completely **hidden** (`Visibility = Collapsed`) — the whole `StackPanel` is gated by `IsManager`. There is no Refresh button visible to Owner on this screen.
- [ ] The status filter ComboBox, search TextBox, and status bar remain visible and functional.

*Observed:* _______________

---

### Test 4.4: Stock Dashboard View
**What to do:**
1. Navigate to **Inventory** -> **Stock Dashboard** in the sidebar.
2. Review the product list (20 seeded products, all at 0 stock) and select a product to open details.

**What you should see:**
- [ ] Product grid, filters, and search load and function correctly showing 20 products.
- [ ] Selecting a product opens the detail panel — batch and movement sub-grids are visible but empty (no data after factory reset).
- [ ] *HIDDEN:* Any action items, reorder suggestion triggers, or stock adjustment buttons are completely absent or disabled.
- [ ] **INV-15 columns visible (read-only):** **Retail Price**, **Avg Cost**, and **FIFO Cost** columns render in the product grid for the Owner exactly as they do for the Manager — no role-gated hiding. Tooltips on each header are readable.

*Observed:* _______________

---

### Test 4.5: Financial Overview (Accounting Tile Block)
**What to do:**
1. Navigate to **Accounting** -> **Financial Overview** in the sidebar.
2. Find the **VAT Payable KPI Tile**.
3. Attempt to click on the VAT Payable Tile.

**What you should see:**
- [ ] The VAT tile displays correctly (value: ₱0.00 after factory reset).
- [ ] Clicking on the tile does **nothing** (navigation is suppressed because `VatReturnView` is not present in the Owner's navigation groups, so the lookup returns `Nothing` and the command exits silently).
- [ ] **Note:** The tile still renders with a `Cursor="Hand"` pointer — this is expected behavior (the cursor is hardcoded in `VatPayableTile.xaml` and not role-gated). The hand cursor appears for both roles; only navigation is suppressed for Owner.

*Observed:* _______________

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
- **Exception type thrown on database write:** _______________
- **Rollback confirmed in SQLite (zero new Sync_Journal rows):** _______________

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
- [ ] `LastPasswordChangeAt` is updated in `Sys_UserAccounts` (verify in SQLite).

*Status Check:*
- **Self-service password update succeeds:** _______________
- **New password set to:** _______________

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
- **Cross-user modification request failed:** _______________
- **Error message returned:** _______________

---

## Part 6: ACC-21 / ACC-22 / INV-15 — Read-Only Verification of Bug-Fix Correctness

> **Goal:** Confirm that the 2026-05-27 bug-fix trio surfaces correctly through the Owner's read-only views — no duplicated revenue lines, accurate per-batch COGS, and the new Stock Dashboard cost columns visible.
>
> **Prerequisite:** The Manager checklist Test 7.3 has been executed at least once (30-unit break-even sale across three batches at ₱1,200 / ₱1,100 / ₱1,000) so there is real data to read. If only Manager Test 5/6 have run, the queries below will return no rows — that itself is a valid pre-condition to flag.

### Test 6.1: Stock Dashboard Cost Columns Visible to Owner (INV-15 read path)
*Owner is read-only, but the INV-15 columns must still surface so the Owner can compare cost vs. retail at a glance.*

**What to do:**
1. While logged in as `owner`, navigate to **Inventory → Stock Dashboard**.
2. Identify a product that has received stock (run Manager Test 7.2 first if needed).
3. Compare what the Owner sees against the Manager's view of the same row.

**What you should see:**
- [ ] **Retail Price**, **Avg Cost**, **FIFO Cost** columns are all visible (none are role-gated).
- [ ] Hovering each column header shows the same tooltip text as on the Manager view.
- [ ] Values match what the Manager sees (read-only does not mutate).
- [ ] No "Edit Price" / "Adjust Cost" / "Override Cost" buttons exist near the new columns (cost data is read-only for Owner; INV-15 introduced no edit affordance for either role).

*Status Check:*
- **Three cost columns visible to Owner:** _______________
- **Values match Manager-side view:** _______________
- **No edit affordances on cost columns:** _______________

---

### Test 6.2: Financial Overview Reflects Single Accurate Revenue Row (ACC-21 + ACC-22)
*The KPI tiles must not double-count revenue and must reflect the corrected COGS.*

**Prerequisite:** Manager Test 7.3 complete — the 30-unit break-even sale exists.

**What to do:**
1. Navigate to **Accounting → Financial Overview**.
2. Inspect the period-to-date Revenue, COGS, and Gross Profit tiles for the period containing today's date.
3. Cross-check the values against the raw `Acc_RevenueRecords` table via DB Browser.

**What you should see:**
- [ ] The PTD revenue contribution from the test sale equals exactly `₱33,000.00` (NOT `₱66,000` — which would indicate the pre-ACC-22 duplicate revenue row).
- [ ] PTD COGS includes exactly `₱33,000.00` for that sale (NOT `₱36,000` — which would indicate the pre-ACC-21 single-batch COGS bug).
- [ ] PTD Gross Profit for that sale = `₱0.00` (break-even, no phantom loss, no phantom profit).
- [ ] The tile's plain-language interpretation handles `₱0` gross profit gracefully (no spurious "operating at a loss" warning triggered solely by this transaction).

*Status Check:*
- **Revenue tile value:** _______________ (expected: includes ₱33,000 only once)
- **COGS tile value:** _______________ (expected: includes ₱33,000)
- **Gross Profit:** _______________ (expected: ₱0.00 from this sale)
- **No phantom "loss" warning:** _______________

---

### Test 6.3: Income Statement & Sales Summary — Same Sale Appears Once (ACC-22)
*The duplicate-writer race would inflate both reports by 2×. Confirm neither does.*

**Prerequisite:** Manager Test 7.3 complete.

**What to do:**
1. Navigate to **Accounting → Income Statement** for the current month. Note the total Sales Revenue and total COGS lines.
2. Navigate to **Accounting → Sales Summary**. Look at the per-product row for the product used in the test sale.

**What you should see:**
- [ ] **Income Statement** — Sales Revenue contains the ₱33,000 test sale exactly **once**, COGS contains the ₱33,000 expense exactly **once**.
- [ ] **Sales Summary** — the test product row shows `QuantitySold = 30` (not 60), and the corresponding Gross Profit for the row matches the Financial Overview.
- [ ] Drilling into the product's per-transaction breakdown (if supported) lists **one** line for the test transaction, not two.

*Status Check:*
- **Income Statement Sales Revenue total looks single-counted:** _______________
- **Sales Summary QuantitySold for test product:** _______________ (expected: 30, NOT 60)
- **Single transaction line per product in drilldown:** _______________

---

### Test 6.4: Direct SQLite Read — Acc_RevenueRecords Integrity (ACC-22)
*Owner cannot write, but can read the DB to verify integrity. Useful when the Manager is not present to run Test 7.3.*

**What to do:**
1. Open `%LOCALAPPDATA%\MerchSys\merchsys.db` in DB Browser for SQLite (read-only).
2. Run:
```sql
SELECT SourceTransactionId, ProductId, COUNT(*) AS row_count
FROM Acc_RevenueRecords
GROUP BY SourceTransactionId, ProductId
HAVING COUNT(*) > 1;
```
3. Then run:
```sql
SELECT COUNT(*) AS sale_cogs_rows FROM Inv_SaleCogs;
```

**What you should see:**
- [ ] The first query returns **zero rows** — confirms no `(Tx, Product)` pair has more than one revenue record system-wide (ACC-22 invariant holds).
- [ ] The second query returns a **positive integer** if at least one sale has been completed since the bug-fix deployment (ACC-21 ledger is populated). Zero is acceptable only if no sales have occurred since deployment.

*Status Check:*
- **Duplicate `(Tx, Product)` revenue rows:** _______________ (expected: 0)
- **`Inv_SaleCogs` row count:** _______________ (expected: ≥ 0; ≥ 1 if any sale has run)

---

### Test 6.5: Central MariaDB Row Count Verification (INFRA-23–27 + ACC-21 + ACC-22)
*Owner is read-only on every DB. This test verifies that the central MariaDB contains the correct rows — in the pure client-server architecture, local and central are the same database, so this is a direct count check, not a sync reconciliation.*

**Prerequisite:** Manager protocol (Tests 4.1 → 7.5) has been executed at least once.

**What to do:**
1. Run row counts against the central MariaDB:
```sql
-- mysql -u root merchsys_central
SELECT 'Inv_SaleCogs' AS k, COUNT(*) AS c FROM Inv_SaleCogs UNION ALL
SELECT 'Acc_RevenueRecords', COUNT(*) FROM Acc_RevenueRecords UNION ALL
SELECT 'Pos_SalesTransactions', COUNT(*) FROM Pos_SalesTransactions UNION ALL
SELECT 'Inv_StockBatches', COUNT(*) FROM Inv_StockBatches UNION ALL
SELECT 'Inv_StockMovements', COUNT(*) FROM Inv_StockMovements;
```
2. Compare row-for-row content on the two highest-stakes tables:
```sql
-- Per-batch COGS for the latest sale (ACC-21)
SELECT TransactionId, ProductId, BatchId, QuantityDeducted, UnitCost, Cogs
FROM Inv_SaleCogs ORDER BY Id;

-- Revenue ledger (ACC-22 — no duplicates allowed)
SELECT Id, SourceTransactionId, ProductId, QuantitySold, NetAmount, COGS, GrossProfit
FROM Acc_RevenueRecords ORDER BY Id;
```

**What you should see:**
- [ ] After the ACC-21 reproduction sale (Manager Test 7.3), `Inv_SaleCogs` shows **3 rows** for the spanning transaction with COGS values `12000.0000`, `11000.0000`, `10000.0000`.
- [ ] `Acc_RevenueRecords` has no `(SourceTransactionId, ProductId)` duplicate (ACC-22 invariant holds).
- [ ] All three `Inv_StockBatches` rows show `QuantityRemaining = 0` (INFRA-26 FIFO `FOR UPDATE` lock applied correctly).
- [ ] Seeded reference tables (`Inv_Products`, `Pur_Vendors`, `Inv_ProductCategories`, `Pos_CreditAccounts`) show their seeded row counts — data is present in MariaDB because seeding happens in `0002_seed_reference_data.sql` (INFRA-24), not via the old `<NoSync>` filter.

*Status Check:*
- **Inv_SaleCogs count:** _______________
- **Acc_RevenueRecords count:** _______________
- **Pos_SalesTransactions count:** _______________
- **Inv_StockBatches count:** _______________
- **Inv_StockMovements count:** _______________
- **Inv_Products on central (expected: 20 seeded):** _______________

---

### Test 6.6: Local Sync_Journal Drain Check
*The Sync_Journal and offline-first sync layer were fully decommissioned by INFRA-27 (2026-05-28). This test is now replaced by the pure MariaDB row-count verification in Test 6.5 above.*

> **Note:** There is no `Sync_Journal` table in the VISTA database as of INFRA-27. If you attempt to query it, the query will fail with a "table not found" error, which is the expected behavior confirming the decommission was successful.

**What to do:**
1. Attempt to query the `Sync_Journal` table:
```sql
-- mysql -u root merchsys_central
SELECT COUNT(*) FROM Sync_Journal;
```

**What you should see:**
- [ ] The query returns an **error** ("Table 'merchsys_central.Sync_Journal' doesn't exist") — confirms INFRA-27 decommission is complete and no legacy sync tables were left behind.

*Status Check:*
- **Sync_Journal table absent (expected: query error):** _______________

---

## Session Notes

**Overall Verdict:** (pending)

*(Record any bugs found, unexpected behavior, or deviations from expected values here.)*
