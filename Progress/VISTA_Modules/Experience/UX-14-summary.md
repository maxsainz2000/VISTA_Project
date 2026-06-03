---
module: MerchSys.App
agent: claude-code
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/14-concurrency-conflict-consolidation.md
status: completed
---

## Task Summary

Consolidated all five inline `DbUpdateConcurrencyException` catch blocks in write-path ViewModels
onto `ConcurrencyHelper.ExecuteWithConflictPromptAsync`, and extended concurrency-conflict coverage
to every ViewModel that mutates a RowVersion-protected table.

**Plan:** `[[14-concurrency-conflict-consolidation]]`

## What Was Done

### Consolidation (5 wired VMs — zero inline catches remain)

- Modified `MerchSys.POS/ViewModels/SalesCartViewModel.vb` — replaced inline concurrency flag/catch/prompt with primitive; replaced `Imports Microsoft.EntityFrameworkCore` with `Imports MerchSys.SharedKernel.Persistence`
- Modified `MerchSys.Purchasing/ViewModels/APLedgerViewModel.vb` — same import swap; `work` = `RecordPaymentAsync`; `onRefresh` = close dialog + reload; success path in `If saved Then`
- Modified `MerchSys.Purchasing/ViewModels/GoodsReceivingViewModel.vb` — same import swap; receipt captured via closure; `onRefresh` resets form + reloads POs
- Modified `MerchSys.POS/ViewModels/CreditManagementViewModel.vb` — kept `Imports Microsoft.EntityFrameworkCore` (still used for `_context.Database.GetConnectionString()`); added `Imports MerchSys.SharedKernel.Persistence`; `targetId` captured pre-lambda for closure safety
- Modified `MerchSys.POS/ViewModels/VatSettingsViewModel.vb` — same import swap; `result` captured via closure; `onRefresh = AddressOf ReloadAsync`

### Chosen consolidation shape (watch-item #1)

**Option A — VM-side outer `Try/Catch/Finally`, primitive for conflict mechanics only.**
- `work` lambda: the mutation only (no success path)
- `onRefresh` lambda: what to do when operator chooses Refresh
- Success path: inside `If saved Then` within the outer `Try`
- Non-concurrency exceptions propagate from `work` to the outer VM `Catch` blocks, so domain-error UX (e.g. `InvalidOperationException` → `PaymentError`) is fully preserved
- `IsBusy = False` in outer `Finally` (runs after the entire primitive including dialog)

Option B (callbacks in the primitive) was rejected: adding `onSuccess`/`onError` to the primitive would over-centralize VM-specific toast/navigation logic with no real gain.

### Coverage sweep (6 additional write-path VMs guarded)

| VM | Method(s) | Token table(s) | Change |
|---|---|---|---|
| `ProductManagementViewModel` | `SaveProductAsync`, `ToggleActiveAsync`, `SaveCategoryAsync`, `DeleteCategoryAsync` | `Inv_Products`, `Inv_ProductCategories` | Added `IConflictPresenter`, `Imports MerchSys.SharedKernel.Persistence`; `productNotFound`/`catNotFound` closure flags for "not found" early-exit paths |
| `ShrinkageViewModel` | `ExecuteRecordAsync` | `Inv_StockBatches` (via `RecordShrinkageAsync`) | Added `IConflictPresenter`, `Imports MerchSys.SharedKernel.{Interfaces,Persistence}`; record qty/value captured via closure |
| `PurchaseOrderListViewModel` | `SaveDraftAsync`, `SubmitFromEditorAsync`, `SubmitSelectedAsync`, `DeleteSelectedAsync` | `Pur_PurchaseOrders` | Added `IConflictPresenter` ctor param; `submittedOrderNumber` closure for `SubmitFromEditorAsync` |
| `ReorderSuggestionsViewModel` | `AcceptAsync` | `Pur_PurchaseOrders` | Added `IConflictPresenter`, `Imports MerchSys.SharedKernel.{Interfaces,Persistence}`; `poNumber` closure |
| `VendorListViewModel` | `SaveVendorAsync`, `DeleteSelectedAsync` | `Pur_Vendors` | Added `IConflictPresenter`, `Imports MerchSys.SharedKernel.{Interfaces,Persistence}`; `isNew` snapshot for status message |
| `TransactionHistoryViewModel` | `ProcessReturnAsync` | `Inv_StockBatches` (when restocking) | Added `IConflictPresenter` ctor param; `onRefresh` closes dialog + reloads transaction detail |

