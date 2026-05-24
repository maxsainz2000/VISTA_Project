# Codebase-Wide Integrity Audit Report

**Date/Time:** 2026-05-24
**Total Audited VB.NET Files:** 340
**Total Audited XAML Files:** 25

## Rule Verification Checklist

| ID | Rule Name | Category | Status | Violations Found |
|---|---|---|---|---|
| 1 | [EF Core HasDefaultValue — enum property rejects int literal](../../LLM_Wiki/agent_wiki/errors/efcore-hasdefaultvalue-enum-type-mismatch.md) | Configuration | ✅ PASS | 0 |
| 2 | [EF Core temp key in sync journal payload](../../LLM_Wiki/agent_wiki/errors/efcore-temp-key-sync-journal-payload.md) | Runtime Logic | ✅ PASS | 0 |
| 3 | [EF Core 10 VB.NET — ToListAsync silently returns empty list](../../LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md) | Runtime Logic | ❌ FAIL | 63 |
| 4 | [EF Core 10 CLI Cannot Discover VB.NET Migration Classes](../../LLM_Wiki/agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md) | CLI & Discovery | ✅ PASS | 0 |
| 5 | [SQLite trigger cannot reference objects in database temp](../../LLM_Wiki/agent_wiki/errors/sqlite-trigger-no-temp-reference.md) | Database | ✅ PASS | 0 |
| 6 | [Reserved Keyword as Enum Member Name](../../LLM_Wiki/agent_wiki/errors/vbnet-reserved-keyword-enum-member.md) | Syntax | ✅ PASS | 0 |
| 7 | [Console namespace shadow in VB.NET](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-console-namespace-shadow.md) | Syntax & Imports | ❌ FAIL | 8 |
| 8 | [VB.NET cstr keyword collision](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-cstr-keyword-collision.md) | Syntax | ✅ PASS | 0 |
| 9 | [VB.NET Err built-in shadows loop variable](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-err-builtin-shadows-loop-variable.md) | Syntax & Scope | ✅ PASS | 0 |
| 10 | [VB.NET lambda parameter shadows local variable](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-lambda-param-shadows-local-variable.md) | Scope | ✅ PASS | 0 |
| 11 | [VB.NET leading dot fluent chains outside With block](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-leading-dot-fluent-chains.md) | Syntax | ✅ PASS | 0 |
| 12 | [VB.NET list count property shadows linq extension](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-list-count-property-shadows-linq-extension.md) | Member Resolution | ❌ FAIL | 1 |
| 13 | [VB.NET loop variable shadows DBContext method](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-loop-variable-shadows-dbcontext-method.md) | Scope | ✅ PASS | 0 |
| 14 | [VB.NET parameter name case-insensitively shadows a property](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-parameter-shadows-property.md) | Scope | ❌ FAIL | 20 |
| 15 | [WPF same-project clr-namespace missing root namespace in XAML](../../LLM_Wiki/agent_wiki/antipatterns/wpf-vbnet-clr-namespace-missing-rootnamespace.md) | XAML Namespace | ✅ PASS | 0 |
| 16 | [ViewModel Timer in ClassLib (no DispatcherTimer)](../../LLM_Wiki/agent_wiki/patterns/classlib-viewmodel-auto-refresh-timer.md) | WPF / ClassLib | ✅ PASS | 0 |
| 17 | [Sync transmitter delete carrying no payload](../../LLM_Wiki/agent_wiki/patterns/sync-transmit-delete-no-payload.md) | Runtime Logic | ✅ PASS | 0 |
| 18 | [Doubling root namespace in Namespace declarations](../../LLM_Wiki/agent_wiki/patterns/vbnet-rootnamespace-relative-declarations.md) | Structure | ✅ PASS | 0 |
| 19 | [WPF MainWindow is not shell window](../../LLM_Wiki/agent_wiki/patterns/wpf-mainwindow-not-shell-window.md) | WPF Navigation | ❌ FAIL | 1 |

## Detailed Audit Findings

