---
module: Integration
agent: claude-code
date: 2026-05-07
plan-ref: Plans/VISTA_Modules/Integration/03-cross-module-handlers.md
status: completed
---

## Task Summary

Implemented INT-03: Cross-Module Contracts & Handlers. Wired all outstanding cross-module MediatR connections deferred during individual module builds. Created new SharedKernel query contracts, implemented handlers in Inventory, POS, and Purchasing, and wired low-stock alerts and receipt generation into the relevant service flows.

**Plan:** `Plans/VISTA_Modules/Integration/03-cross-module-handlers.md`

## What Was Done

### New SharedKernel Query Contracts

- Created `src/MerchSys.SharedKernel/Queries/GetProductCostQuery.vb` — `IRequest(Of GetProductCostResult)` with `ProductId`; sent by Accounting to get FIFO cost from Inventory
- Created `src/MerchSys.SharedKernel/Queries/GetProductCostResult.vb` — response DTO: `ProductId`, `FifoUnitCost`
- Created `src/MerchSys.SharedKernel/Queries/GetTotalARQuery.vb` — `IRequest(Of Decimal)`; sent by Accounting, handled by POS
- Created `src/MerchSys.SharedKernel/Queries/GetTotalAPQuery.vb` — `IRequest(Of Decimal)`; sent by Accounting, handled by Purchasing
- Created `src/MerchSys.SharedKernel/Queries/GetLowStockAlertCountQuery.vb` — `IRequest(Of Integer)`; sent by Accounting, handled by Inventory

### Pre-existing SharedKernel Contracts (Verified, No Action Needed)

- `Events/StockReturnedEvent.vb` — already existed with all required properties
- `Queries/GetProductCatalogQuery.vb` + `GetProductCatalogResult.vb` — already existed with `ProductCatalogItem` nested class

### New Inventory Handlers

- Created `src/MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb` — queries `InventoryDbContext.Products` with LINQ (name/SKU filter), computes `AvailableStock` from non-expired batches, returns `GetProductCatalogResult`
- Created `src/MerchSys.Inventory/Handlers/StockReturnedEventHandler.vb` — handles `StockReturnedEvent`, calls `IStockService.AddStockBatchAsync` to restock returned items (no expiry, no PO link)
- Created `src/MerchSys.Inventory/Handlers/GetProductCostQueryHandler.vb` — queries oldest non-expired `StockBatch` by `ReceiptDate` and returns its `UnitCost` as the FIFO cost; returns `0` when no stock exists
- Created `src/MerchSys.Inventory/Handlers/GetLowStockAlertCountQueryHandler.vb` — calls `ILowStockAlertService.GetCurrentAlertsAsync()` and returns `.Count`
- Created `src/MerchSys.Inventory/Handlers/ShrinkageRecordedHandler.vb` — handles `ShrinkageRecordedEvent`, calls `ILowStockAlertService.CheckAndGenerateAlertsAsync()` to trigger alerts after stock reduction

### New POS Handler

- Created `src/MerchSys.POS/Handlers/GetTotalARQueryHandler.vb` — queries `POSDbContext.CreditAccounts` and sums `CurrentBalance` across all non-deleted accounts

### New Purchasing Handler

- Created `src/MerchSys.Purchasing/Handlers/GetTotalAPQueryHandler.vb` — queries `PurchasingDbContext.AccountsPayableEntries` and sums `Balance` where `IsPaid = False`

### Service-Level Modifications

- Modified `src/MerchSys.POS/Services/CartService.vb` — added `IReceiptService` constructor dependency; calls `_receiptService.GenerateReceiptAsync(transaction.Id)` inside `FinalizeAsync` after `SaveChangesAsync`. `ReceiptService.GenerateReceiptAsync` is idempotent (returns existing receipt if already created), so the existing ViewModel call is safe without duplication.
- Modified `src/MerchSys.Inventory/Handlers/SaleCompletedHandler.vb` — added `ILowStockAlertService` constructor dependency; calls `CheckAndGenerateAlertsAsync()` after all FIFO deductions complete for the transaction
- Modified `src/MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` — added `IMediator` constructor dependency; for each sale item, sends `GetProductCostQuery` via MediatR to resolve FIFO unit cost, then populates `RevenueRecord.COGS`, `RevenueRecord.GrossProfit`, and `ExpenseRecord.Amount` with actual values instead of `0`

### UI Enforcement (Verified Already Complete)

- `SalesCartViewModel.CanPay` — credit blocking was already implemented (`If SelectedCreditCustomer.IsBlocked Then Return False`). No change needed.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ `0 Error(s), 0 Warning(s)` |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None. Build passed first time.

## Deviations from Plan

- **UI Enforcement (Deliverable 6):** `SalesCartViewModel.CanPay` already had the credit-blocking guard at lines 320–323. No change was needed.
- **ShrinkageRecordedHandler location:** Plan listed this under "Service-Level Wiring" as a modification to an existing handler, but `ShrinkageRecordedHandler.vb` did not exist in the Inventory module. Created as a new `INotificationHandler(Of ShrinkageRecordedEvent)` in `MerchSys.Inventory/Handlers/`.

## Codebase Wiki Discrepancies

- `codebase_wiki/modules/shared-kernel/events-queries.md` shows `GetProductCatalogQuery` returning `GetProductCatalogResult`, but lists no `GetInventoryValuationQueryHandler` and no Handlers layer for the existing queries. The new contracts added in INT-03 (`GetProductCostQuery`, `GetTotalARQuery`, `GetTotalAPQuery`, `GetLowStockAlertCountQuery`) are not yet reflected in the wiki.
- `codebase_wiki/modules/inventory/handlers.md` lists only `GetCurrentStockHandler`, `GoodsReceivedHandler`, and `SaleCompletedHandler`. The five new handlers created here are not yet reflected.
- `codebase_wiki/modules/pos/services.md` does not reflect the new `IReceiptService` dependency in `CartService`.

## What's Next

- [x] INT-04 (next integration plan, if applicable) *(completed — INT-04 delivered)*
- [x] Antigravity to sync codebase_wiki with the new handlers, contracts, and the CartService constructor change *(completed — synced 2026-05-07 per `codebase_wiki/log.md`)*

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/analysis/cross-module-data-flow.md`
- Codebase Wiki pages consulted: `LLM_Wiki/codebase_wiki/modules/shared-kernel/events-queries.md`, `LLM_Wiki/codebase_wiki/modules/inventory/handlers.md`, `LLM_Wiki/codebase_wiki/modules/inventory/services.md`, `LLM_Wiki/codebase_wiki/modules/accounting/handlers.md`, `LLM_Wiki/codebase_wiki/modules/pos/services.md`, `LLM_Wiki/codebase_wiki/modules/purchasing/data-access.md`
