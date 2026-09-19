# INFRA-20 Write-Path Audit: OWASP DA5 Data-Layer Compliance

This audit documents and classifies every database write site (insert, update, delete) in the VISTA solution to ensure role-based write rejection (OWASP DA5) is applied correctly.

## Classification Guide
- **U (User)**: Originates from a user-initiated UI action (WPF views, ViewModels commands). Rejected for Owner role.
- **S (System)**: Background tasks, scheduled services, or async event handlers responding to domain triggers. Always allowed.
- **A (Auth)**: Personal credential management (e.g., self password changes). Allowed for Owner ONLY when modifying their own account row.

---

## Audited Write Sites

| File:Line | Class | Justification |
|---|---|---|
| `MerchSys.App/Services/IAuthenticationService.vb:164` | A | `AuthenticationService.ChangePasswordCore` — Owner updates own credentials |
| `MerchSys.App/Services/IAuthenticationService.vb:207` | S | `AuthenticationService.ClearFailedAttempts` — system updating lockout state |
| `MerchSys.App/Services/IAuthenticationService.vb:225` | S | `AuthenticationService.BumpFailedAttempts` — system updating lockout state |
| `MerchSys.App/Services/SyncOrchestrator.vb:180` | S | `SyncOrchestrator.IncrementAttemptAsync` — background sync writing journal updates |
| `MerchSys.App/Data/DatabaseInitializer.vb` | S | Startup database setup and seeding — unauthenticated/system context |
| `MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb:61` | S | `SyncableRepositoryCore.SaveWithJournalAsync` — core journal data save |
| `MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb:74` | S | `SyncableRepositoryCore.SaveWithJournalAsync` — journal sync log save |
| `MerchSys.Purchasing/Data/PurchasingSyncableRepository.vb:85` | S | `MarkSyncedAsync` — background sync marking journal rows |
| `MerchSys.Inventory/Data/InventorySyncableRepository.vb:85` | S | `MarkSyncedAsync` — background sync marking journal rows |
| `MerchSys.POS/Data/PosSyncableRepository.vb:85` | S | `MarkSyncedAsync` — background sync marking journal rows |
| `MerchSys.Accounting/Data/AccountingSyncableRepository.vb:85` | S | `MarkSyncedAsync` — background sync marking journal rows |
| `MerchSys.SharedKernel/Sync/MariaDbSyncContext.vb` | S | Remote MariaDb sync context raw-SQL operations — system-initiated |
| `MerchSys.Accounting/Handlers/CreditPaymentAccountingHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/ReceiptTamperDetectedHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/SaleCompletedWithVatHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Accounting/Handlers/ShrinkageAccountingHandler.vb` | S | Accounting MediatR event handler — background updates |
| `MerchSys.Inventory/Handlers/GoodsReceivedHandler.vb` | S | Inventory MediatR event handler — background updates |
| `MerchSys.Inventory/Handlers/SaleCompletedHandler.vb` | S | Inventory MediatR event handler — background updates |
| `MerchSys.Inventory/Handlers/ShrinkageRecordedHandler.vb` | S | Inventory MediatR event handler — background updates |
| `MerchSys.Inventory/Handlers/StockReturnedEventHandler.vb` | S | Inventory MediatR event handler — background updates |
| `MerchSys.Inventory/Services/LowStockAlertService.vb:74` | S | low-stock alerts — background service scheduled checks |
| `MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb` | U | Product / category additions and updates — user click |
| `MerchSys.POS/Services/CreditService.vb` | U | Credit account modifications — user action |
| `MerchSys.POS/Services/CartService.vb` | U | Sales cart modifications — user action |
| `MerchSys.POS/Services/PaymentService.vb` | U | Payments and checkout — user action |
| `MerchSys.POS/Services/ReceiptService.vb` | U | Receipt generation — user action |
| `MerchSys.POS/Services/ReceiptIntegrityService.vb` | U | Integrity hashing — user action |
| `MerchSys.POS/Services/SalesReturnService.vb` | U | Returns processing — user action |
| `MerchSys.Purchasing/Services/VendorService.vb` | U | Vendor CRUD — user action |
| `MerchSys.Purchasing/Services/PriceChangeService.vb` | U | Price adjustments — user action |
| `MerchSys.Purchasing/Services/PurchaseOrderService.vb` | U | Purchase order operations — user action |
| `MerchSys.Purchasing/Services/GoodsReceivingService.vb` | U | Goods receiving — user action |
| `MerchSys.Purchasing/Services/AccountsPayableService.vb` | U | AP invoice adjustments — user action |
| `MerchSys.Purchasing/Services/ReorderService.vb` | U | Reorder adjustments — user action |
| `MerchSys.Inventory/Services/StockService.vb` | U | Stock level overrides — user action |
| `MerchSys.Inventory/Services/ShrinkageService.vb` | U | Shrinkage recordings — user action |
| `MerchSys.Inventory/Services/InventoryAuditService.vb` | U | Stock counts — user action |
| `MerchSys.Inventory/Services/ExpiryTrackingService.vb` | U | Expiry warnings / actions — user action |
| `MerchSys.Accounting/Services/FinancialOverviewService.vb` | U | Period closure and manual reconciliations — user action |
| `MerchSys.Accounting/Services/VatReportingService.vb` | U | VAT filings — user action |
