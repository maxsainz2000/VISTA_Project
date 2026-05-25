# ToListAsync Remediation Triage Checklist

**Plan:** INT-14  
**Date produced:** 2026-05-25  
**Detector version:** INFRA-18 corrected Rule 3 detector  
**Baseline input:** `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md` (63 sites)  
**Corrected output:** 47 true positives (16 false positives cleared)

---

## Detector Summary

The corrected INFRA-18 detector cleared **16 sites** from the 2026-05-24 baseline:

| Cleared site | Reason |
|---|---|
| FinancialOverviewService.vb:90 | GroupBy → Select(New With {…}) — anonymous projection |
| FinancialOverviewService.vb:120 | GroupBy → Select(New With {…}) — anonymous projection |
| IncomeStatementService.vb:73 | GroupBy → Select(New With {…}) — anonymous projection |
| ITamperAuditQueryService.vb:53 | Select(Function(e) e.TamperKind) — scalar string projection |
| SalesSummaryService.vb:62 | GroupBy → Select(New With {…}) — anonymous projection |
| SalesSummaryService.vb:104 | GroupBy → Select(New With {…}) — anonymous projection |
| SalesSummaryService.vb:155 | GroupBy → Select(New With {…}) — anonymous projection |
| InventoryAuditService.vb:164 | GroupBy → Select(…g.First()) — GroupBy gate fires |
| LowStockAlertService.vb:92 | GroupBy → Select(New With {…}) — anonymous projection |
| VelocityService.vb:36 | GroupBy → Select(New With {…}) — anonymous projection |
| SalesReturnService.vb:136 | Select(Function(r) r.OriginalTransactionId) — scalar int projection |
| ReceiptArchivalService.vb:183 | LINQ query-syntax `Select New With {…}` — anonymous projection |
| GoodsReceivingService.vb:56 | Select(Function(r) r.ReceiptNumber) — scalar string projection |
| PurchaseOrderService.vb:30 | Select(Function(p) p.OrderNumber) — scalar string projection |
| ReorderService.vb:42 | Select(Function(s) s.ProductId) — scalar int projection |
| ReorderService.vb:113 | Select(Function(p) p.OrderNumber) — scalar string projection |

---

## Remediation Checklist

Paths are relative to `WPF_Applications/MerchSys/src/`.

### INT-15 Batch — Inventory + POS (27 rows)

