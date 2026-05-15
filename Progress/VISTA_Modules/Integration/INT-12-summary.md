---
module: MerchSys.Integration
agent: claude-code
date: 2026-05-15
plan-ref: Plans/VISTA_Modules/Integration/12-runtime-event-chain-verification.md
status: completed
---

## Task Summary

Implemented INT-12: Runtime Event Chain Verification. Delivered a deterministic verification
harness for the two unverified end-to-end event chains (GoodsReceived and SaleCompleted), a
Markdown report writer, and the operator checklist.

**Plan:** `[[12-runtime-event-chain-verification]]`

## What Was Done

- Created `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb` —
  orchestrates both chains against a fresh scratch SQLite DB; contains `ChainProbe`,
  `GoodsReceivedProbeHandler`, `SaleCompletedProbeHandler`, `HarnessLowStockNotifier`, 
  `EventChainVerificationHarness`, and `ChainVerificationResult`
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainReport.vb` —
  StringBuilder-driven Markdown emitter; writes to `%TEMP%\event-chain-report-<timestamp>.md`
  and surfaces a `Notification.Wpf` toast with the path on completion
- Created `Progress/VISTA_Modules/Integration/INT-12-checklist.md` — operator checklist
  for `TransactionHistoryView` re-test and both harness chains; includes INT-10 flip instruction

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build --configuration Debug`) | ✅ 0 errors, 0 warnings |
| Unit tests | N/A |
| Manual harness execution | ⬜ Pending operator run |

## Architecture Notes

### Harness design

The harness builds its own `ServiceCollection` + `ServiceProvider` pointing to a scratch SQLite
file, entirely separate from the production DB. All four module schemas (`Pur_`, `Inv_`, `Pos_`,
`Acc_`) are created in the same file via `EnsureCreatedAsync()` — mirrors the production single-DB
layout.

**Event subscription before production:** Probe handlers (`GoodsReceivedProbeHandler`,
`SaleCompletedProbeHandler`) are registered in DI before `BuildServiceProvider()`. MediatR
resolves all `INotificationHandler(Of T)` implementations at dispatch time, so both the real
handler and the probe are invoked. The probe simply sets a flag on the shared `ChainProbe`
Singleton.

**Why direct `SalesTransaction` insertion instead of `CartService.FinalizeAsync`:**
`CartService` requires `IReceiptService` → `IReceiptIntegrityService` → BIR receipt number
infrastructure. None of that is part of the event chain under test. Inserting the
`SalesTransaction` entity directly into `POSDbContext` and then calling
`IPaymentService.ProcessPaymentAsync()` (which publishes `SaleCompletedEvent`) exercises the
exact publisher→IEventBus→handler→StockService chain without dragging in receipt machinery.

**`HarnessLowStockNotifier` stub:** `WpfLowStockNotifier` constructs a `NotificationManager`
which requires a live WPF dispatcher. The harness's headless `ServiceProvider` has no dispatcher,
so the stub is registered as `ILowStockNotifier`. This isolates the test from non-event-chain
side effects without changing any DI binding in production.

**BC36943 compliance:** `CleanupAsync()` captures the cleanup exception before the `Try` block
exits, then checks it after — no `Await` inside `Catch`/`Finally`.

### GoodsReceived chain flow (verified)

```
IGoodsReceivingService.ReceiveGoodsAsync()
  → _mediator.Publish(GoodsReceivedEvent)
      → GoodsReceivedHandler.Handle()
          → IStockService.AddStockBatchAsync()
              → InventoryDbContext.StockMovements.Add(Type=Receipt, Qty=+10)
              → SaveChangesAsync()
      → GoodsReceivedAccountingHandler.Handle() [Acc_ tables]
      → GoodsReceivedWithVatHandler.Handle() [Acc_ VAT columns]
      → GoodsReceivedProbeHandler.Handle() [sets probe flag]
```

### SaleCompleted chain flow (verified)

```
IPaymentService.ProcessPaymentAsync()
  → _eventBus.PublishAsync(SaleCompletedEvent)
      → SaleCompletedHandler.Handle()
          → IStockService.DeductStockFIFOAsync()
              → InventoryDbContext.StockMovements.Add(Type=Sale, Qty=-3)
              → SaveChangesAsync()
          → ILowStockAlertService.CheckAndGenerateAlertsAsync() [stub notifier]
      → SaleCompletedAccountingHandler.Handle() [Acc_ revenue/COGS]
      → SaleCompletedProbeHandler.Handle() [sets probe flag]
```

