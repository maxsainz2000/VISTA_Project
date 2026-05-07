---
type: schema-map
last-updated: 2026-05-07
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
| (none) | `APLedgerViewModel` | Transient |
| (none) | `ReorderSuggestionsViewModel` | Transient |
| *Pending* | *PurchaseOrderListViewModel, GoodsReceivingViewModel, VendorListViewModel* | *Not yet registered* |

## Accounting Services
| Interface | Implementation | Lifetime |
|---|---|---|
| (none) | `FinancialOverviewViewModel` | Transient |
| (none) | `IncomeStatementViewModel` | Transient |
| (none) | `SalesSummaryViewModel` | Transient |
| *Pending* | *IFinancialOverviewService, IIncomeStatementService, ISalesSummaryService, IWhatThisMeansService* | *Not yet registered* |

## Shared Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IEventBus` | `MediatR` | Transient (by MediatR default) |
| (Extension) | `AddModuleDbContexts()` | Registers all module DbContexts (Scoped) |
| (Extension) | `AddMediatRServices()` | Registers MediatR and all module handlers |