| # | Module | File | Line | Method | Entity | Has Include | UI surface | Fix complexity | Batch | Fix applied | Verified |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Inventory | MerchSys.Inventory/Services/StockDashboardService.vb | 34 | GetDashboardDataAsync | Product | yes | StockDashboardView main panel — all product stock levels, valuation, expiry counts rendered on first load | graph | INT-15 | yes | yes |
| 2 | Inventory | MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb | 344 | LoadDataAsync | Product | yes | ProductManagementView product catalog list — loads all non-deleted products with Category nav | joined | INT-15 | yes | yes |
| 3 | Inventory | MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb | 364 | LoadDataAsync | ProductCategory | no | ProductManagementView category list — loaded in same LoadDataAsync call as row 2 | simple | INT-15 | yes | yes |
| 4 | Inventory | MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb | 45 | Handle | Product | yes | POS SalesCartView product search panel — cross-module MediatR query, called every time a cashier searches for a product during a sale | joined | INT-15 | yes | yes |
| 5 | Inventory | MerchSys.Inventory/Services/ExpiryTrackingService.vb | 40 | GetNearExpiryBatchesAsync | StockBatch | yes | ExpiryMonitorView near-expiry batch list — loads batches expiring within the configured threshold | joined | INT-15 | yes | yes |
| 6 | Inventory | MerchSys.Inventory/Services/ExpiryTrackingService.vb | 71 | GetExpiredBatchesAsync | StockBatch | yes | ExpiryMonitorView expired batch list — loads batches past their expiry date | joined | INT-15 | yes | yes |
| 7 | Inventory | MerchSys.Inventory/Services/LowStockAlertService.vb | 82 | BuildAlertsAsync (private) | StockAlertConfig | no | StockDashboardView alert count panel; also fires via CheckAndGenerateAlertsAsync after every sale | simple | INT-15 | yes | yes |
| 8 | Inventory | MerchSys.Inventory/Services/ShrinkageService.vb | 149 | GetShrinkageHistoryAsync | ShrinkageRecord | yes | ShrinkageView shrinkage history list — loads all shrinkage records with Product and StockBatch navigation | graph | INT-15 | yes | yes |
| 9 | Inventory | MerchSys.Inventory/Services/StockService.vb | 120 | GetCurrentStockAsync | Product | yes | StockDashboardView stock levels; also fed into LowStockAlertService.BuildAlertsAsync and GetCurrentStockQuery handler | joined | INT-15 | yes | yes |
| 10 | Inventory | MerchSys.Inventory/Services/InventoryAuditService.vb | 156 | GetAuditHistoryAsync | StockAuditRecord | yes | StockDashboardView or dedicated audit history panel — loads audit records with Product nav, filtered by product/date | joined | INT-15 | yes | yes |
| 11 | Inventory | MerchSys.Inventory/Services/ShrinkageService.vb | 82 | RecordShrinkageAsync | StockBatch | no | ShrinkageView record shrinkage form — loads batches for FIFO deduction path when no specific batchId is supplied | simple | INT-15 | yes | yes |
| 12 | Inventory | MerchSys.Inventory/Services/StockService.vb | 77 | DeductStockFIFOAsync | StockBatch | no | Background POS SaleCompleted handler — FIFO batch consumption, blocks every sale transaction | simple | INT-15 | yes | yes |
| 13 | Inventory | MerchSys.Inventory/Services/StockService.vb | 141 | GetStockBatchesAsync | StockBatch | no | Batch detail support — called by inventory service operations that need per-batch breakdown | simple | INT-15 | yes | yes |
| 14 | Inventory | MerchSys.Inventory/Services/VelocityService.vb | 29 | ClassifyAllProductsAsync | Product | yes | StockDashboardView velocity classification panel; also used by background reorder analysis | graph | INT-15 | yes | yes |
| 15 | POS | MerchSys.POS/Services/CreditService.vb | 62 | GetAllAccountsAsync | CreditAccount | no | CreditManagementView account list — loads all non-deleted credit accounts on screen open | simple | INT-15 | yes | yes |
| 16 | POS | MerchSys.POS/Services/CreditService.vb | 73 | SearchAccountsAsync | CreditAccount | no | CreditManagementView search results; also SalesCartView credit customer picker | simple | INT-15 | yes | yes |
| 17 | POS | MerchSys.POS/Services/CreditService.vb | 172 | GetOverdueAccountsAsync | CreditAccount | no | CreditManagementView overdue accounts panel — accounts with balance > 0 and no recent payment | simple | INT-15 | yes | yes |
| 18 | POS | MerchSys.POS/Services/DailySummaryService.vb | 40 | BuildDailySummaryAsync (private) | SalesTransaction | yes | DailySummaryView daily summary — loads all transactions with Lines nav for payment breakdown and top-products | joined | INT-15 | yes | yes |
| 19 | POS | MerchSys.POS/Services/DailySummaryService.vb | 96 | BuildPeriodSummaryAsync (private) | SalesTransaction | yes | DailySummaryView weekly/monthly summary — same shape as daily but over a date range | joined | INT-15 | yes | yes |
| 20 | POS | MerchSys.POS/Services/CartService.vb | 163 | GetTransactionHistoryAsync | SalesTransaction | yes | TransactionHistoryView sales history list — loads transactions with Lines nav, filtered by date range | joined | INT-15 | yes | yes |
| 21 | POS | MerchSys.POS/Services/CreditService.vb | 151 | GetPaymentHistoryAsync | CreditPayment | no | CreditManagementView payment history panel for a selected account | simple | INT-15 | yes | yes |
| 22 | POS | MerchSys.POS/ViewModels/CreditManagementViewModel.vb | 361 | LoadHistoryInternalAsync | SalesTransaction | no | CreditManagementView credit transaction history panel — loads credit-method transactions for a selected account | simple | INT-15 | yes | yes |
| 23 | POS | MerchSys.POS/Services/DailySummaryService.vb | 44 | BuildDailySummaryAsync (private) | SalesReturn | no | DailySummaryView — daily return count and value, loaded in same call as row 18 | simple | INT-15 | yes | yes |
| 24 | POS | MerchSys.POS/Services/DailySummaryService.vb | 100 | BuildPeriodSummaryAsync (private) | SalesReturn | no | DailySummaryView — period return count and value, loaded in same call as row 19 | simple | INT-15 | yes | yes |
| 25 | POS | MerchSys.POS/Services/SalesReturnService.vb | 115 | GetReturnsForTransactionAsync | SalesReturn | no | TransactionHistoryView detail panel — returns associated with a selected transaction | simple | INT-15 | yes | yes |
| 26 | POS | MerchSys.POS/Services/SalesReturnService.vb | 124 | GetReturnHistoryAsync | SalesReturn | no | TransactionHistoryView return history date-range search | simple | INT-15 | yes | yes |
| 27 | POS | MerchSys.POS/Services/ReceiptIntegrityService.vb | 110 | ValidateChainAsync | ReceiptIntegrity | yes | Background archival sweep — called by ReceiptArchivalService to validate hash chain before archiving; not a visible list screen | graph | INT-15 | yes | yes |