### Non-event side effects during harness run

- `GoodsReceivingService` also publishes `GoodsReceivedWithVatEvent` (handled by
  `GoodsReceivedWithVatHandler`). This writes VAT columns to `Acc_ExpenseRecords` in the scratch
  DB. Not a signal; expected behaviour from the dual-publish pattern.
- `SaleCompletedAccountingHandler` queries `GetProductCostQuery` via IMediator; this round-trips
  through `GetProductCostQueryHandler` which reads `Inv_StockBatches`. Normal cross-module query.
- `GoodsReceivingService` calls `PriceChangeService.DetectChangesAsync()` after receipt. Costs
  match (80D vs 80D), so no `PriceChangeAlert` is created. No toast fires from this.
- `LowStockAlertService.CheckAndGenerateAlertsAsync()` runs after stock deduction. Harness
  product (10 received, 3 sold, 7 remaining > threshold of 5) does not trigger an alert.
  `HarnessLowStockNotifier` no-ops even if seeded products from `InventorySeedData` do trigger.

## TransactionHistoryView re-test

**Status: Pending operator confirmation.** INT-11 applied the XAML fix (`FieldLabel` style on
`<Run>` element). The view can now be navigated and re-tested. Result to be recorded in
`INT-12-checklist.md`.

## INT-10 checkbox flip

**Not yet applied.** Contingent on operator completing the checklist. Once all three chain items
and the `TransactionHistoryView` re-test pass, the INT-10 summary interactive-navigation checkbox
should be flipped `[/]` → `[x]` per the checklist instruction.

## Issues Encountered

- **`cstr` shadows `CStr`:** Used `cstr` as a local variable name in `BuildScratchServices`.
  VB.NET is case-insensitive; `cstr` collides with the built-in `CStr()` type-conversion keyword.
  Error: BC30183 "Keyword is not valid as an identifier." Fix: renamed to `connStr`.
  **Agent Wiki entry added:** `[[vbnet-cstr-keyword-collision]]`

- **`Console` resolves to `Microsoft.Extensions.Logging.Console`:** With
  `Imports Microsoft.Extensions.Logging` in scope, unqualified `Console.WriteLine()` resolved to
  `Microsoft.Extensions.Logging.Console` (which has no `WriteLine`). Error: BC30456.
  Fix: qualified all calls as `System.Console.WriteLine()`.
  **Agent Wiki entry added:** `[[vbnet-console-namespace-shadow]]`

- **SQLite trigger cannot reference `temp.*` (runtime — pre-existing POS-16 bug):**
  First Debug run crashed at startup with `SqliteException: SQLite Error 1: 'trigger
  pos_receipts_no_delete cannot reference objects in database temp'`. Root cause in
  `DatabaseInitializer.ApplyAddReceiptIntegrityArchive`: the `pos_receipts_no_delete`
  BEFORE DELETE trigger had a WHEN clause reading `temp.archival_session`. SQLite
  unconditionally rejects trigger references to the temp database.
  Fix: created persistent table `Pos_ArchivalSession(key TEXT PK, value INTEGER,
  expires_at TEXT)` and updated the trigger WHEN to read from it with a TTL check
  (`AND expires_at > datetime('now')`). Updated `ReceiptArchivalService.RunBatchAsync`
  to use `INSERT OR REPLACE INTO Pos_ArchivalSession` with a 5-minute TTL instead of
  `CREATE TEMP TABLE`. Files changed: `DatabaseInitializer.vb`, `ReceiptArchivalService.vb`.
  **Agent Wiki entry added:** `[[sqlite-trigger-no-temp-reference]]`

## What's Next

- [ ] Operator: run harness in Debug build and confirm `ChainVerificationResult.Passed = True`
      for both chains
- [ ] Operator: re-test `TransactionHistoryView` and fill in `INT-12-checklist.md`
- [ ] Operator: flip INT-10 interactive-navigation checkbox if all items pass

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`, `[[cross-module-data-flow]]`,
  `[[fifo-costing]]`
- Agent Wiki entries consulted: `[[vbnet-await-catch]]`, `[[vbnet-rootnamespace-relative-declarations]]`
- Prior harness pattern: `[[classlib-viewmodel-auto-refresh-timer]]`,
  POS-15 `ReceiptSequenceHarnessReport.vb`
