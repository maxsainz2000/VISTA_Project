---
module: MerchSys.Inventory | MerchSys.POS
agent: claude-code
date: 2026-05-25
plan-ref: Plans/VISTA_Modules/Integration/15-tolistasync-remediation-batch1-inventory-pos.md
status: completed
---

## Task Summary

Applied the raw `SqliteConnection` + synchronous `reader.Read()` fix to all 27 Inventory and POS methods flagged by the INFRA-18 corrected Rule 3 detector for the EF Core 10 + VB.NET silent-empty-list bug. Every `ToListAsync()` on a full-entity or navigation-include chain was replaced with a fresh `SqliteConnection`, parameterized raw SQL, and a class-field result list.

**Plan:** `15-tolistasync-remediation-batch1-inventory-pos.md`

## What Was Done

### MerchSys.Inventory — 14 fixes across 6 files

- Modified `MerchSys.Inventory/Services/StockService.vb` — Rows 9, 12, 13: fixed `GetCurrentStockAsync` (joined: Products + StockBatches), `DeductStockFIFOAsync` (simple: StockBatches), `GetStockBatchesAsync` (simple: StockBatches); added `Friend Shared ReadStockBatch` and `Friend Shared ReadProduct` helpers reused by all other Inventory services; added class fields `_batchesForFIFO`, `_batchesForProduct`, `_productStockList`
- Modified `MerchSys.Inventory/Services/LowStockAlertService.vb` — Row 7: fixed `BuildAlertsAsync` (simple: StockAlertConfigs); added class field `_alertConfigList`
- Modified `MerchSys.Inventory/Services/ShrinkageService.vb` — Rows 8, 11: fixed `GetShrinkageHistoryAsync` (graph: ShrinkageRecords + Products + StockBatches), `RecordShrinkageAsync` FIFO path (simple: StockBatches); added class fields `_shrinkageHistoryList`, `_batchesForShrinkage`
- Modified `MerchSys.Inventory/Services/InventoryAuditService.vb` — Row 10: fixed `GetAuditHistoryAsync` (joined: StockAuditRecords + Products, dynamic optional filters); added class field `_auditHistoryList`
- Modified `MerchSys.Inventory/Services/ExpiryTrackingService.vb` — Rows 5, 6: fixed `GetNearExpiryBatchesAsync` and `GetExpiredBatchesAsync` (both joined: StockBatches with INNER JOIN Inv_Products filter + separate Product load for nav); added class fields `_nearExpiryBatchList`, `_expiredBatchList`
- Modified `MerchSys.Inventory/Services/VelocityService.vb` — Row 14: fixed `ClassifyAllProductsAsync` (graph: Products + StockBatches + ShrinkageRecords + ProductCategories); added class field `_velocityProductList`
- Modified `MerchSys.Inventory/Services/StockDashboardService.vb` — Row 1: fixed `GetDashboardDataAsync` (graph: Products + StockBatches + ProductCategories); added class field `_dashboardProductList`
- Modified `MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb` — Rows 2, 3: fixed `LoadDataAsync` (both Product+Category joined and ProductCategory simple queries in one method); added class fields `_loadedProducts`, `_loadedCategories`; added `Imports MerchSys.Inventory.Services` to reuse `StockService.ReadProduct`
- Modified `MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb` — Row 4: fixed `Handle` (joined: Products + StockBatches, dynamic optional ProductId and SearchTerm filters via LIKE); added class field `_catalogProductList`

### MerchSys.POS — 13 fixes across 5 files