---

### INT-16 Batch — Purchasing + Accounting (20 rows)

| # | Module | File | Line | Method | Entity | Has Include | UI surface | Fix complexity | Batch | Fix applied | Verified |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 28 | Purchasing | MerchSys.Purchasing/Services/PurchaseOrderService.vb | 77 | GetAllAsync | PurchaseOrder | yes | PO list screen — main Purchasing view, loads all POs with Lines and Vendor nav, optional status filter | graph | INT-16 | | |
| 29 | Purchasing | MerchSys.Purchasing/Services/VendorService.vb | 58 | GetAllAsync | Vendor | no | Vendor list screen; also vendor dropdown in PO creation flow | simple | INT-16 | | |
| 30 | Purchasing | MerchSys.Purchasing/Services/AccountsPayableService.vb | 99 | GetAllOutstandingAsync | AccountsPayableEntry | yes | Purchasing AP outstanding list dashboard — unpaid entries with Vendor and PO nav | graph | INT-16 | | |
| 31 | Purchasing | MerchSys.Purchasing/Services/AccountsPayableService.vb | 139 | GetAllAsync | AccountsPayableEntry | yes | Purchasing AP complete history screen — all entries ordered by invoice date | graph | INT-16 | | |
| 32 | Purchasing | MerchSys.Purchasing/Services/ReorderService.vb | 93 | GetPendingSuggestionsAsync | ReorderSuggestion | no | Purchasing reorder suggestion dashboard — pending suggestions list | simple | INT-16 | | |
| 33 | Purchasing | MerchSys.Purchasing/Services/ReorderService.vb | 191 | GetAllConfigsAsync | ReorderConfig | yes | Purchasing reorder configuration list screen — all configs with PreferredVendor nav | joined | INT-16 | | |
| 34 | Purchasing | MerchSys.Purchasing/Services/VendorService.vb | 122 | SearchAsync | Vendor | no | Vendor search screen — name/contact/phone text search | simple | INT-16 | | |
| 35 | Purchasing | MerchSys.Purchasing/Services/AccountsPayableService.vb | 108 | GetByVendorAsync | AccountsPayableEntry | yes | Purchasing AP by-vendor detail view — all entries for a vendor | graph | INT-16 | | |
| 36 | Purchasing | MerchSys.Purchasing/Services/AccountsPayableService.vb | 118 | GetOverdueAsync | AccountsPayableEntry | yes | Purchasing AP overdue dashboard panel — entries past due date | graph | INT-16 | | |
| 37 | Purchasing | MerchSys.Purchasing/Services/GoodsReceivingService.vb | 162 | GetReceiptsForPOAsync | GoodsReceipt | yes | Purchasing PO detail view — goods receipts associated with a selected PO | joined | INT-16 | | |
| 38 | Purchasing | MerchSys.Purchasing/Services/PriceChangeService.vb | 83 | GetUnacknowledgedAsync | PriceChangeAlert | no | Purchasing price-change alert notification list — unacknowledged alerts | simple | INT-16 | | |
| 39 | Purchasing | MerchSys.Purchasing/Services/PriceChangeService.vb | 103 | GetHistoryForProductAsync | PriceChangeAlert | no | Purchasing product price-history panel — all change alerts for a selected product | simple | INT-16 | | |
| 40 | Purchasing | MerchSys.Purchasing/Services/ReorderService.vb | 197 | GetAllSuggestionsAsync | ReorderSuggestion | no | Purchasing all-suggestions history list (accepted + dismissed + pending) | simple | INT-16 | | |
| 41 | Purchasing | MerchSys.Purchasing/Services/AccountsPayableService.vb | 39 | CreateFromPurchaseOrderAsync | GoodsReceipt | yes | AP entry creation path — GRs for a PO are loaded with Lines to compute invoice total | joined | INT-16 | | |
| 42 | Purchasing | MerchSys.Purchasing/Services/VendorService.vb | 141 | GetVendorWithPurchaseHistoryAsync | GoodsReceipt | no | Vendor detail view — GRs loaded to compute average lead time in purchase history section | simple | INT-16 | | |
| 43 | Purchasing | MerchSys.Purchasing/Services/ReorderService.vb | 35 | GenerateSuggestionsAsync | ReorderConfig | yes | Background reorder engine — all active configs loaded to check stock vs reorder point after each sale | joined | INT-16 | | |
| 44 | Accounting | MerchSys.Accounting/Services/VatReportingService.vb | 170 | ListReturnsAsync | VatReturn | no | Accounting VAT return list screen — all VAT return headers, filtered by optional year | simple | INT-16 | | |
| 45 | Accounting | MerchSys.Accounting/Services/VatReportingService.vb | 270 | CollectLedgerDataAsync (private) | RevenueRecord | no | VAT return generation screens (monthly, quarterly, non-VAT) — revenue records loaded for the reporting window | simple | INT-16 | | |
| 46 | Accounting | MerchSys.Accounting/Services/VatReportingService.vb | 275 | CollectLedgerDataAsync (private) | ExpenseRecord | no | VAT return generation screens — expense records loaded in same CollectLedgerDataAsync call as row 45 | simple | INT-16 | | |
| 47 | Accounting | MerchSys.Accounting/Services/ITamperAuditQueryService.vb | 39 | GetIncidentsAsync | TamperAuditEntry | no | Accounting tamper audit reporting — loads audit log entries for a date range | simple | INT-16 | | |