### Rule 1: EF Core HasDefaultValue — enum property rejects int literal
**Wiki Reference:** [errors/efcore-hasdefaultvalue-enum-type-mismatch.md](../../LLM_Wiki/agent_wiki/errors/efcore-hasdefaultvalue-enum-type-mismatch.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 2: EF Core temp key in sync journal payload
**Wiki Reference:** [errors/efcore-temp-key-sync-journal-payload.md](../../LLM_Wiki/agent_wiki/errors/efcore-temp-key-sync-journal-payload.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 3: EF Core 10 VB.NET — ToListAsync silently returns empty list
**Wiki Reference:** [errors/efcore-vbnet-tolistasync-entity-empty.md](../../LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md)

*63 violation(s) found:*

| File | Line | Code | Details |
|---|---|---|---|
| [FinancialOverviewService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/FinancialOverviewService.vb) | 90 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [FinancialOverviewService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/FinancialOverviewService.vb) | 120 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [IncomeStatementService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/IncomeStatementService.vb) | 73 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ITamperAuditQueryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb) | 39 | `ToListAsync(CancellationToken.None)` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ITamperAuditQueryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb) | 53 | `ToListAsync(CancellationToken.None)` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesSummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/SalesSummaryService.vb) | 62 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesSummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/SalesSummaryService.vb) | 104 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesSummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/SalesSummaryService.vb) | 155 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) | 170 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) | 270 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) | 275 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [GetProductCatalogQueryHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb) | 45 | `Dim products = Await query.OrderBy(Function(p) p.Name).ToListAsync(cancellationToken)` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ExpiryTrackingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ExpiryTrackingService.vb) | 40 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ExpiryTrackingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ExpiryTrackingService.vb) | 71 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [InventoryAuditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/InventoryAuditService.vb) | 156 | `Return Await query.OrderByDescending(Function(a) a.AuditedAt).ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [InventoryAuditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/InventoryAuditService.vb) | 164 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [LowStockAlertService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/LowStockAlertService.vb) | 82 | `ToListAsync()).` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [LowStockAlertService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/LowStockAlertService.vb) | 92 | `ToListAsync()).` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ShrinkageService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ShrinkageService.vb) | 82 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ShrinkageService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ShrinkageService.vb) | 149 | `Return Await query.OrderByDescending(Function(r) r.RecordedDate).ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [StockDashboardService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockDashboardService.vb) | 34 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb) | 77 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb) | 120 | `Dim products As List(Of Product) = Await query.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb) | 141 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VelocityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/VelocityService.vb) | 29 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VelocityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/VelocityService.vb) | 36 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ProductManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb) | 344 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ProductManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb) | 364 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb) | 163 | `Return Await query.OrderByDescending(Function(t) t.TransactionDate).ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb) | 62 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb) | 73 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb) | 151 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb) | 172 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [DailySummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/DailySummaryService.vb) | 40 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [DailySummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/DailySummaryService.vb) | 44 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [DailySummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/DailySummaryService.vb) | 96 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [DailySummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/DailySummaryService.vb) | 100 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReceiptIntegrityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptIntegrityService.vb) | 110 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesReturnService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/SalesReturnService.vb) | 115 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesReturnService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/SalesReturnService.vb) | 124 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [SalesReturnService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/SalesReturnService.vb) | 136 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReceiptArchivalService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb) | 183 | `}).Take(candidateCount).ToListAsync(cancellationToken)` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [CreditManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/CreditManagementViewModel.vb) | 361 | `.ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | 39 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | 99 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | 108 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | 118 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | 139 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb) | 56 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb) | 162 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [PriceChangeService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PriceChangeService.vb) | 83 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [PriceChangeService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PriceChangeService.vb) | 103 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb) | 30 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb) | 77 | `Return Await query.OrderByDescending(Function(po) po.OrderDate).ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 35 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 42 | `ToListAsync())` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 93 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 113 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 191 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb) | 197 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VendorService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/VendorService.vb) | 58 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VendorService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/VendorService.vb) | 122 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |
| [VendorService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/VendorService.vb) | 141 | `ToListAsync()` | Full entity ToListAsync() query risk (potential silent empty list bug) |

---

