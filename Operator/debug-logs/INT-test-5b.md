---
test-id: INT-Test-5b
checklist: INT-verification-checklist.md
branch: debug/INT-test-5b
started: 2026-05-20T17:35
resolved: 2026-05-20T18:02
status: resolved
---

# Debug Session — INT Test 5b (Event Chain Harness — both chains failing)

## Problem Statement

Running the Event Chain Harness reports FAIL on both chains:

- **GoodsReceived**: `DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.`
- **SaleCompleted**: `InvalidOperationException: Unable to resolve service for type 'MerchSys.SharedKernel.Persistence.ISyncableRepository\`1[MerchSys.POS.Data.POSDbContext]' while attempting to activate 'MerchSys.POS.Services.PaymentService'.`

Report: `%TEMP%\event-chain-report-20260520-173048.md`

## Starting State
- **Commit:** `498c958`
- **Build status:** clean (0 errors, 0 warnings)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/Configurations/VendorConfiguration.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb` — both fixes apply here

### Off-limits (do NOT touch)
- Entity configs, entity classes, services — the harness is the test fixture, not the application

---

## Attempt Log

### Attempt 1 — ⚠️ Partial
- **Hypothesis (GoodsReceived):** `VendorConfiguration.Address` is `IsRequired()` — harness seed missing it causes `DbUpdateException` (NOT NULL violation).
- **Hypothesis (SaleCompleted):** `PaymentService` requires `ISyncableRepository(Of POSDbContext)` which was not registered in scratch DI.
- **Changed:** Added `.Address = "Harness Address"` to vendor seed; added `HarnessPosRepository` no-op stub and registered it.
- **Build result:** ✅ 0 errors, 0 warnings
- **Runtime result:** GoodsReceived now fails with `no such table: Inv_ProductCategories`; SaleCompleted fails with `no such table: Pos_SalesTransactions`.
- **Verdict:** ⚠️ Partial — DI and seed issues fixed; schema creation broken.
- **Action:** Committed partial fix, continued to Attempt 2.

### Attempt 2 — ⚠️ Partial
- **Hypothesis:** `EnsureCreatedAsync()` on a shared SQLite file only creates tables for the FIRST `DbContext` called — subsequent calls see the file exists and return `False` without creating tables. Fix: use `RelationalDatabaseCreator.CreateTablesAsync()` per context.
- **Changed:** Replaced four `EnsureCreatedAsync()` calls with `CreateTablesAsync()` via `GetInfrastructure().GetService(Of IDatabaseCreator)()`.
- **Build result:** ✅ 0 errors, 0 warnings
- **Runtime result:** GoodsReceived fails `ISyncableRepository(Of PurchasingDbContext)` not registered; SaleCompleted fails `ISyncableRepository(Of InventoryDbContext)` not registered.
- **Verdict:** ⚠️ Partial — schema creation fixed; still missing repo stubs for Purchasing and Inventory.
- **Action:** Committed partial fix, continued to Attempt 3 (which became Attempt 4 after context reset).

### Attempt 3 (final) — ✅ Fixed
- **Hypothesis:** `PurchaseOrderService` / `GoodsReceivingService` require `ISyncableRepository(Of PurchasingDbContext)`; `StockService` requires `ISyncableRepository(Of InventoryDbContext)`; Accounting handlers may require `ISyncableRepository(Of AccountingDbContext)`. None were registered in the harness scratch `ServiceCollection`.
- **Changed:** Added `HarnessPurchasingRepository`, `HarnessInventoryRepository`, `HarnessAccountingRepository` stub classes (delegating to `_context.SaveChangesAsync()`); registered all three in `BuildScratchServices`.
- **Build result:** ✅ 0 errors, 0 warnings
- **Runtime result:** `event-chain-report-20260520-180147.md` — Overall: PASS ✅. Both chains pass all three checks (publisher fired, handler executed, stock movement row found).
- **Verdict:** ✅ Fixed
- **Action:** Committed `fix(INT-test-5b)`.

---

## Resolution

**Root causes (4 issues, all in `EventChainVerificationHarness.vb`):**
1. Vendor seed missing `Address` field → NOT NULL violation.
2. `ISyncableRepository(Of POSDbContext)` not registered → `PaymentService` DI failure.
3. `EnsureCreatedAsync()` multi-context limitation on shared SQLite file → missing tables for contexts 2–4.
4. `ISyncableRepository(Of Purchasing/Inventory/AccountingDbContext)` not registered → DI failures for `PurchaseOrderService`, `StockService`, Accounting handlers.

**Fix:** All four issues resolved inside the harness fixture only. No application services were touched.