---

## Already-Applied Fix (Reference)

The following fix was applied before the 2026-05-24 baseline run and therefore does not appear in the corrected Rule 3 detector output. It is recorded here per INT-14 AC6 and serves as the canonical "simple" fix shape for INT-15 and INT-16 implementors.

| Module | File | Method | Entity | Fix shape | Fix applied | Source |
|---|---|---|---|---|---|---|
| Purchasing | MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb | LoadDataAsync | Vendor | Raw `SqliteConnection` + synchronous `reader.Read()` loop writing to `_vendorList` class field | yes | Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md §2 (Rule 3, "INFRA-test-X resolved this for vendors only") |

The fix replaces the `_db.Vendors.AsNoTracking().IgnoreQueryFilters().ToListAsync()` call. The exact pattern is in `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` under the **Fix** section. Use this as the reference shape for every `simple` row.

---

## Ordered Sequence — INT-15

Sequence for INT-15 implementors to apply fixes, ranked by user visibility:

### Visible list / dashboard screens first

1. **Row 1** — `StockDashboardService.GetDashboardDataAsync` (Product, graph) — most visible Inventory screen
2. **Row 2** — `ProductManagementViewModel.LoadDataAsync` line 344 (Product, joined) — product catalog list
3. **Row 3** — `ProductManagementViewModel.LoadDataAsync` line 364 (ProductCategory, simple) — category list, same LoadDataAsync call
4. **Row 4** — `GetProductCatalogQueryHandler.Handle` (Product, joined) — POS product search, critical to sales flow
5. **Row 15** — `CreditService.GetAllAccountsAsync` (CreditAccount, simple) — credit list on open
6. **Row 16** — `CreditService.SearchAccountsAsync` (CreditAccount, simple) — credit search
7. **Row 17** — `CreditService.GetOverdueAccountsAsync` (CreditAccount, simple) — overdue panel
8. **Row 18** — `DailySummaryService.BuildDailySummaryAsync` line 40 (SalesTransaction, joined) — daily summary
9. **Row 19** — `DailySummaryService.BuildPeriodSummaryAsync` line 96 (SalesTransaction, joined) — weekly/monthly summary
10. **Row 20** — `CartService.GetTransactionHistoryAsync` (SalesTransaction, joined) — transaction history list