### Not guarded (no RowVersion token — documented)

| VM | Method(s) | Reason |
|---|---|---|
| `ReorderSuggestionsViewModel.SaveConfigAsync` | Save reorder config | `Pur_ReorderConfigs` — no RowVersion column |
| `ReorderSuggestionsViewModel.DismissAsync` | Dismiss suggestion | `Pur_ReorderSuggestions` — no RowVersion column |
| `VendorCatalogViewModel` (all write paths) | Add/Update/Delete catalog entry | `Pur_VendorProducts` — no RowVersion column |
| `PurchaseOrderEditorViewModel` | (no direct writes) | Pure form-state holder; actual saves go through `PurchaseOrderListViewModel` |
| `VendorEditorViewModel` | (no direct writes) | Pure form-state holder; actual saves go through `VendorListViewModel` |
| All Accounting VMs | (no writes to token tables) | Dashboards and reports only; no mutable-row writes |

### RowVersion table reference (from `codebase_wiki/schemas/database.md`)

Eight tables carry `RowVersion TIMESTAMP(6)`: `Inv_StockBatches`, `Inv_Products`, `Pur_AccountsPayable`, `Pur_PurchaseOrders`, `Pos_CreditAccounts`, `Pos_SalesTransactions`, `Pur_Vendors`, `Inv_ProductCategories`.

`Pos_SalesTransactions` — write path is `CartService.FinalizeAsync` inside `SalesCartViewModel.ProcessPaymentAsync`, already guarded. ✓

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | Deferred — see realization check note below |

## Grep counts (required by plan)

| Scope | Before | After |
|---|---|---|
| `DbUpdateConcurrencyException` in `ViewModels/` | 5 | 0 |
| `DbUpdateConcurrencyException` in codebase total | 7 | 2 |

The 2 remaining: `ConcurrencyHelper.vb` (the primitive itself, correct) and `ReceiptIntegrityService.vb` (service-layer tamper check, not a ViewModel — correct).

## Conflict manual repro (acceptance criterion 4)

**Simulated repro procedure:**
1. Open two Manager sessions pointing at the same MariaDB instance.
2. Client A: open AP Ledger, select an invoice.
3. Client B: record a partial payment on the same invoice (raises `OutstandingBalance` RowVersion token).
4. Client A: attempt to record a payment on the same invoice (stale RowVersion).
5. **Expected:** "Data changed elsewhere — refresh and retry" dialog with Refresh/Cancel.
6. Refresh: AP Ledger reloads live balance; payment dialog closes.
7. Cancel: payment dialog stays open, operator's typed amount is preserved.
8. No path re-saves with stale values.

Realization check not performed in this session (no second client available); the same pattern was verified in UX-06 post-review. The code path is mechanically identical — only the call site changed.

## Issues Encountered

- **`productNotFound`/`catNotFound` closure flag pattern** — `ProductManagementViewModel` has an early `Return` path when `FindAsync` returns Nothing. Since `Return` inside an `Async Function` lambda exits the lambda (not the outer method), the primitive sees the lambda complete normally and returns `True`. A closure Boolean flag (`productNotFound`) was used to detect this case after the primitive call and set `EditorError` appropriately, then `Return` from the outer method. This is safe: `Finally` still runs, resetting `IsBusy`. No antipattern entry needed (it is VB-idiomatic).

- **`IConflictPresenter` DI resolution** — All new VMs register via `services.AddTransient(Of VM)()` with auto-DI. `IConflictPresenter` is already registered as Singleton. Adding it as a constructor parameter resolves automatically with no DI registration file changes required.

## What's Next

- [ ] UX-15 state consistency sweep (remaining IsBusy/IsEmpty gaps)
- [ ] UX-16 command palette

## Cross-References

- Domain Wiki: `[[centralized-database-architecture]]`, `[[client-server-wpf]]`
- Agent Wiki: `[[wpf-vista-state-feedback]]`, `[[efcore-inherited-rowversion-unmapped-column]]`, `[[feedback-vbnet-await-catch]]`
- Codebase Wiki discrepancies: DI registry (`di-registry.md`) does not reflect the new `IConflictPresenter` constructor params for the 6 newly guarded VMs — Antigravity to sync during wiki-sync step.
