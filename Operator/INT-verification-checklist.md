---
module: Integration
source: Integration-audit-2026-05-17.md
generated: 2026-05-17
---

# Operator Verification Checklist — Integration

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 13 Integration plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
> INT-12 items are also tracked in the dedicated [INT-12-checklist.md](INT-12-checklist.md) file.

### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| TransactionHistoryView (XAML) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.xaml.vb` |
| TransactionHistoryViewModel | `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\TransactionHistoryViewModel.vb` |
| FinancialOverviewView (code-behind) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml.vb` |
| FinancialOverviewVatExtension | `WPF_Applications\MerchSys\src\MerchSys.Accounting\ViewModels\Extensions\FinancialOverviewVatExtension.vb` |
| MainWindowViewModel (navigation) | `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` |
| INT-10 summary (for checkbox flip) | `Progress\VISTA_Modules\Integration\INT-10-summary.md` |
| EF Core bug notes | `LLM_Wiki\agent_wiki\errors\efcore10-vbnet-migration-discovery-bug.md` |

---

## INT-10 — Runtime Verification & Smoke Testing

### Test 1: GoodsReceived event chain (live test)

**What to do:**
1. Launch the app (F5).
2. Go to the **Purchasing** section in the sidebar.
3. Create a new Purchase Order (or use an existing one that is approved but not yet received).
4. Go to the **Goods Receiving** screen.
5. Receive the goods for that Purchase Order (mark items as received and save).
6. Open DB Browser for SQLite.
7. Open the database at `%LOCALAPPDATA%\MerchSys\merchsys.db`.
8. Run this query:
   ```sql
   SELECT * FROM Inv_StockMovements WHERE Type = 'Receipt' ORDER BY Id DESC LIMIT 5;
   ```

**What you should see:**
- A new row appears in `Inv_StockMovements` with `Type = 'Receipt'`.
- The row's product and quantity match what you just received.
- This proves the full event chain works: Goods Receiving → `GoodsReceivedEvent` → Handler → Stock Service → Database.

- [ ] GoodsReceived chain: receiving goods creates a StockMovement row with Type=Receipt

---

### Test 2: SaleCompleted event chain (live test)

**What to do:**
1. Launch the app (F5).
2. Go to the **POS** section (Sales Cart) in the sidebar.
3. Add one or more products to the cart.
4. Complete the sale (process payment and finish).
5. Open DB Browser for SQLite.
6. Open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
7. Run this query:
   ```sql
   SELECT * FROM Inv_StockMovements WHERE Type = 'Sale' ORDER BY Id DESC LIMIT 5;
   ```

**What you should see:**
- A new row appears in `Inv_StockMovements` with `Type = 'Sale'`.
- The row's product and quantity match what you just sold.
- This proves the full event chain works: Sale → `SaleCompletedEvent` → Handler → Stock Service → Database.

- [ ] SaleCompleted chain: completing a sale creates a StockMovement row with Type=Sale

---

### ⏳ Ongoing: EF Core VB.NET CLI bug

**What to do:**
- Nothing. This is a known bug in EF Core's VB.NET migration discovery.
- Tracked in: `LLM_Wiki\agent_wiki\errors\efcore10-vbnet-migration-discovery-bug.md`
- No action needed until Microsoft releases a fix.

- [ ] ⏳ Ongoing — monitoring for upstream EF Core fix

---

## INT-11 — IEventBus DI Registration Gap

### Test 3: TransactionHistoryView works correctly

**What to do:**
1. Launch the app (F5).
2. Navigate to the **Transaction History** view (in the POS section of the sidebar).
3. Look at the view carefully. Check that:
   - The view opens without crashing.
   - All columns show data correctly.
   - The text labels are styled properly (no raw `Run` elements or broken XAML).

> **View file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.xaml.vb`
> **ViewModel:** `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\TransactionHistoryViewModel.vb`

**What you should see:**
- The view loads and displays transaction history data.
- No visual glitches or unstyled text elements.
- No errors in the Visual Studio Output window.

- [ ] TransactionHistoryView displays correctly after XAML fix

---

### Test 4: All 16 navigation views pass (then update docs)

**What to do:**
1. After completing Test 3 above, check if ALL 16 views in the app navigate and display correctly.
2. The list of 16 views is in:
   `Progress\VISTA_Modules\Integration\INT-10-summary.md`
3. If all 16 views pass:
   - Open `Progress\VISTA_Modules\Integration\INT-10-summary.md`
   - Find the interactive-navigation checkbox item that currently shows `[/]`.
   - Change it to `[x]`.

**What you should see:**
- Every view in the sidebar opens without errors and shows data.

- [ ] All 16 views pass → INT-10 summary checkbox updated to `[x]`

---

## INT-12 — Runtime Event Chain Verification

> These items overlap with the [INT-12-checklist.md](INT-12-checklist.md). You can use either file to track your results.

### Test 5: Run the event chain verification harness

**What to do:**
1. Build the solution in **Debug** configuration (Ctrl+Shift+B).
2. Press **F5** to launch the app.
3. Find the debug tool that runs the `EventChainVerificationHarness`. (Check the Developer menu or debug tools section.)
4. Click the button to run the harness.
5. Wait for it to finish.

**What you should see:**
- A toast notification appears with a file path to a Markdown report.
- Open the report.
- Both chains (GoodsReceived and SaleCompleted) show `Passed = True`.

- [ ] Event chain harness reports Passed = True for both chains

---

### Test 6: TransactionHistoryView re-test (from INT-12 checklist)

**What to do:**
- Same as Test 3 above. If you already did Test 3, you can mark this as done.

- [ ] TransactionHistoryView re-test (same as Test 3)

---

### Test 7: Flip INT-10 checkbox (from INT-12 checklist)

**What to do:**
- Same as Test 4 above. If all views passed and you updated the doc, mark this as done.
- File to edit: `Progress\VISTA_Modules\Integration\INT-10-summary.md`

- [ ] INT-10 checkbox flipped (same as Test 4)

---

## INT-13 — VatReturnView Navigation Wire-up

### Test 8: Verify type-based navigation pattern

**What to do:**
1. Open this file in Visual Studio:
   `WPF_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.xaml.vb`
2. Find the `NavigateToVatReturnRequested` event handler method (use Ctrl+F to search).
3. Read the code inside.

**What you should see:**
- The handler resolves a `NavigationItem` **by type** (like `GetNavigationItem(Of VatReturnView)()` or similar).
- It does **NOT** use a string like `NavigateCommand("VatReturn")`.
- This confirms ACC-14 followed the correct pattern required by INT-13.

- [ ] VatReturnView navigation uses type-based resolution, not string key