### Search and detail screens next

11. **Row 5** — `ExpiryTrackingService.GetNearExpiryBatchesAsync` (StockBatch, joined) — near-expiry list
12. **Row 6** — `ExpiryTrackingService.GetExpiredBatchesAsync` (StockBatch, joined) — expired list
13. **Row 7** — `LowStockAlertService.BuildAlertsAsync` (StockAlertConfig, simple) — alert list / post-sale trigger
14. **Row 8** — `ShrinkageService.GetShrinkageHistoryAsync` (ShrinkageRecord, graph) — shrinkage history
15. **Row 9** — `StockService.GetCurrentStockAsync` (Product, joined) — stock levels, feeds alerts
16. **Row 10** — `InventoryAuditService.GetAuditHistoryAsync` (StockAuditRecord, joined) — audit history
17. **Row 21** — `CreditService.GetPaymentHistoryAsync` (CreditPayment, simple) — payment history panel
18. **Row 22** — `CreditManagementViewModel.LoadHistoryInternalAsync` (SalesTransaction, simple) — credit tx history
19. **Row 23** — `DailySummaryService.BuildDailySummaryAsync` line 44 (SalesReturn, simple) — daily returns
20. **Row 24** — `DailySummaryService.BuildPeriodSummaryAsync` line 100 (SalesReturn, simple) — period returns
21. **Row 25** — `SalesReturnService.GetReturnsForTransactionAsync` (SalesReturn, simple) — transaction detail returns
22. **Row 26** — `SalesReturnService.GetReturnHistoryAsync` (SalesReturn, simple) — return history search

### Background-job-only methods last

23. **Row 11** — `ShrinkageService.RecordShrinkageAsync` line 82 (StockBatch, simple) — FIFO load within write operation
24. **Row 12** — `StockService.DeductStockFIFOAsync` (StockBatch, simple) — POS sale FIFO deduction
25. **Row 13** — `StockService.GetStockBatchesAsync` (StockBatch, simple) — batch detail support
26. **Row 14** — `VelocityService.ClassifyAllProductsAsync` (Product, graph) — background velocity analysis
27. **Row 27** — `ReceiptIntegrityService.ValidateChainAsync` (ReceiptIntegrity, graph) — archival integrity sweep

---

## Ordered Sequence — INT-16

Sequence for INT-16 implementors to apply fixes, ranked by user visibility:

### Visible list / dashboard screens first

1. **Row 28** — `PurchaseOrderService.GetAllAsync` (PurchaseOrder, graph) — main PO list, most visible Purchasing screen
2. **Row 29** — `VendorService.GetAllAsync` (Vendor, simple) — vendor list; note: same entity already fixed in PurchaseOrderListViewModel (reference above)
3. **Row 30** — `AccountsPayableService.GetAllOutstandingAsync` (AccountsPayableEntry, graph) — AP outstanding dashboard
4. **Row 31** — `AccountsPayableService.GetAllAsync` (AccountsPayableEntry, graph) — AP history screen
5. **Row 32** — `ReorderService.GetPendingSuggestionsAsync` (ReorderSuggestion, simple) — pending suggestions list
6. **Row 33** — `ReorderService.GetAllConfigsAsync` (ReorderConfig, joined) — reorder config list
7. **Row 44** — `VatReportingService.ListReturnsAsync` (VatReturn, simple) — VAT return list screen

