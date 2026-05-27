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
| Main Sidebar View Model (Nav) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` |
| SQLite Database File | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| Security Exception | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Exceptions\UnauthorizedWriteException.vb` (or equivalent data layer rule) |

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

### Test 2.1: Restricted Sidebar Access
*Verifies that the Owner role's navigation sidebar is restricted exclusively to read-only views, completely hiding operational write views as specified in Section 7 of the system plan.*

**What to do:**
1. While logged in as `owner`, inspect the sidebar navigation panel.
2. Go through each group and check that all CRUD, transaction-processing, configuration, and developer menus are hidden.

**What you should see:**
- [ ] **Owner Dashboard** group contains:
  - `KPI Overview` (the main landing page)
- [ ] **Point of Sale** group contains:
  - `Transaction History` (read-only historical search)
  - *HIDDEN:* Sales Cart, Credit Management, Daily Summary, and VAT Settings are completely absent.
- [ ] **Purchasing** group contains:
  - `Purchase Orders` (read-only list of POs)
  - `Accounts Payable` (read-only AP ledger)
  - *HIDDEN:* Goods Receiving, Vendor Directory, and Reorder Suggestions are completely absent.
- [ ] **Inventory** group contains:
  - `Stock Dashboard` (read-only inventory status list)
  - *HIDDEN:* Product Management, Expiry Monitor, and Shrinkage are completely absent.
- [ ] **Accounting** group contains:
  - `Financial Overview` (P&L metrics and Vat tile)
  - `Income Statement` (monthly profit/loss reports)
  - `Sales Summary` (sales trends review)
  - `VAT Relief Report` (BIR VAT relief/exemption report — read-only, visible to both roles)
  - *HIDDEN:* Tamper Audit Report and VAT Return (BIR) are completely absent.
- [ ] *HIDDEN:* **Developer Tools** menu group is completely absent.

*Status Check:*
- **Total sidebar items visible:** _______________ (expected: 9 — Owner Dashboard×1 + POS×1 + Purchasing×2 + Inventory×1 + Accounting×4)
- **Confirm absolute absence of "Sales Cart":** _______________
- **Confirm absolute absence of "VAT Return (BIR)":** _______________
- **Confirm presence of "VAT Relief Report" in Accounting:** _______________

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

## Session Notes

**Overall Verdict:** (pending)

*(Record any bugs found, unexpected behavior, or deviations from expected values here.)*
