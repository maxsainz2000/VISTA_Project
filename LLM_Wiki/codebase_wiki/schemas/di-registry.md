---
type: schema-map
last-updated: 2026-05-11
---

# Dependency Injection Registry

This page documents the composition root in `MerchSys.App`.

## POS Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `ICartService` | `CartService` | Scoped |
| `IPaymentService` | `PaymentService` | Scoped |
| `ICreditService` | `CreditService` | Scoped |
| `ISalesReturnService` | `SalesReturnService` | Scoped |
| `IReceiptService` | `VatAwareReceiptService` | Scoped |
| `IDailySummaryService` | `DailySummaryService` | Scoped |
| `IReceiptIntegrityService` | `ReceiptIntegrityService` | Scoped |
| `IReceiptBodyComposer` | `BirCompliantReceiptBodyComposer` | Scoped |
| `IVatCalculator` | `VatCalculator` | Scoped |
| `VatConfigurationLoader` | `VatConfigurationLoader` | Singleton |
| `IVatConfigurationWriter` | `VatConfigurationWriter` | Scoped |
| (none) | `SalesCartViewModel` | Transient |
| (none) | `CreditManagementViewModel` | Transient |
| (none) | `TransactionHistoryViewModel` | Transient |
| (none) | `DailySummaryViewModel` | Transient |
| (none) | `VatSettingsViewModel` | Transient |
| `(Extension)` | `AddPosModule()` | Scoped | Registers all POS services and ViewModels (`PosServiceRegistration.vb`). |

## Inventory Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IStockService` | `StockService` | Scoped |
| `IExpiryTrackingService` | `ExpiryTrackingService` | Scoped |
| `IStockDashboardService` | `StockDashboardService` | Scoped |
| `ILowStockAlertService` | `LowStockAlertService` | Scoped |
| `ILowStockNotifier` | `WpfLowStockNotifier` | Singleton |
| `IShrinkageService` | `ShrinkageService` | Scoped |
| `IVelocityService` | `VelocityService` | Scoped |
| `IStockoutEstimationService` | `StockoutEstimationService` | Scoped |
| (none) | `StockDashboardViewModel` | Transient |
| (none) | `ProductManagementViewModel` | Transient |
| (none) | `ExpiryMonitorViewModel` | Transient |
| (none) | `ShrinkageViewModel` | Transient |
| `IInventoryAuditService` | `InventoryAuditService` | Scoped |

## Purchasing Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IPurchaseOrderService` | `PurchaseOrderService` | Scoped |
| `IPriceChangeService` | `PriceChangeService` | Scoped |
| `IGoodsReceivingService` | `GoodsReceivingService` | Scoped |
| `IVendorService` | `VendorService` | Scoped |
| `IAccountsPayableService` | `AccountsPayableService` | Scoped |
| `IReorderService` | `ReorderService` | Scoped |
| (none) | `GoodsReceiptVatCalculator` | Scoped |
| (none) | `PurchaseOrderListViewModel` | Transient |
| (none) | `GoodsReceivingViewModel` | Transient |
| (none) | `VendorListViewModel` | Transient |
| (none) | `APLedgerViewModel` | Transient |
| (none) | `ReorderSuggestionsViewModel` | Transient |

## Accounting Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IFinancialOverviewService` | `VatEnrichedFinancialOverviewService` (Decorating `FinancialOverviewService`) | Scoped |
| `IKpiProvider` | `VatPayableKpiProvider` | Scoped |
| `IFinancialInsightProvider` | `VatPayableInsightProvider` | Scoped |
| `IIncomeStatementService` | `IncomeStatementService` | Scoped |
| `ISalesSummaryService` | `SalesSummaryService` | Scoped |
| `IWhatThisMeansService` | `WhatThisMeansService` | Scoped |
| `IVatReportingService` | `VatReportingService` | Scoped |
| `IVatReturnExporter` | `VatReturnExporter` | Scoped |
| `ITamperAuditQueryService` | `TamperAuditQueryService` | Scoped |
| (none) | `FinancialOverviewViewModel` | Transient |
| (none) | `IncomeStatementViewModel` | Transient |
| (none) | `SalesSummaryViewModel` | Transient |
| (none) | `VatReturnViewModel` | Transient |

## Views (UserControls)
| View | Module | Lifetime |
|---|---|---|
| `SalesCartView` | POS | Transient |
| `CreditManagementView` | POS | Transient |
| `TransactionHistoryView` | POS | Transient |
| `DailySummaryView` | POS | Transient |
| `VatSettingsView` | POS | Transient |
| `PurchaseOrderListView` | Purchasing | Transient |
| `GoodsReceivingView` | Purchasing | Transient |
| `VendorDirectoryView` | Purchasing | Transient |
| `APLedgerView` | Purchasing | Transient |
| `ReorderSuggestionsView` | Purchasing | Transient |
| `StockDashboardView` | Inventory | Transient |
| `ProductManagementView` | Inventory | Transient |
| `ExpiryMonitorView` | Inventory | Transient |
| `ShrinkageView` | Inventory | Transient |
| `FinancialOverviewView` | Accounting | Transient |
| `IncomeStatementView` | Accounting | Transient |
| `SalesSummaryView` | Accounting | Transient |
| `VatReturnView` | Accounting | Transient |
| `VatPayableTile` | Accounting | Transient |

## Shell Components
| Class | Lifetime | Description |
|---|---|---|
| `MainWindowViewModel` | Singleton | Main navigation hub and state manager for the shell. |
| `MainWindow` | Singleton | Main application window. |

## Syncable Repositories
| Interface | Implementation | Lifetime |
|---|---|---|
| `ISyncableRepository(Of PurchasingDbContext)` | `PurchasingSyncableRepository` | Scoped |
| `ISyncableRepository(Of InventoryDbContext)` | `InventorySyncableRepository` | Scoped |
| `ISyncableRepository(Of PosDbContext)` | `PosSyncableRepository` | Scoped |
| `ISyncableRepository(Of AccountingDbContext)` | `AccountingSyncableRepository` | Scoped |
| (Extension) | `AddSyncableRepositories()` | Scoped | Registers all four repositories (`SyncableRepositoryRegistration.vb`). |

## Shared Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IEventBus` | `MediatREventBus` | Scoped |
| `INotificationService` | `DefaultNotificationService` | Singleton |
| `ISyncProbe` | `DualConditionSyncProbe` | Singleton |
| `ISessionService` | `DefaultSessionService` | Singleton |
| `SyncOrchestrator` | `SyncOrchestrator` | Scoped |
| `SyncWorker` | `SyncWorker` | Singleton (HostedService) |
| `IConflictResolver` | `ConflictResolver` | Scoped |
| `MariaDbSyncContext` | `MariaDbSyncContext` | Scoped |
| `SyncJournalDbContext` | `SyncJournalDbContext` | Scoped |
| (Extension) | `AddModuleDbContexts()` | Registers all module DbContexts (Scoped) |
| (Extension) | `AddMediatRServices()` | Registers MediatR and all module handlers |
