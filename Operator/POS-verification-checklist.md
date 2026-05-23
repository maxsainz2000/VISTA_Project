---
module: POS
source: POS-audit-2026-05-17.md
generated: 2026-05-17
---

# Operator Verification Checklist — POS

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 18 POS plans are completed. These are the remaining acceptance tests.
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
| DailySummaryView (code-behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\DailySummaryView.xaml.vb` |
| DailySummaryViewModel | `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\DailySummaryViewModel.vb` |
| Concurrency harness | `WPF_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb` |
| ReceiptArchivalService | `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb` |
| ReceiptArchivalOptions | `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb` |
| VatSettingsView (code-behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\VatSettingsView.xaml.vb` |
| VatSettingsViewModel | `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\VatSettingsViewModel.vb` |
| VatConfigurationLoader | `WPF_Applications\MerchSys\src\MerchSys.POS\Services\VatConfigurationLoader.vb` |
| VatConfigurationChangedEvent | `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Events\VatConfigurationChangedEvent.vb` |
| IVatConfigurationWriter | `WPF_Applications\MerchSys\src\MerchSys.POS\Services\IVatConfigurationWriter.vb` |
| POS service registration (DI) | `WPF_Applications\MerchSys\src\MerchSys.App\Startup\PosServiceRegistration.vb` |

---

## POS-12 — View — Daily Summary

### Test 1: Daily Summary view works end-to-end

**What to do:**
1. Launch the app (F5).
2. Complete at least one sale so there is data for today.
3. Navigate to the **Daily Summary** view (in the POS section of the sidebar).
4. Look at the summary data displayed.

> **View file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\DailySummaryView.xaml.vb`
> **ViewModel:** `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\DailySummaryViewModel.vb`

**What you should see:**
- The view opens without crashing.
- Today's sales data appears (total sales, number of transactions, etc.).
- The numbers look correct based on the sales you just made.

- [x] Daily Summary view loads and shows correct data — View opened, KPIs correct, Top 5 products aggregated correctly. Fixed 2 bugs: ImmutableEntityException on PAY (double GenerateReceiptAsync call) and Top 5 grouping (missing Key keyword on VB.NET anonymous type).

---

## POS-13 — BIR Tamper-Proof Receipt Retention & Sequence

### Test 2: Receipt sequence concurrency harness

**What to do:**
1. Open Visual Studio with the solution loaded.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Open the **Immediate Window** (Debug → Windows → Immediate).
5. Type the following and press Enter:
   ```
   ? Await Pos_SequenceConcurrencyHarness.RunAsync()
   ```
6. Wait for it to finish (this may take a few seconds).

> **Harness file:** `WPF_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb`

**What you should see:**
- The harness runs multiple threads trying to reserve receipt sequence numbers at the same time.
- The output should show:
  - **No duplicate** sequence numbers were assigned.
  - **No gaps** in the sequence.
- If you see duplicates or gaps, there is a concurrency bug.

- [x] Concurrency harness: no duplicates and no gaps in receipt sequence — [PASS] 1000 numbers generated, 1000 unique, contiguous sequence confirmed. Fixed 3 harness bugs: missing no-arg overload (Immediate Window unsupported), EnsureCreatedAsync race (called 1000× in parallel), WAL file lock on temp DB cleanup.

---

## POS-15 — Receipt Numbering Integration & Concurrency Validation

### Test 3: Receipt sequence harness report

**What to do:**
1. Open Visual Studio with the solution loaded.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Open the **Immediate Window** (Debug → Windows → Immediate).
5. Type the following and press Enter:
   ```
   ? Await ReceiptSequenceHarnessReport.RunAndReportAsync()
   ```
6. Wait for it to finish.

**What you should see:**
- A report with four key numbers:
  - `Duplicates = 0` (no duplicate sequence numbers)
  - `Gaps = 0` (no missing numbers in the sequence)
  - `TotalReservations = 800` (the harness reserved 800 numbers)
  - `Elapsed` = a reasonable time (a few seconds is normal)

- [x] Receipt harness report: Duplicates=0, Gaps=0, TotalReservations=800 — PASS. TotalReservations=800, Duplicates=0, Gaps=0, ElapsedMs=2383. Wired to Dev menu button (Immediate Window blocked by VS hot-reload).

---

## POS-16 — Receipt Archival Background Service

> This section has 6 tests. They all involve the receipt archival system that moves old receipts to archive tables.
>
> **Service file:** `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb`
> **Options file:** `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb`

### Test 4: Archival moves the right number of receipts

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Seed test data: insert 100 receipts into `Pos_OfficialReceipts` where `RetentionExpiresAt` is in the past (expired) AND they are from a **previous** fiscal year. Also insert 100 receipts where `RetentionExpiresAt` is still in the future (in-window).
3. Trigger the archival service (launch the app and wait for the background service to run, or trigger it manually from code).
4. Check the `Pos_OfficialReceiptArchive` table.

**What you should see:**
- Exactly 100 receipts were moved to the archive table (the expired ones).
- The 100 in-window receipts remain in `Pos_OfficialReceipts`.

- [x] Archival moves exactly 100 expired receipts, leaves 100 in-window untouched — PASS. ReceiptsMoved=100, LiveRemaining=100, ArchiveCount=100, ElapsedMs=1204. EF Core ToListAsync anonymous-type projection materialized correctly. No production code change needed; harness wired to Dev menu button.

---

### Test 5: Batch size flag works correctly

**What to do:**
1. Keep the same test data from Test 4 (or re-seed 100 expired receipts).
2. Change the batch size to `batchSize = 50`. You can do this by editing:
   `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb`
   or setting it in the app configuration.
3. Trigger the archival service once.

**What you should see:**
- Only 50 receipts are moved in this batch.
- The archival result shows `HadMoreEligible = True` (meaning there are more receipts waiting to be archived in the next batch).

- [x] Batch size of 50: only 50 moved, HadMoreEligible = True — PASS. ReceiptsMoved=50, HadMoreEligible=True, LiveRemaining=50, ArchiveCount=50, ElapsedMs=1101. No production code change needed.

---

### Test 6: Fiscal-year guard prevents premature archival

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Insert some receipts into `Pos_OfficialReceipts` where `RetentionExpiresAt` is in the past BUT `IssuedAt` is in the **current fiscal year** (i.e., this year, 2026).
3. Trigger the archival service.

**What you should see:**
- Those receipts are **NOT** archived, even though their retention has "expired".
- The BIR rule is: receipts from the current fiscal year must never be archived regardless of the retention date.

- [ ] Current-year receipts are NOT archived even if RetentionExpiresAt is past

---

### Test 7: Rollback on archive failure

**What to do:**
1. Set up a test where the archive-insert step will fail. One way: temporarily rename the `Pos_OfficialReceiptArchive` table in the database so the insert fails.
2. Trigger the archival service.
3. Check the `Pos_OfficialReceipts` table (the live table).

**What you should see:**
- The live table is untouched — no receipts were deleted.
- The failure is rolled back cleanly. No data is lost.

- [ ] Forced archive failure: live table remains intact (rollback works)

---

### Test 8: BIR immutability trigger blocks direct deletes

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Try to run this SQL directly:
   ```sql
   DELETE FROM Pos_OfficialReceipts WHERE Id = <pick any id>;
   ```

**What you should see:**
- The DELETE fails with a `BIR-immutable` trigger error.
- This proves the immutability trigger is working — you cannot manually delete official receipts.

- [x] Direct DELETE from Pos_OfficialReceipts fails with BIR-immutable error — PASS. `DELETE FROM Pos_OfficialReceipts WHERE Id = 211` returned `Error: BIR-immutable` (exit code 1). Trigger `pos_receipts_no_delete` is active and working.

---

### Test 9: Archive tables reject UPDATE and DELETE

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Try these SQL commands on the archive tables:
   ```sql
   UPDATE Pos_OfficialReceiptArchive SET Status = 'test' WHERE Id = <pick any id>;
   ```
   ```sql
   DELETE FROM Pos_OfficialReceiptArchive WHERE Id = <pick any id>;
   ```
3. Do the same for `Pos_ReceiptIntegrityArchive`.

**What you should see:**
- Both UPDATE and DELETE fail on both archive tables.
- Archive records are permanent and cannot be changed or removed.

- [x] Pos_OfficialReceiptArchive rejects UPDATE and DELETE — PASS. UPDATE and DELETE both returned `Error: BIR-archive-immutable` (exit code 1). Triggers `pos_receipt_archive_no_update` and `pos_receipt_archive_no_delete` confirmed working.
- [x] Pos_ReceiptIntegrityArchive rejects UPDATE and DELETE — PASS. UPDATE and DELETE both returned `Error: BIR-archive-immutable` (exit code 1). Triggers `pos_integrity_archive_no_update` and `pos_integrity_archive_no_delete` confirmed working.

---

## POS-17 — VAT Settings UI

### Test 10: Load, save, reload round-trip

**What to do:**
1. Launch the app (F5).
2. Log in with username `manager` and your password.
3. Navigate to **VAT Settings** in the sidebar.
4. Note the current values displayed (VAT rate, registration status, etc.).
5. Change one value (for example, toggle the VAT registration checkbox or change the rate).
6. Click **Save**.
7. Navigate away to a different screen.
8. Navigate back to **VAT Settings**.

> **View file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\VatSettingsView.xaml.vb`
> **ViewModel:** `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\VatSettingsViewModel.vb`

**What you should see:**
- The new values you saved are still there after navigating away and back.
- The old values are gone — the save actually persisted.

- [ ] VAT Settings: load → change → save → reload shows saved values

---

### Test 11: VatConfigurationLoader returns updated values

**What to do:**
1. After doing Test 10, open the **Immediate Window** in Visual Studio (while the app is still running in Debug).
2. Type:
   ```
   ? Await VatConfigurationLoader.GetAsync()
   ```
3. Check the returned values.

> **Loader file:** `WPF_Applications\MerchSys\src\MerchSys.POS\Services\VatConfigurationLoader.vb`

**What you should see:**
- The values match what you just saved in the UI (not the old values).

- [ ] VatConfigurationLoader.GetAsync() returns the newly saved values

---

### Test 12: VatConfigurationChangedEvent is published

**What to do:**
1. Open this file in Visual Studio:
   `WPF_Applications\MerchSys\src\MerchSys.POS\Services\IVatConfigurationWriter.vb`
   (This is where the event gets published after saving.)
2. Find where `VatConfigurationChangedEvent` is created/published. Set a breakpoint on that line.
3. You can also check the event definition at:
   `WPF_Applications\MerchSys\src\MerchSys.SharedKernel\Events\VatConfigurationChangedEvent.vb`
4. Press **F5** to launch the app in Debug mode.
5. Go to VAT Settings and change the VAT registration status (toggle it on or off).
6. Click Save.

**What you should see:**
- The breakpoint gets hit.
- The event's `PreviousIsVatRegistered` property matches the **old** value (before you changed it).
- The current value matches the **new** value you just set.

- [ ] VatConfigurationChangedEvent fires with correct PreviousIsVatRegistered

---

### Test 13: VAT Settings visibility by role

**What to do:**
1. Launch the app and log in with username `manager` and your password.
2. Look at the sidebar navigation.
3. Click the **Log Out** button in the sidebar.
4. Log in with username `owner` and your password.
5. Look at the sidebar navigation again.

> **Navigation config:** `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` — search for `VatSettings` to see how role visibility is configured.

**What you should see:**
- **Manager** sees "VAT Settings" in the sidebar.
- **Owner** does **NOT** see "VAT Settings" in the sidebar.

- [ ] Manager sees "VAT Settings" in sidebar
- [ ] Owner does NOT see "VAT Settings" in sidebar
