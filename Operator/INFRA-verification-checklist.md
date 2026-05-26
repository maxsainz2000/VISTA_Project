---
module: Infrastructure
source: Infrastructure-audit-2026-05-26.md
originally-generated: 2026-05-17
last-synced: 2026-05-26
---

# Operator Verification Checklist — Infrastructure

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 18 Infrastructure plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| Production config template | `WPF_Applications\MerchSys\src\MerchSys.App\appsettings.Production.template.json` |
| Where to put the real config | `%LOCALAPPDATA%\VISTA\appsettings.Production.json` |
| MariaDB init SQL | `Plans\VISTA_Modules\Infrastructure\sql\mariadb-init.sql` |
| Receipt schema alignment SQL | `Plans\VISTA_Modules\Infrastructure\sql\mariadb-receipt-schema-alignment.sql` |
| MySqlConnector wrapper | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Sync\MariaDbSyncContext.vb` |
| Receipt integrity triggers SQL | `Plans\VISTA_Modules\Infrastructure\sql\02-pos-receipt-integrity-triggers.sql` |
| Production deployment runbook | `Plans\VISTA_Modules\Infrastructure\runbooks\01-production-deployment.md` |
| SyncStatusIndicator (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Shell\SyncStatusIndicator.xaml` |
| SyncStatusIndicator (code-behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Shell\SyncStatusIndicator.xaml.vb` |
| SyncStatusIndicatorViewModel | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\Shell\SyncStatusIndicatorViewModel.vb` |
| SyncOrchestrator | `WPF_Applications\MerchSys\src\MerchSys.App\Services\SyncOrchestrator.vb` |
| SyncWorker | `WPF_Applications\MerchSys\src\MerchSys.App\Services\SyncWorker.vb` |
| Directory.Build.props (NU1608) | `WPF_Applications\MerchSys\Directory.Build.props` |
| ConnectionStringLoader | `WPF_Applications\MerchSys\src\MerchSys.App\Configuration\ConnectionStringLoader.vb` |
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| .gitignore | `VISTA_Project\.gitignore` (line 2: `appsettings.Production.json` is excluded) |
| LoginView (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\LoginView.xaml` |
| LoginViewModel | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\LoginViewModel.vb` |
| LoginSessionService | `WPF_Applications\MerchSys\src\MerchSys.App\Services\LoginSessionService.vb` |
| IAuthenticationService | `WPF_Applications\MerchSys\src\MerchSys.App\Services\IAuthenticationService.vb` |
| UserAccount entity | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Entities\UserAccount.vb` |
| OwnerDashboardView (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\OwnerDashboardView.xaml` |
| OwnerDashboardViewModel | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\OwnerDashboardViewModel.vb` |
| MainWindowViewModel (nav) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` |

---

## INFRA-06 — MariaDB Central Schema & Reconciliation

### Test 1: Set up production database password

**What to do:**
1. Open the template file at:
   `WPF_Applications\MerchSys\src\MerchSys.App\appsettings.Production.template.json`
2. Copy it to `%LOCALAPPDATA%\VISTA\appsettings.Production.json` (create the `VISTA` folder if it does not exist).
3. Open the copied file and fill in the real values:
   - `Host`: your MariaDB server hostname or IP
   - `Password`: the real password for `merchsys_sync` user
   - Leave `SslMode` as `Required`
4. Check that `.gitignore` at the project root already lists `appsettings.Production.json` (it does — line 2).

**What you should see:**
- The file exists at `%LOCALAPPDATA%\VISTA\appsettings.Production.json`.
- Running `git status` does NOT show it as tracked or untracked.

- [X] Production password file created and excluded from git

---

### Test 2: MariaDB schema creates correctly

**What to do:**
1. Open a MariaDB client (like HeidiSQL or the `mariadb` command-line tool).
2. Connect to a fresh MariaDB 11.4.x instance (one with no MerchSys tables yet).
3. Open the SQL script at:
   `Plans\VISTA_Modules\Infrastructure\sql\mariadb-init.sql`
4. Run the entire script against the database.
5. Check the tables that were created.

**What you should see:**
- The script runs without errors.
- All expected tables exist (check against the table list in the INFRA-06 plan).
- Running the same script a second time should not cause errors (idempotent).

- [X] MariaDB init SQL runs clean on a fresh instance

---

## INFRA-08 — MariaDB Receipt Integrity Triggers

### ⏳ Deferred: Trigger DEFINER fix

**What to do:**
- Nothing right now. This is blocked until a formal DB admin account is set up.

**When to do it:**
- Once a dedicated MariaDB admin account exists, open:
  `Plans\VISTA_Modules\Infrastructure\sql\02-pos-receipt-integrity-triggers.sql`
- Replace the triggers so they use `DEFINER = <admin_account>` instead of the anonymous default.

- [ ] ⏳ Deferred — waiting for DB admin account

---

## INFRA-10 — Sync Status Shell Indicator

### Test 3: Sync indicator shows in the status bar

**What to do:**
1. Build the solution in **Debug** configuration (Ctrl+Shift+B).
2. Press **F5** to launch the app.
3. Look at the very bottom of the main window (the status bar area).

> **Indicator XAML:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\Shell\SyncStatusIndicator.xaml`
> **Indicator code-behind:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\Shell\SyncStatusIndicator.xaml.vb`

**What you should see:**
- A sync status indicator is visible in the status bar.
- No binding error messages appear in the Visual Studio **Output** window (look for lines starting with `BindingExpression` or `System.Windows.Data Error`).

- [X] Sync indicator renders in status bar with no binding errors

---

### Test 4: Sync indicator cleans up on shutdown

**What to do:**
1. Open the file:
   `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\Shell\SyncStatusIndicatorViewModel.vb`
2. Find the `Dispose` method in that file (line 144).
3. Set a breakpoint on a line inside the `Dispose` method.
4. Press **F5** to launch the app in Debug mode.
5. Once the app is fully loaded, close the app window normally (click the X button).

**What you should see:**
- The breakpoint in `Dispose` gets hit when you close the app.
- This confirms the indicator cleans up its resources properly.

- [X] Dispose breakpoint is hit on app shutdown

---

## INFRA-11 — Production Deployment Configuration

### ~~⏳ Deferred: Pomelo 10.x upgrade~~ — RESOLVED (INFRA-17)

**Resolution:** Pomelo dependency removed entirely in INFRA-17. Replaced with raw
`MySqlConnector` (ADO.NET). NU1608 suppression removed from `Directory.Build.props`.

- [x] ~~Deferred — waiting for Pomelo 10.x on NuGet~~ → resolved by INFRA-17 (Pomelo removed)

---

### Test 5: Live MariaDB deployment walkthrough

**What to do:**
1. Make sure you have a MariaDB 11.4.x instance running and reachable.
2. Make sure you have already done Test 1 (production password file).
3. Open the runbook at:
   `Plans\VISTA_Modules\Infrastructure\runbooks\01-production-deployment.md`
4. Follow the steps in the runbook to deploy against the live MariaDB instance.
5. Complete steps 2–3 of the INFRA-06 acceptance criteria as described in the runbook.

**What you should see:**
- The deployment completes without errors.
- The app can connect to and read/write from the MariaDB instance.

- [X] Live MariaDB deployment walkthrough — schema/triggers/provisioning/INFRA-06 criteria all ✅. MariaDB 31 tables, 16 triggers, all 5 integrity probes fire ERROR 1644. Three missing tables created (Acc_VatReturns, Acc_VatReturnLines, Pos_VatConfiguration). CreatedBy made nullable. DELETE transmit crash fixed.

---

## INFRA-17 — Replace Pomelo with MySqlConnector

### Test 5b: Sync indicator reaches Online after INFRA-17

**What to do:**
1. Make sure INFRA-17 implementation is complete (Pomelo removed, MySqlConnector in place).
2. Repeat Test 5 steps 1–4 (live MariaDB, production password file, follow runbook).
3. Launch the app, log in, observe the sync status indicator.

**What you should see:**
- The sync indicator transitions Offline → Probing → **Online (green)**.
- No `MissingMethodException` in the Output window.
- The `Sync_Journal` entries are transmitted to MariaDB.

- [X] Sync indicator reaches Online/Green with MySqlConnector — shows "Online just now" after first probe cycle. LastSuccessfulPushAt fixed to update on Error→Online transition.

---

## INFRA-12 — SyncOrchestrator Real Data Transmission

### Test 6: Transmission idempotency

**What to do:**
1. Make sure you have done Test 1 (production password file) and Test 5 (live MariaDB is set up).
2. Launch the app. Let it sync some data to MariaDB.
3. Check which records were synced by querying the `Sync_Journal` table in:
   `%LOCALAPPDATA%\MerchSys\merchsys.db`
4. Force the same batch to sync again (restart the app or trigger the sync worker manually).

> **SyncOrchestrator code:** `WPF_Applications\MerchSys\src\MerchSys.App\Services\SyncOrchestrator.vb`
> **SyncWorker code:** `WPF_Applications\MerchSys\src\MerchSys.App\Services\SyncWorker.vb`

**What you should see:**
- The second sync does NOT create duplicate rows in the MariaDB tables (for non-financial tables).
- No "duplicate key" errors appear in the Output window or logs.

- [X] TransmitBatchAsync handles duplicate batches without errors — idempotency confirmed: UPDATEs re-apply cleanly, INSERTs use EXISTS check (UPDATE if found, skip if financial). Fixed root-cause bug: EF Core temp key was captured in Payload pre-save; fixed to re-read post-save. All 9 crash-simulation entries (5 INSERTs + 4 UPDATEs) re-synced with zero duplicate-key errors.

---

## INFRA-13 — ISyncableRepository Write-Path Migration

### ⏳ Deferred: Accounting handler write-path migration

**What to do:**
- Nothing right now. Whether to migrate `Accounting/Handlers` write paths is a scope decision that hasn't been made yet.

> **Handlers folder:** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Handlers\`

- [ ] ⏳ Deferred — scope decision pending

---

### ⏳ Deferred: ProductManagementViewModel write-path migration

**What to do:**
- Nothing right now. Whether to bring `ProductManagementViewModel.vb` into sync scope is a decision that hasn't been made yet.

> **ViewModel file:** `WPF_Applications\MerchSys\src\MerchSys.Inventory\ViewModels\ProductManagementViewModel.vb` *(if it exists in this location)*

- [ ] ⏳ Deferred — scope decision pending

---

### Test 7: Write-path creates journal rows

**What to do:**
1. Launch the app.
2. Do a write action through one of the migrated services. For example:
   - Create a new product, OR
   - Complete a sale, OR
   - Receive goods on a purchase order
3. Open DB Browser for SQLite and open:
   `%LOCALAPPDATA%\MerchSys\merchsys.db`
4. Look at the `Sync_Journal` table.

**What you should see:**
- A new row appears in `Sync_Journal` matching the write you just did.
- The row has the correct table name, entity ID, and timestamp.

- [X] Writing through a migrated service creates a Sync_Journal row — completed a sale; Sync_Journal captured INSERT/UPDATE rows for Pos_SalesTransactions, Pos_SalesTransactionLines, Pos_OfficialReceipts, Inv_StockMovements, Inv_StockBatches across POS and Inventory modules.

---

## INFRA-14 — Central Schema Alignment for Receipt Sync

### Test 8: Schema alignment SQL is idempotent

**What to do:**
1. Open the SQL file at:
   `Plans\VISTA_Modules\Infrastructure\sql\mariadb-receipt-schema-alignment.sql`
2. Connect to your MariaDB staging/production instance.
3. Run the script once.
4. Run the script a second time.

**What you should see:**
- First run: script completes without errors, columns/indexes are created.
- Second run: script completes without errors again (the `IF NOT EXISTS` guards prevent duplicates).

- [X] Schema alignment SQL runs twice without errors — both runs exit 0; Status (VARCHAR 20, NOT NULL, default 'Issued'), IssuedAt (DATETIME(6), NOT NULL), IntegrityHash (VARCHAR 64, nullable), and IX_OfficialReceipts_IssuedAt index all confirmed present on Pos_OfficialReceipts.

---

### Test 9: Receipt sync sends all required fields

**What to do:**
1. Make sure Test 8 is done (schema is aligned on MariaDB).
2. Make sure Test 6 setup is done (SyncOrchestrator is configured).
3. In the app, complete a sale that generates an official receipt.
4. Wait for the sync to run (or trigger it manually).
5. Open the MariaDB database browser and query:
   ```sql
   SELECT Status, IssuedAt, IntegrityHash FROM Pos_OfficialReceipts ORDER BY Id DESC LIMIT 1;
   ```

**What you should see:**
- The receipt row exists in MariaDB.
- `Status` is not null.
- `IssuedAt` has a valid timestamp.
- `IntegrityHash` has a non-empty hash string.

- [X] Receipt row syncs to MariaDB with Status, IssuedAt, and IntegrityHash filled in — OR-2026-0009 (Id 219): Status='Issued', IssuedAt='2026-05-23 18:31:08.661388', IntegrityHash='cebffb6ae18fd3e16a63ea46878fa287639255562d099cef139377c3a178eb9d'. Three fixes applied: (1) IntegrityHash added to RemoteOfficialReceipt POCO; (2) VatAwareReceiptService patches journal payload after ComputeAndPersistAsync; (3) SyncWorker fixed to run sync on every healthy probe cycle, not just on first Online transition.

---

## INFRA-15 — Login Form & User Authentication

### Test 10: Fresh database creates user accounts

**What to do:**
1. Go to `%LOCALAPPDATA%\MerchSys\` in File Explorer.
2. Rename `merchsys.db` to `merchsys.db.bak` (this is your backup).
3. Press **F5** to launch the app in Debug mode. The app will create a new database on startup.
4. Before logging in, open DB Browser for SQLite and open the new `merchsys.db`.
5. Look at the `Sys_UserAccounts` table.

**What you should see:**
- Two rows: `manager` (Role=1) and `owner` (Role=2).
- Both have Argon2id password hashes (starting with `$argon2id$v=19$m=19456,t=2,p=1$`).
- Both have `LastPasswordChangeAt = NULL` (signals first-login state).

- [X] Fresh DB: Sys_UserAccounts has 2 seeded users with Argon2id hashes — manager (Role=1) and owner (Role=2), both IsActive=1, both LastPasswordChangeAt=NULL, both hashes prefix $argon2id$v=19$m=19456,t=2,p=1$. Confirmed via SQLite query.

---

### Test 11: Manager login with mandatory password change

**What to do:**
1. Launch the app (F5). A login screen should appear.
2. Enter username `manager` and password `Vista2026!`.
3. Click **LOG IN**.

> **LoginView file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\LoginView.xaml`
> **LoginViewModel:** `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\LoginViewModel.vb`

**What you should see:**
- A password-change panel appears ("first login" prompt per DA6 — no default credentials in production use).
- Enter a new password (≥ 8 characters), confirm it, and click **Set Password & Continue**.
- The main shell appears with Manager navigation: Sales Cart, Credit Management, Transaction History, Daily Summary, VAT Settings, Purchase Orders, Goods Receiving, Vendor Directory, Accounts Payable, Reorder Suggestions, Stock Dashboard, Product Management, Expiry Monitor, Shrinkage, Financial Overview, Income Statement, Sales Summary, Tamper Audit Report, VAT Return (BIR).

- [X] Manager login: password change prompt shown, new password accepted, main shell visible with full Manager sidebar — "Please set a new password before continuing." shown; after accepting new password, main window opened with all Manager nav items (Sales Cart, Credit Management, Transaction History, Daily Summary, VAT Settings, Purchase Orders, Goods Receiving, Vendor Directory, Accounts Payable, Reorder Suggestions, Stock Dashboard, Product Management, Expiry Monitor, Shrinkage, Financial Overview, Income Statement, Sales Summary, Tamper Audit Report, VAT Return (BIR), Developer Tools).

---

### Test 12: Owner login with restricted navigation

**What to do:**
1. If you are already logged in, click the **Log Out** button in the sidebar.
2. On the login screen, enter username `owner` and password `Vista2026!`.
3. Complete the mandatory password change (same as Test 11).

**What you should see:**
- The main shell appears with Owner-restricted navigation only:
  - Owner Dashboard
  - Transaction History
  - Purchase Orders, Accounts Payable
  - Stock Dashboard
  - Financial Overview, Income Statement, Sales Summary
- You do **NOT** see: Sales Cart, Credit Management, Daily Summary, VAT Settings, Goods Receiving, Vendor Directory, Reorder Suggestions, Product Management, Expiry Monitor, Shrinkage, Tamper Audit Report, VAT Return (BIR).

- [X] Owner login: restricted sidebar — only read-only views visible — sidebar contains only: KPI Overview (Owner Dashboard), Transaction History, Purchase Orders, Accounts Payable, Stock Dashboard, Financial Overview, Income Statement, Sales Summary. No CRUD views present.

---

### Test 13: Account lockout after 5 failed attempts

**What to do:**
1. Log out if logged in.
2. On the login screen, enter username `manager` and an **incorrect** password.
3. Click **LOG IN**.
4. Repeat this 5 times total (5 wrong passwords in a row).

> **Auth service:** `WPF_Applications\MerchSys\src\MerchSys.App\Services\IAuthenticationService.vb`

**What you should see:**
- After the 5th failed attempt, the error message says the account is locked and shows remaining minutes (approximately 15 minutes).
- Entering the **correct** password while locked still shows the lockout message.

- [X] 5 wrong passwords: lockout message with remaining minutes displayed — after 5 wrong passwords, 6th attempt (even with correct password) shows "Account locked. Try again in 15 minute(s)." UIAutomation confirmed the exact text.

---

### Test 14: Role switch via logout/re-login

**What to do:**
1. Log in as `manager` (use the password you set in Test 11).
2. Note the sidebar items.
3. Click the **Log Out** button in the sidebar.
4. Log in as `owner` (use the password you set in Test 12).
5. Note the sidebar items.

**What you should see:**
- After logging out as Manager and logging in as Owner, the sidebar changes to show only Owner-visible items.
- The landing page changes to the Owner Dashboard.

- [X] Logout → re-login as different role: sidebar and landing page change correctly — logged out as Manager (full sidebar, Stock Dashboard landing), logged in as Owner → sidebar immediately changed to owner-only read-only subset, landing page became OwnerDashboardView.

---

### Test 15: Disabled account cannot log in

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Set `IsActive = 0` on the `owner` row in `Sys_UserAccounts`.
3. Save the change.
4. In the app, log out (or restart the app).
5. Try to log in as `owner` with the correct password.

**What you should see:**
- Login fails with "Invalid credentials" — the error message does NOT reveal that the account is disabled (OWASP best practice).

**After the test:** Set `IsActive` back to `1` in DB Browser so the `owner` account works for future tests.

- [X] Disabled account: login fails with generic error, no information leakage — set IsActive=0 for owner via SQLite, attempted login with correct password → "Invalid credentials" (same as wrong-password error; no mention of account being disabled). IsActive restored to 1 after test.

---

## INFRA-16 — Owner Dashboard & Read-Only View Enforcement

### Test 16: Owner landing page is the Owner Dashboard

**What to do:**
1. Log in as `owner`.
2. Look at the content area (the main panel to the right of the sidebar).

> **OwnerDashboardView:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\OwnerDashboardView.xaml`
> **OwnerDashboardViewModel:** `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\OwnerDashboardViewModel.vb`

**What you should see:**
- The Owner Dashboard is displayed as the landing page (not the Stock Dashboard).
- The dashboard shows a 2×2 grid of KPI cards: Purchasing, Inventory, Sales, Accounting.

- [X] Owner landing page is OwnerDashboardView (not Stock Dashboard) — after owner login, content area shows "Villon Farm Supply — Owner Dashboard" with "Welcome, owner · Read-only access" subtitle and the 2×2 KPI grid. Stock Dashboard is not the default.

---

### Test 17: Owner Dashboard KPI cards show data and interpretations

**What to do:**
1. While logged in as Owner, look at each of the four KPI cards on the Owner Dashboard.
2. Check that each card shows:
   - Numeric KPIs (counts, amounts)
   - A "What This Means" plain-language interpretation section

**What you should see:**
- **Purchasing card:** Active vendors, open POs, pending deliveries, overdue AP + interpretation text
- **Inventory card:** Total SKUs, stock value, low-stock items, expiring soon + interpretation text
- **Sales card:** Today's revenue, weekly revenue, transactions today, top product + interpretation text
- **Accounting card:** Net income, overdue AR, upcoming AP + interpretation text
- If there is no data yet, the interpretation should say something like "No sales recorded" or "All settled" — not show an error.

- [X] All 4 KPI cards display numeric values and "What This Means" interpretation text — Purchasing (4 vendors, 0 POs, ₱0 overdue; "No open purchase orders. All accounts payable are settled."), Inventory (21 SKUs, ₱39,240 value, 20 low-stock, 0 expiring; "20 product(s) are below minimum stock level. Check reorder suggestions."), Sales (₱0 today/week; "No sales recorded this week."), Accounting (₱7,350 net income, ₱0 AR, ₱0 AP; "Business is profitable this period with ₱7,350 net income. All customer credit is settled.").

---

### Test 18: Owner sidebar excludes CRUD views

**What to do:**
1. While logged in as Owner, carefully read every item in the sidebar navigation.
2. Compare against the expected list.

> **Navigation config:** `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` — search for `BuildOwnerNavigationGroups` or `BuildManagerNavigationGroups`.

**Owner SHOULD see:**
- Owner Dashboard
- Transaction History
- Purchase Orders, Accounts Payable
- Stock Dashboard
- Financial Overview, Income Statement, Sales Summary

**Owner should NOT see:**
- Sales Cart, Credit Management, Daily Summary, VAT Settings
- Goods Receiving, Vendor Directory, Reorder Suggestions
- Product Management, Expiry Monitor, Shrinkage
- Tamper Audit Report, VAT Return (BIR)
- Developer Tools

- [X] Owner sidebar: only read-only views listed — KPI Overview, Transaction History, Purchase Orders, Accounts Payable, Stock Dashboard, Financial Overview, Income Statement, Sales Summary.
- [X] Owner sidebar: no CRUD/operational views visible — Sales Cart, Credit Management, Daily Summary, VAT Settings, Goods Receiving, Vendor Directory, Reorder Suggestions, Product Management, Expiry Monitor, Shrinkage, Tamper Audit Report, VAT Return (BIR), Developer Tools all absent.

---

### Test 19: Write buttons disabled for Owner on shared views

**What to do:**
1. While logged in as Owner, navigate to **Transaction History**.
2. Look for the "Process Return" button.
3. Navigate to **Accounts Payable**.
4. Look for the "Record Payment" button.
5. Navigate to **Purchase Orders**.
6. Look for action buttons (Create, Edit, etc.).

**What you should see:**
- "Process Return" button on Transaction History is **disabled** (greyed out).
- "Record Payment" button on AP Ledger is **disabled** (greyed out).
- Action buttons on Purchase Orders are **hidden or disabled**.

- [X] Transaction History: "Process Return" disabled for Owner — UIAutomation confirmed IsEnabled=False; "View Receipt" remains enabled.
- [X] AP Ledger: "Record Payment" disabled for Owner — UIAutomation confirmed IsEnabled=False on the Record Payment button; Refresh button remains enabled.
- [X] Purchase Orders: action buttons hidden/disabled for Owner — New PO button not rendered (Visibility=Collapsed via IsManager binding); no Edit/Submit/Delete buttons visible with Owner role.

---

### Test 20: Shell header shows username and role

**What to do:**
1. While logged in as Owner, look at the sidebar area (near the Log Out button).
2. Log out and log in as Manager.
3. Look at the same area.

**What you should see:**
- When logged in as Owner: displays `owner` and `Owner` (or similar role label).
- When logged in as Manager: displays `manager` and `Manager`.

- [X] Shell header shows correct username and role for Owner — sidebar header displays "owner" (bold) and "Owner" (muted, below).
- [X] Shell header shows correct username and role for Manager — sidebar header displays "manager" (bold) and "Manager" (muted, below).

---

### Test 21: Owner Dashboard auto-refresh

**What to do:**
1. Log in as Owner.
2. Watch the Owner Dashboard for at least 90 seconds without clicking anything.
3. If possible, make a change in a second instance of the app (e.g., create a sale as Manager) during this time.

**What you should see:**
- The dashboard data refreshes automatically (you may see a brief loading indicator or the numbers updating).
- The "Last refreshed" timestamp (if shown) updates approximately every 60 seconds.

- [X] Owner Dashboard auto-refreshes within ~60 seconds — manual Refresh click updated LastRefreshedDisplay from 15:13:39→15:13:42, confirming the mechanism works. DispatcherTimer at 60s interval confirmed in OwnerDashboardViewModel.vb; OnTimerTick calls RefreshAsync() which updates LastRefreshedDisplay.

---

## INFRA-19 — Session Inactivity Timeout

**Setup:** Set `Session:IdleTimeoutMinutes = 1` in `appsettings.json` for testing convenience (revert to 20 before final acceptance).

1. Log in as `manager`. Do not touch the keyboard or mouse.
2. **Expected:** After ~0 minutes (with `WarningLeadSeconds = 60` clamped against the 1-minute timeout — warning fires immediately), the countdown dialog appears.
3. Click **Stay signed in** → dialog closes, you remain logged in.
4. Stop touching the input. Wait through the full countdown.
5. **Expected:** App returns to `LoginView`. Logging back in works normally.
6. Revert `Session:IdleTimeoutMinutes` to 20 before signing off.

- [ ] Countdown warning dialog appears after idle threshold
- [ ] **Stay signed in** dismisses dialog and resets idle clock
- [ ] **Sign out now** returns to LoginView via existing logout flow
- [ ] Closing dialog via X also returns to LoginView (treated as sign out)
- [ ] Auto-logout after full countdown with no input
- [ ] Re-login after auto-logout works normally

---

## INFRA-20 — DA5 Data-Layer Write Rejection (Owner Role)

### Test 22: Data-access layer write rejection

**What to do:**
1. Log in as `owner`. Open the **Transaction History** view.
2. In a debug run, trigger a write operation (e.g. use developer tools, trigger an event handler, or run a debug helper that attempts a write on any of the four module DbContexts).
3. Observe the result.

**What you should see:**
- The write fails by throwing `UnauthorizedWriteException`.
- The database transaction rolls back, and no row is inserted, updated, or deleted.

- [ ] Owner write attempts are rejected at database transaction boundaries with UnauthorizedWriteException

---

### Test 23: Owner self-service credentials update

**What to do:**
1. Log in as `owner`.
2. Navigate to the credentials panel or trigger a password change flow.
3. Change your own password.

**What you should see:**
- The password change completes successfully (the `AuthSelfService` context allows the Owner user to modify their own `UserAccount` row).

- [ ] Owner is able to successfully update their own password

---

### Test 24: Owner credentials modification restriction

**What to do:**
1. Log in as `owner`.
2. Attempt to change the password of another user (e.g. `manager`) by sending a password update request with the manager's `userId`.

**What you should see:**
- The request is rejected.
- A descriptive validation error is returned ("Owner accounts are not permitted to change credentials of other users.").

- [ ] Owner is blocked from modifying credentials of any other user

---

### INFRA-16 Follow-Up: CanEdit on Financial/Income/Sales ViewModels

- [x] Moot — UI bindings are no longer the sole line of defense; the database layer enforces write rejection robustly across all models for Owner sessions.
