---
module: Accounting
source: Accounting-audit-2026-05-17.md
generated: 2026-05-17
---

# Operator Verification Checklist — Accounting

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 18 Accounting plans are completed. These are the remaining acceptance tests.
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
| Database config | `WPF_Applications\MerchSys\src\MerchSys.App\Data\DatabaseConfig.vb` |
| FinancialOverviewView (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml` |
| FinancialOverviewView (code-behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml.vb` |
| VatPayableTile | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\Components\VatPayableTile.xaml.vb` |
| FinancialOverviewVatExtension | `WPF_Applications\MerchSys\src\MerchSys.Accounting\ViewModels\Extensions\FinancialOverviewVatExtension.vb` |
| Concurrency harness | `WPF_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb` |

---

## ACC-10 — VAT Ledger Schema Extension

### Test 1: Fresh database migration

**What to do:**
1. Go to `%LOCALAPPDATA%\MerchSys\` in File Explorer.
2. Rename `merchsys.db` to `merchsys.db.bak` (this is your backup).
3. Open the solution in Visual Studio and press **F5** to launch the app in Debug mode. The app will create a new database on startup.
4. Wait for the app to fully load.

**What you should see:**
- The app starts without errors.
- No crash or migration error messages appear.
- A new `merchsys.db` file appears in `%LOCALAPPDATA%\MerchSys\`.

- [X] Fresh database migration works

---

### Test 2: Existing database migration

**What to do:**
1. If you backed up in Test 1, restore it: delete the new `merchsys.db` and rename `merchsys.db.bak` back to `merchsys.db`.
2. Press **F5** to launch the app in Debug mode.

**What you should see:**
- The app starts without errors.
- No crash or migration error messages appear.
- Your old data is still there.

- [X] Existing database migration works

---

### Test 3: Duplicate VAT return filing is blocked

**What to do:**
1. Open DB Browser for SQLite (or similar tool).
2. Open the database file at `%LOCALAPPDATA%\MerchSys\merchsys.db`.
3. Find the `Acc_VatReturns` table.
4. Try to insert two rows that have the **same** `PeriodStart`, `PeriodEnd`, and `FormType` values.

**What you should see:**
- The first row inserts fine.
- The second row fails with a unique constraint error.

- [X] Duplicate VAT return filing is blocked

---

### Test 4: Deleting a VAT return also deletes its lines

**What to do:**
1. Open DB Browser for SQLite.
2. Open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
3. Find a row in `Acc_VatReturns` that has child rows in `Acc_VatReturnLines`.
4. Note the `Id` of the VAT return.
5. Delete that VAT return row.
6. Check `Acc_VatReturnLines` for rows with that same `VatReturnId`.

**What you should see:**
- All the child rows in `Acc_VatReturnLines` are automatically deleted when you delete the parent.

- [X] Cascade delete from VatReturn to VatReturnLines works

---

## ACC-12 — VAT Payable KPI Tile

### Test 5: VAT tile shows correct amount

**What to do:**
1. Make sure there is VAT ledger data in the database for the current month. (If there is none, complete a few sales first, or seed data manually in `Acc_VatReturnLines`.)
2. Launch the app and log in as `manager` (use your changed password, or `Vista2026!` if first run — you will be prompted to change it).
3. Navigate to the **Financial Overview** screen.
4. Find the VAT Payable tile among the KPI cards.

> **View file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml`
> **Tile file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\Components\VatPayableTile.xaml.vb`

**What you should see:**
- The tile displays a peso amount that matches the sum of VAT for the current period.
- The tile colour changes based on how close the BIR deadline is:
  - Green = plenty of time remaining
  - Yellow/Orange = deadline is approaching
  - Red = deadline is very close or past

- [X] VAT tile shows correct amount and colour — "Percentage Tax" ₱306.00 Due Jul 25, white border (Info severity, deadline 65+ days away)

---

## ACC-13 — VAT Ledger Schema Verification Harness

### Test 6: Run the schema harness from the developer menu

**What to do:**
1. Build the solution in **Debug** configuration (Ctrl+Shift+B).
2. Press **F5** to launch the app.
3. In the menu bar, click **Developer Tools** (or look for a "Dev" menu).
4. Click **Run VAT Schema Harness** (or similar button text).
5. Wait a few seconds for it to finish.

**What you should see:**
- A message box pops up telling you the harness is done.
- A `.md` (Markdown) report file appears in your `%TEMP%` folder (usually `C:\Users\<you>\AppData\Local\Temp\`).
- Open that report. It should show all four checks with a **Pass** status.

- [ ] Schema harness runs and all four checks pass

---

## ACC-14 + ACC-16 — VAT Tile in Financial Overview (combined)

> ACC-14 and ACC-16 have the same acceptance tests. Completing these once covers both plans.

### Test 7: Manager can see and click the VAT tile

**What to do:**
1. Press **F5** to launch the app.
2. Log in with username `manager` and your password.
3. Navigate to the **Financial Overview** screen.
4. Look for the VAT Payable tile (it should be among the KPI cards at the top).
5. Click the VAT Payable tile.

> **Navigation handler:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml.vb` — look for `NavigateToVatReturnRequested`

**What you should see:**
- The tile is visible on the Financial Overview.
- Clicking it takes you to the **VAT Return View** screen.

- [X] Manager sees VAT tile and clicking it opens VatReturnView — fixed: Application.Current.MainWindow was LoginView (first shown); handler now iterates Application.Current.Windows to find the shell

---

### Test 8: Owner can see the tile but cannot navigate

**What to do:**
1. Launch the app.
2. Log out if currently logged in (click **Log Out** in the sidebar).
3. Log in with username `owner` and your password.
4. Navigate to the **Financial Overview** screen.
5. Look for the VAT Payable tile.
6. Click the VAT Payable tile.

**What you should see:**
- The tile is visible on the Financial Overview.
- Clicking it does **nothing** (no navigation happens). The Owner role is restricted from accessing the VAT Return View.

- [X] Owner sees VAT tile but clicking does nothing — VAT Return nav item absent from Owner nav groups; FirstOrDefault returns Nothing, navigation suppressed

---

### Test 9: VAT tile smoke harness

**What to do:**
1. Build the solution in **Debug** configuration.
2. Press **F5** to launch the app.
3. Open the **Immediate Window** in Visual Studio (Debug → Windows → Immediate).
4. Type: `? Await VatTileSmokeHarness.RunAsync(host)` and press Enter.
5. Read the output.

> **VAT extension logic:** `WPF_Applications\MerchSys\src\MerchSys.Accounting\ViewModels\Extensions\FinancialOverviewVatExtension.vb`

**What you should see:**
- `ComputedVatPayable = 9000` (or whatever the expected test value is)
- `NavigationRouteFound = True`

- [ ] VatTileSmokeHarness passes with expected values

---

## ACC-17 — Schema Harness Dev-Menu Button

> This overlaps with Test 6 above. If you already did Test 6, just confirm the button exists.

### Test 10: Dev menu button exists and works

**What to do:**
1. Build in **Debug** configuration and launch the app (F5).
2. Go to **Developer Tools** in the menu.
3. Click the **Run VAT Schema Harness** button.

**What you should see:**
- A `MessageBox` pops up confirming the harness ran.
- A `.md` report file appears in `%TEMP%`.

- [ ] Dev menu button exists and produces harness report