### Rule 4: EF Core 10 CLI Cannot Discover VB.NET Migration Classes
**Wiki Reference:** [errors/efcore10-vbnet-migration-discovery-bug.md](../../LLM_Wiki/agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 5: SQLite trigger cannot reference objects in database temp
**Wiki Reference:** [errors/sqlite-trigger-no-temp-reference.md](../../LLM_Wiki/agent_wiki/errors/sqlite-trigger-no-temp-reference.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 6: Reserved Keyword as Enum Member Name
**Wiki Reference:** [errors/vbnet-reserved-keyword-enum-member.md](../../LLM_Wiki/agent_wiki/errors/vbnet-reserved-keyword-enum-member.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 7: Console namespace shadow in VB.NET
**Wiki Reference:** [antipatterns/vbnet-console-namespace-shadow.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-console-namespace-shadow.md)

*8 violation(s) found:*

| File | Line | Code | Details |
|---|---|---|---|
| [ReceiptSequenceHarnessReport.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptSequenceHarnessReport.vb) | 130 | `Console.WriteLine($"[ReceiptSequenceHarnessReport] {If(passed, "PASS", "FAIL")} — Report: {reportPath}")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [ReceiptSequenceHarnessReport.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptSequenceHarnessReport.vb) | 132 | `Console.WriteLine($"[ReceiptSequenceHarnessReport] Warning: scratch DB cleanup failed: {cleanupEx.Message}")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 53 | `Console.WriteLine($"[Harness] Starting {CallCount} parallel GetNextReceiptNumberAsync calls for year {year}...")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 79 | `Console.WriteLine($"[Harness] Generated {results.Count} numbers, {distinct} unique.")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 82 | `Console.WriteLine($"[FAIL] Expected {CallCount} unique numbers, got {distinct}.")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 92 | `Console.WriteLine($"[FAIL] {missing.Count} expected numbers missing from results.")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 94 | `Console.WriteLine($"  Missing: {m}")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |
| [Pos.SequenceConcurrencyHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb) | 97 | `Console.WriteLine($"[PASS] All {CallCount} numbers are unique and form a contiguous sequence.")` | Console method call is shadowed by Microsoft.Extensions.Logging.Console namespace |

---

### Rule 8: VB.NET cstr keyword collision
**Wiki Reference:** [antipatterns/vbnet-cstr-keyword-collision.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-cstr-keyword-collision.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 9: VB.NET Err built-in shadows loop variable
**Wiki Reference:** [antipatterns/vbnet-err-builtin-shadows-loop-variable.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-err-builtin-shadows-loop-variable.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 10: VB.NET lambda parameter shadows local variable
**Wiki Reference:** [antipatterns/vbnet-lambda-param-shadows-local-variable.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-lambda-param-shadows-local-variable.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 11: VB.NET leading dot fluent chains outside With block
**Wiki Reference:** [antipatterns/vbnet-leading-dot-fluent-chains.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-leading-dot-fluent-chains.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 12: VB.NET list count property shadows linq extension
**Wiki Reference:** [antipatterns/vbnet-list-count-property-shadows-linq-extension.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-list-count-property-shadows-linq-extension.md)

*1 violation(s) found:*

| File | Line | Code | Details |
|---|---|---|---|
| [VatLedgerSchemaHarnessRunner.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Debug/VatLedgerSchemaHarnessRunner.vb) | 37 | `Dim passCount = checks.Count(Function(t) t.Item2 IsNot Nothing AndAlso t.Item2.Passed)` | Calling Count with lambda on List (resolves to Count property first in VB.NET) |

---

### Rule 13: VB.NET loop variable shadows DBContext method
**Wiki Reference:** [antipatterns/vbnet-loop-variable-shadows-dbcontext-method.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-loop-variable-shadows-dbcontext-method.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 14: VB.NET parameter name case-insensitively shadows a property
**Wiki Reference:** [antipatterns/vbnet-parameter-shadows-property.md](../../LLM_Wiki/agent_wiki/antipatterns/vbnet-parameter-shadows-property.md)

*20 violation(s) found:*

| File | Line | Code | Details |
|---|---|---|---|
| [VatReturnLockedException.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Exceptions/VatReturnLockedException.vb) | 26 | `Public Sub New(returnId As Integer, year As Integer, period As Integer, formType As VatReturnFormType)` | Parameter 'returnId' shadows class property/field 'returnId' case-insensitively |
| [VatReturnLockedException.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Exceptions/VatReturnLockedException.vb) | 26 | `Public Sub New(returnId As Integer, year As Integer, period As Integer, formType As VatReturnFormType)` | Parameter 'year' shadows class property/field 'year' case-insensitively |
| [VatReturnLockedException.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Exceptions/VatReturnLockedException.vb) | 26 | `Public Sub New(returnId As Integer, year As Integer, period As Integer, formType As VatReturnFormType)` | Parameter 'period' shadows class property/field 'period' case-insensitively |
| [VatReturnLockedException.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Exceptions/VatReturnLockedException.vb) | 26 | `Public Sub New(returnId As Integer, year As Integer, period As Integer, formType As VatReturnFormType)` | Parameter 'formType' shadows class property/field 'formType' case-insensitively |
| [VatReturnViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/ViewModels/VatReturnViewModel.vb) | 425 | `Private Sub PopulateFromReturn(vatReturn As VatReturn)` | Parameter 'vatReturn' shadows class property/field 'vatReturn' case-insensitively |
| [NavigationItem.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Models/NavigationItem.vb) | 28 | `Public Sub New(groupName As String, items As List(Of NavigationItem))` | Parameter 'groupName' shadows class property/field 'groupName' case-insensitively |
| [NavigationItem.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Models/NavigationItem.vb) | 28 | `Public Sub New(groupName As String, items As List(Of NavigationItem))` | Parameter 'items' shadows class property/field 'items' case-insensitively |
| [IAuthenticationService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb) | 249 | `Public Function Verify(password As String, storedHash As String) As Boolean` | Parameter 'storedHash' shadows class property/field 'storedHash' case-insensitively |
| [IAuthenticationService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb) | 273 | `Private Function ComputeHash(passwordBytes As Byte(), salt As Byte()) As Byte()` | Parameter 'salt' shadows class property/field 'salt' case-insensitively |
| [LoginView.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/LoginView.xaml.vb) | 16 | `Public Sub New(viewModel As LoginViewModel)` | Parameter 'viewModel' shadows class property/field 'viewModel' case-insensitively |
| [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb) | 20 | `Public Sub New(productId As Integer, requestedQty As Integer, availableQty As Integer)` | Parameter 'productId' shadows class property/field 'productId' case-insensitively |
| [VelocityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/VelocityService.vb) | 124 | `Private Shared Function Classify(avgDailySales As Decimal) As String` | Parameter 'avgDailySales' shadows class property/field 'avgDailySales' case-insensitively |
| [ExpiryMonitorViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb) | 234 | `Private Shared Function GetUrgencyLevel(daysRemaining As Integer) As String` | Parameter 'daysRemaining' shadows class property/field 'daysRemaining' case-insensitively |
| [ReceiptArchivalHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptArchivalHarness.vb) | 732 | `Public Function GetSection(key As String) As IConfigurationSection Implements IConfiguration.GetSection` | Parameter 'key' shadows class property/field 'key' case-insensitively |
| [ReceiptArchivalHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptArchivalHarness.vb) | 750 | `Public Sub New(key As String)` | Parameter 'key' shadows class property/field 'key' case-insensitively |
| [ReceiptArchivalHarness.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptArchivalHarness.vb) | 782 | `Public Function GetSection(key As String) As IConfigurationSection Implements IConfiguration.GetSection` | Parameter 'key' shadows class property/field 'key' case-insensitively |
| [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb) | 176 | `Private Shared Sub ValidateLineIndex(cart As CartDto, lineIndex As Integer)` | Parameter 'cart' shadows class property/field 'cart' case-insensitively |
| [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb) | 182 | `Private Sub RecalculateTotals(cart As CartDto)` | Parameter 'cart' shadows class property/field 'cart' case-insensitively |
| [ReceiptArchivalService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb) | 110 | `Private Sub LogBatchResult(result As ReceiptArchivalBatchResult)` | Parameter 'result' shadows class property/field 'result' case-insensitively |
| [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb) | 227 | `Private Shared Sub RecalculateTotal(po As PurchaseOrder)` | Parameter 'po' shadows class property/field 'po' case-insensitively |

---

### Rule 15: WPF same-project clr-namespace missing root namespace in XAML
**Wiki Reference:** [antipatterns/wpf-vbnet-clr-namespace-missing-rootnamespace.md](../../LLM_Wiki/agent_wiki/antipatterns/wpf-vbnet-clr-namespace-missing-rootnamespace.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 16: ViewModel Timer in ClassLib (no DispatcherTimer)
**Wiki Reference:** [patterns/classlib-viewmodel-auto-refresh-timer.md](../../LLM_Wiki/agent_wiki/patterns/classlib-viewmodel-auto-refresh-timer.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 17: Sync transmitter delete carrying no payload
**Wiki Reference:** [patterns/sync-transmit-delete-no-payload.md](../../LLM_Wiki/agent_wiki/patterns/sync-transmit-delete-no-payload.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 18: Doubling root namespace in Namespace declarations
**Wiki Reference:** [patterns/vbnet-rootnamespace-relative-declarations.md](../../LLM_Wiki/agent_wiki/patterns/vbnet-rootnamespace-relative-declarations.md)

*No violations found. The codebase is compliant with this rule.*

---

### Rule 19: WPF MainWindow is not shell window
**Wiki Reference:** [patterns/wpf-mainwindow-not-shell-window.md](../../LLM_Wiki/agent_wiki/patterns/wpf-mainwindow-not-shell-window.md)

*1 violation(s) found:*

| File | Line | Code | Details |
|---|---|---|---|
| [FinancialOverviewView.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb) | 33 | `' Application.Current.MainWindow is the LoginView (first window shown), not the shell.` | Use of Application.Current.MainWindow (may point to LoginView instead of shell) |

---
