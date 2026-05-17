---
module: Infrastructure
source: Infrastructure-audit-2026-05-17.md
generated: 2026-05-17
---

# Operator Verification Checklist — Infrastructure

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 14 Infrastructure plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.

### Key file locations

| What | Path |
|------|------|
| Production config template | `WPF_Applications\MerchSys\src\MerchSys.App\appsettings.Production.template.json` |
| Where to put the real config | `%LOCALAPPDATA%\VISTA\appsettings.Production.json` |
| MariaDB init SQL | `Plans\VISTA_Modules\Infrastructure\sql\mariadb-init.sql` |
| Receipt schema alignment SQL | `Plans\VISTA_Modules\Infrastructure\sql\mariadb-receipt-schema-alignment.sql` |
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

- [ ] Production password file created and excluded from git

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

- [ ] MariaDB init SQL runs clean on a fresh instance

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

- [ ] Sync indicator renders in status bar with no binding errors

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

- [ ] Dispose breakpoint is hit on app shutdown

---

## INFRA-11 — Production Deployment Configuration

### ⏳ Deferred: Pomelo 10.x upgrade

**What to do:**
- Nothing right now. The Pomelo.EntityFrameworkCore.MySql package version 10.x is not yet published on NuGet.

**When to do it:**
- Check NuGet periodically for a stable `Pomelo.EntityFrameworkCore.MySql` 10.x release.
- Once available, update the package reference and remove the `NU1608` suppression from:
  `WPF_Applications\MerchSys\Directory.Build.props`

- [ ] ⏳ Deferred — waiting for Pomelo 10.x on NuGet

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

- [ ] Live MariaDB deployment walkthrough completed

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

- [ ] TransmitBatchAsync handles duplicate batches without errors

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

- [ ] Writing through a migrated service creates a Sync_Journal row

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

- [ ] Schema alignment SQL runs twice without errors

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

- [ ] Receipt row syncs to MariaDB with Status, IssuedAt, and IntegrityHash filled in
