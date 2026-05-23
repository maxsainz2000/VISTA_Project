---
plan-id: INT-12
generated: 2026-05-17
---

# Runtime Event Chain Verification — Checklist

> This checklist is for INT-12 specifically. The main Integration checklist is in [INT-verification-checklist.md](INT-verification-checklist.md).
>
> **How to use:** Run the harness first, then fill in what you saw for each section.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| TransactionHistoryView | `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.xaml.vb` |
| TransactionHistoryViewModel | `WPF_Applications\MerchSys\src\MerchSys.POS\ViewModels\TransactionHistoryViewModel.vb` |
| INT-10 summary (for checkbox flip) | `Progress\VISTA_Modules\Integration\INT-10-summary.md` |
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |

---

## How to run the harness

1. Open the solution in Visual Studio.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Find the debug tool that runs `EventChainVerificationHarness`. (Look in the Developer menu or a debug tools area.)
5. Click the button to run the harness.
6. The harness runs both event chains against a temporary SQLite file in your `%TEMP%` folder (usually `C:\Users\<you>\AppData\Local\Temp\`).
7. When it finishes, a toast notification appears with the path to a Markdown report.
8. Open that report and use it to fill in the sections below.

---

## TransactionHistoryView re-test

**What to do:**
1. While the app is still running, navigate to the **Transaction History** view (in the POS section of the sidebar).
2. Check that it opens and looks correct.

> **View file:** `WPF_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.xaml.vb`

**What you should see:**
- The view opens without crashing.
- All columns display data.
- No unstyled text or broken layout.

- [ ] View navigates without exception
- [ ] All columns render correctly
- [ ] Operator: *(write your name here)*
- [ ] Result attached: yes / no

---

## GoodsReceived chain (from harness)

**What you should see in the harness report:**
- The `GoodsReceivedEvent` was published (the probe handler set a flag).
- The `GoodsReceivedHandler` ran and called `StockService.AddStockBatchAsync`.
- A row exists in `Inv_StockMovements` with `Type = Receipt`.

To verify manually in the database, open `%LOCALAPPDATA%\MerchSys\merchsys.db` and run:
```sql
SELECT * FROM Inv_StockMovements WHERE Type = 'Receipt' ORDER BY Id DESC LIMIT 5;
```

- [ ] Publisher fired — `GoodsReceivedEvent` flag was set by probe handler
- [ ] Handler executed — `GoodsReceivedHandler` ran and called `AddStockBatchAsync`
- [ ] StockMovement row present with `Type=Receipt` in `Inv_StockMovements`

---

## SaleCompleted chain (from harness)

**What you should see in the harness report:**
- The `SaleCompletedEvent` was published (the probe handler set a flag).
- The `SaleCompletedHandler` ran and called `StockService.DeductStockFIFOAsync`.
- A row exists in `Inv_StockMovements` with `Type = Sale`.

To verify manually in the database, open `%LOCALAPPDATA%\MerchSys\merchsys.db` and run:
```sql
SELECT * FROM Inv_StockMovements WHERE Type = 'Sale' ORDER BY Id DESC LIMIT 5;
```

- [ ] Publisher fired — `SaleCompletedEvent` flag was set by probe handler
- [ ] Handler executed — `SaleCompletedHandler` ran and called `DeductStockFIFOAsync`
- [ ] StockMovement row present with `Type=Sale` in `Inv_StockMovements`

---

## INT-10 checkbox flip

**What to do:**
1. Only do this after **all three sections above pass**.
2. Open this file:
   `Progress\VISTA_Modules\Integration\INT-10-summary.md`
3. Find the interactive-navigation line that currently shows `[/]` (partially done).
4. Change it to `[x]` (fully done).
5. Save the file.

- [ ] All sections above passed → INT-10 summary checkbox changed from `[/]` to `[x]`