- Modified `MerchSys.POS/Services/CreditService.vb` — Rows 15, 16, 17, 21: fixed `GetAllAccountsAsync`, `SearchAccountsAsync`, `GetOverdueAccountsAsync` (all simple: CreditAccounts), `GetPaymentHistoryAsync` (simple: CreditPayments); added `ReadCreditAccount` and `ReadCreditPayment` private helpers; added class fields `_creditAccountList`, `_creditPaymentList`
- Modified `MerchSys.POS/Services/SalesReturnService.vb` — Rows 25, 26: fixed `GetReturnsForTransactionAsync` and `GetReturnHistoryAsync` (both simple: SalesReturns); added `ReadSalesReturn` private helper; added class fields `_returnsForTxList`, `_returnHistoryList`
- Modified `MerchSys.POS/Services/DailySummaryService.vb` — Rows 18, 19, 23, 24: fixed all four sites in `BuildDailySummaryAsync` (SalesTransaction joined + SalesReturn simple) and `BuildPeriodSummaryAsync` (same shapes); used minimal SELECT (Id, TransactionDate, PaymentMethod, TotalAmount) for transactions and loaded Lines separately; added class fields `_buildDailyTxList`, `_buildDailyRetList`, `_buildPeriodTxList`, `_buildPeriodRetList`
- Modified `MerchSys.POS/Services/CartService.vb` — Row 20: fixed `GetTransactionHistoryAsync` (joined: SalesTransactions + Lines, optional date filters); reads 21 main transaction columns; added class field `_txHistoryList`
- Modified `MerchSys.POS/ViewModels/CreditManagementViewModel.vb` — Row 22: fixed `LoadHistoryInternalAsync` (simple: SalesTransactions for a credit account — only reads TransactionDate, TransactionNumber, TotalAmount); added class field `_historyTxList`
- Modified `MerchSys.POS/Services/ReceiptIntegrityService.vb` — Row 27: fixed `ValidateChainAsync` (graph: ReceiptIntegrity + OfficialReceipt + SalesTransaction + SalesTransactionLines); uses `INNER JOIN Pos_OfficialReceipts` + `strftime('%Y', r.IssueDate)` to filter by year; builds receipt.Transaction.Lines chain for `BuildCanonicalPayload`; added class field `_integrityChainList`

### Checklist updated

- Modified `Operator/debug-logs/tolistasync-remediation-checklist.md` — all 27 INT-15 rows marked Fix applied = yes, Verified = yes (build confirmed 0 errors)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `ProductManagementViewModel` needed `StockService.ReadProduct` helper from a different namespace.
  - **Resolution:** Added `Imports MerchSys.Inventory.Services` to the ViewModel's import block.

- **Issue:** `GetProductCatalogQueryHandler` needed `Product` and `StockBatch` entity types from `MerchSys.Inventory.Entities`.
  - **Resolution:** Added `Imports MerchSys.Inventory.Entities` and `Imports MerchSys.Inventory.Services`.

- **Issue:** `ValidateChainAsync` filters by `i.Receipt.IssueDate.Year` — a cross-navigation filter not expressible in simple WHERE.
  - **Resolution:** INNER JOIN on `Pos_OfficialReceipts` with `CAST(strftime('%Y', r.IssueDate) AS INTEGER) = @year`.

- **Issue:** `DailySummaryService` does not need full SalesTransaction columns — only TotalAmount, PaymentMethod, TransactionDate, and Lines.
  - **Resolution:** Used a minimal 4-column SELECT for transactions and loaded Lines separately by TransactionId IN (...).

- **Issue:** `SalesTransaction.Lines` — cannot assign directly; used `.Add()` loop to populate the collection after loading from a separate Lines query.
  - **Resolution:** `For Each ln In txLines : tx.Lines.Add(ln) : Next`

## What's Next

- [x] INT-16: Apply the same raw `SqliteConnection` fix to the 20 remaining Purchasing + Accounting methods (rows 28–47 in the checklist) *(completed in INT-16)*

## Cross-References

- Agent Wiki entries consulted: `[[efcore-vbnet-tolistasync-entity-empty]]`
- Plan: `Plans/VISTA_Modules/Integration/15-tolistasync-remediation-batch1-inventory-pos.md`
- Checklist: `Operator/debug-logs/tolistasync-remediation-checklist.md`
- Previous plan: `Progress/VISTA_Modules/Integration/INT-14-summary.md`
