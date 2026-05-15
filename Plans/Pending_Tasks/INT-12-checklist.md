---
plan-id: INT-12
generated: 2026-05-15
---

# Runtime Event Chain Verification — Checklist

## TransactionHistoryView re-test

- [ ] View navigates without exception
- [ ] All columns render
- [ ] Operator: <name>
- [ ] Result attached: yes/no

## GoodsReceived chain (from harness)

- [ ] Publisher fired (`GoodsReceivedEvent` flag set by probe handler)
- [ ] Handler executed (`GoodsReceivedHandler` ran → `StockService.AddStockBatchAsync`)
- [ ] StockMovement row present with `Type=Receipt` in `Inv_StockMovements`

## SaleCompleted chain (from harness)

- [ ] Publisher fired (`SaleCompletedEvent` flag set by probe handler)
- [ ] Handler executed (`SaleCompletedHandler` ran → `StockService.DeductStockFIFOAsync`)
- [ ] StockMovement row present with `Type=Sale` in `Inv_StockMovements`

## INT-10 checkbox flip

- [ ] After all three sections above pass, edit `Progress/VISTA_Modules/Integration/INT-10-summary.md`:
  change the interactive-navigation item from `[/]` → `[x]`

## How to run the harness

1. Build the solution in Debug configuration (`dotnet build --configuration Debug`).
2. Launch the application and navigate to the debug tool that invokes
   `EventChainVerificationHarness` (wire a button in `MainWindow` or a debug menu if not already
   present).
3. The harness runs both chains sequentially against a scratch SQLite file under `%TEMP%`.
4. A `Notification.Wpf` toast appears with the path to the Markdown report.
5. Open the report and fill in the checkboxes above based on the results.
6. If all boxes pass, flip the INT-10 checkbox per the instructions above.