### Search and detail screens next

8. **Row 34** — `VendorService.SearchAsync` (Vendor, simple) — vendor search
9. **Row 35** — `AccountsPayableService.GetByVendorAsync` (AccountsPayableEntry, graph) — AP by vendor
10. **Row 36** — `AccountsPayableService.GetOverdueAsync` (AccountsPayableEntry, graph) — AP overdue panel
11. **Row 37** — `GoodsReceivingService.GetReceiptsForPOAsync` (GoodsReceipt, joined) — PO detail GRs
12. **Row 38** — `PriceChangeService.GetUnacknowledgedAsync` (PriceChangeAlert, simple) — alert notifications
13. **Row 39** — `PriceChangeService.GetHistoryForProductAsync` (PriceChangeAlert, simple) — price history panel
14. **Row 40** — `ReorderService.GetAllSuggestionsAsync` (ReorderSuggestion, simple) — suggestions history
15. **Row 41** — `AccountsPayableService.CreateFromPurchaseOrderAsync` line 39 (GoodsReceipt, joined) — AP creation path
16. **Row 42** — `VendorService.GetVendorWithPurchaseHistoryAsync` (GoodsReceipt, simple) — vendor purchase history
17. **Row 45** — `VatReportingService.CollectLedgerDataAsync` line 270 (RevenueRecord, simple) — VAT generation path
18. **Row 46** — `VatReportingService.CollectLedgerDataAsync` line 275 (ExpenseRecord, simple) — VAT generation path
19. **Row 47** — `ITamperAuditQueryService.GetIncidentsAsync` (TamperAuditEntry, simple) — audit reporting

### Background-job-only methods last

20. **Row 43** — `ReorderService.GenerateSuggestionsAsync` (ReorderConfig, joined) — background reorder engine

---

## Include-Graph Methods (Fix complexity = graph)

These 9 methods require manual JOIN SQL plus per-row entity reassembly. They are riskier than the single-table `SELECT … FROM table` shape and should receive extra budget in INT-15 and INT-16 planning.

| Row | Batch | File | Method | Include structure |
|---|---|---|---|---|
| 1 | INT-15 | StockDashboardService.vb | GetDashboardDataAsync | `.Include(Category).Include(StockBatches)` — two navigation collections per Product |
| 8 | INT-15 | ShrinkageService.vb | GetShrinkageHistoryAsync | `.Include(Product).Include(StockBatch)` — two navigation references per ShrinkageRecord |
| 14 | INT-15 | VelocityService.vb | ClassifyAllProductsAsync | `.Include(Category).Include(StockBatches).Include(ShrinkageRecords)` — three collections per Product |
| 27 | INT-15 | ReceiptIntegrityService.vb | ValidateChainAsync | `.Include(Receipt).ThenInclude(Transaction).ThenInclude(Lines)` — nested three-level graph per ReceiptIntegrity |
| 28 | INT-16 | PurchaseOrderService.vb | GetAllAsync | `.Include(Lines).Include(Vendor)` — two navigations per PurchaseOrder |
| 30 | INT-16 | AccountsPayableService.vb | GetAllOutstandingAsync | `.Include(Vendor).Include(PurchaseOrder)` — two navigations per AccountsPayableEntry |
| 31 | INT-16 | AccountsPayableService.vb | GetAllAsync | `.Include(Vendor).Include(PurchaseOrder)` — two navigations per AccountsPayableEntry |
| 35 | INT-16 | AccountsPayableService.vb | GetByVendorAsync | `.Include(Vendor).Include(PurchaseOrder)` — two navigations per AccountsPayableEntry |
| 36 | INT-16 | AccountsPayableService.vb | GetOverdueAsync | `.Include(Vendor).Include(PurchaseOrder)` — two navigations per AccountsPayableEntry |

**INT-15 graph rows:** 4  
**INT-16 graph rows:** 5  
**Total graph rows:** 9
