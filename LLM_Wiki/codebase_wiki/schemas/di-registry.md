---
type: schema-map
last-updated: 2026-05-09
---

# Dependency Injection Registry

This page documents the composition root in `MerchSys.App`.

## POS Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IReceiptService` | `ReceiptService` | Scoped |
| `IDailySummaryService` | `DailySummaryService` | Scoped |
| (none) | `SalesCartViewModel` | Transient |
| (none) | `CreditManagementViewModel` | Transient |
| (none) | `TransactionHistoryViewModel` | Transient |
| (none) | `DailySummaryViewModel` | Transient |
| *Pending* | *ICartService, IPaymentService, ICreditService, ISalesReturnService* | *Not yet registered* |

## Inventory Services
| Interface | Implementation | Lifetime |
|---|---|---|
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
| *Pending* | *IStockService, IInventoryAuditService* | *Not yet registered* |

## Purchasing Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IPurchaseOrderService` | `PurchaseOrderService` | Scoped |
| `IPriceChangeService` | `PriceChangeService` | Scoped |
| `IGoodsReceivingService` | `GoodsReceivingService` | Scoped |
| `IVendorService` | `VendorService` | Scoped |
| `IAccountsPayableService` | `AccountsPayableService` | Scoped |
| `IReorderService` | `ReorderService` | Scoped |
| (none) | `PurchaseOrderListViewModel` | Transient |
| (none) | `GoodsReceivingViewModel` | Transient |
| (none) | `VendorListViewModel` | Transient |
| (none) | `APLedgerViewModel` | Transient |
| (none) | `ReorderSuggestionsViewModel` | Transient |

## Accounting Services
| Interface | Implementation | Lifetime |
|---|---|---|
| (none) | `FinancialOverviewViewModel` | Transient |
| (none) | `IncomeStatementViewModel` | Transient |
| (none) | `SalesSummaryViewModel` | Transient |
| *Pending* | *IFinancialOverviewService, IIncomeStatementService, ISalesSummaryService, IWhatThisMeansService* | *Not yet registered* |

## Views (UserControls)
| View | Module | Lifetime |
|---|---|---|
| `SalesCartView` | POS | Transient |
| `CreditManagementView` | POS | Transient |
| `TransactionHistoryView` | POS | Transient |
| `DailySummaryView` | POS | Transient |
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

## Shell Components
| Class | Lifetime | Description |
|---|---|---|
| `MainWindowViewModel` | Singleton | Main navigation hub and state manager for the shell. |
| `MainWindow` | Singleton | Main application window. |

## Shared Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IEventBus` | `MediatR` | Transient (by MediatR default) |
| `ISessionService` | `DefaultSessionService` | Singleton |
| (Extension) | `AddModuleDbContexts()` | Registers all module DbContexts (Scoped) |
| (Extension) | `AddMediatRServices()` | Registers MediatR and all module handlers |
