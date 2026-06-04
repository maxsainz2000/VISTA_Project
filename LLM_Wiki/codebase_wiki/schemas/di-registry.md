---
type: schema-map
last-updated: 2026-06-03
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
| `IReceiptArchivalService` | `ReceiptArchivalService` | Scoped |
| (none) | `ReceiptArchivalService` | Singleton (HostedService) |
| `IOptions(Of ReceiptArchivalOptions)` | (Configuration) | Singleton |
| `IReceiptRenderer` | `ConsoleReceiptRenderer` or `PdfReceiptRenderer` (from configuration) | Scoped |
| `ConsoleReceiptRenderer` | `ConsoleReceiptRenderer` | Scoped |
| `PdfReceiptRenderer` | `PdfReceiptRenderer` | Scoped |
| `IOptions(Of ReceiptPdfOptions)` | (Configuration) | Singleton |
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
| (none) | `ProductPriceHistoryViewModel` | Transient |

## Purchasing Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IPurchaseOrderService` | `PurchaseOrderService` | Scoped |
| `IPriceChangeService` | `PriceChangeService` | Scoped |
| `IGoodsReceivingService` | `GoodsReceivingService` | Scoped |
| `IVendorService` | `VendorService` | Scoped |
| `IAccountsPayableService` | `AccountsPayableService` | Scoped |
| `IReorderService` | `ReorderService` | Scoped |
| `IVendorProductService` | `VendorProductService` | Scoped |
| (none) | `GoodsReceiptVatCalculator` | Scoped |
| (none) | `PurchaseOrderListViewModel` | Transient |
| (none) | `GoodsReceivingViewModel` | Transient |
| (none) | `VendorListViewModel` | Transient |
| (none) | `APLedgerViewModel` | Transient |
| (none) | `ReorderSuggestionsViewModel` | Transient |
| (none) | `VendorCatalogViewModel` | Transient |
| (none) | `PurchasingDashboardViewModel` | Transient |

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
| `ITamperReportExporter` | `TamperReportExporter` | Scoped |
| `IOptions(Of TamperReportExportOptions)` | (Configuration) | Singleton |
| `IVatReliefReportService` | `VatReliefReportService` | Scoped |
| (none) | `FinancialOverviewViewModel` | Transient |
| (none) | `IncomeStatementViewModel` | Transient |
| (none) | `SalesSummaryViewModel` | Transient |
| (none) | `VatReturnViewModel` | Transient |
| (none) | `TamperAuditReportViewModel` | Transient |
| (none) | `VatReliefReportViewModel` | Transient |

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
| `VendorCatalogView` | Purchasing | Transient |
| `APLedgerView` | Purchasing | Transient |
| `ReorderSuggestionsView` | Purchasing | Transient |
| `PurchasingDashboardView` | Purchasing | Transient |
| `StockDashboardView` | Inventory | Transient |
| `ProductManagementView` | Inventory | Transient |
| `ProductPriceHistoryView` | Inventory | Transient |
| `ExpiryMonitorView` | Inventory | Transient |
| `ShrinkageView` | Inventory | Transient |
| `FinancialOverviewView` | Accounting | Transient |
| `IncomeStatementView` | Accounting | Transient |
| `SalesSummaryView` | Accounting | Transient |
| `VatReturnView` | Accounting | Transient |
| `VatPayableTile` | Accounting | Transient |
| `TamperAuditReportView` | Accounting | Transient |
| `VatReliefReportView` | Accounting | Transient |
| `LoginView` | (Shell) | Transient |
| `OwnerDashboardView` | (Shell) | Transient |
| `SessionTimeoutWarningView` | (Shell) | Transient |
| `ConnectionStatusIndicator` | (Shell) | Transient |
| `ActivityRail` | (Shell) | Singleton |
| `ModuleDetailPanel` | (Shell) | Singleton |
| `CommandPalette` | (Shell) | Singleton |


## Shell Components
| Class | Lifetime | Description |
|---|---|---|
| `MainWindowViewModel` | Singleton | Main navigation hub and state manager for the shell. Contains the `AllNavigableItems` aggregator and controls command palette visibility state. |
| `MainWindow` | Singleton | Main application window. |
| `LoginViewModel` | Transient | VM for the standalone login window. |
| `OwnerDashboardViewModel` | Transient | VM for the owner dashboard. |
| `SessionTimeoutWarningViewModel` | Transient | VM for the inactivity warning countdown dialog. |
| `ActivityRailViewModel` | Singleton | VM for the master activity rail module navigation. |
| `ConnectionStatusViewModel` | Transient | VM for the connection health status indicator pill. |
| `CommandPaletteViewModel` | Singleton | VM for the Spotlight-style command palette overlay. Handles debounced search and sets active module before routing navigation. |


## Shared Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IEventBus` | `MediatREventBus` | Scoped |
| `INotificationService` | `DefaultNotificationService` | Singleton |
| `IConflictPresenter` | `DefaultConflictPresenter` | Singleton |
| `IConfirmationPresenter` | `DefaultConfirmationPresenter` | Singleton |
| `ISessionService` | `LoginSessionService` | Singleton |
| `IWriteContextScope` | `WriteContextScope` | Singleton |
| `RoleGuardInterceptor` | `RoleGuardInterceptor` | Scoped |
| `IAuthenticationService` | `AuthenticationService` | Transient |
| `IIdleMonitor` | `WpfIdleMonitor` | Singleton |
| `IdleMonitorOptions` | (Configuration) | Singleton |
| `IConnectionHealthMonitor` | `ConnectionHealthMonitor` | Singleton |
| `IThemeService` | `ThemeService` | Singleton |
| (Extension) | `AddModuleDbContexts()` | Registers all module DbContexts (Scoped) |
| (Extension) | `AddMediatRServices()` | Registers MediatR and all module handlers |
| (Extension) | `AddConnectionHealthMonitor()` | Registers Connection health monitor services |

## UX-14 — Concurrency-Guard Dependency Notes

> `IConflictPresenter` (Singleton, registered above) is injected as a constructor parameter into the following VMs as of UX-14. No new DI registrations were required — DI auto-resolves the Singleton into each Transient VM.

| ViewModel | Module | Write paths guarded |
|---|---|---|
| `ProductManagementViewModel` | Inventory | `SaveProductAsync`, `ToggleActiveAsync`, `SaveCategoryAsync`, `DeleteCategoryAsync` |
| `ShrinkageViewModel` | Inventory | `ExecuteRecordAsync` |
| `PurchaseOrderListViewModel` | Purchasing | `SaveDraftAsync`, `SubmitFromEditorAsync`, `SubmitSelectedAsync`, `DeleteSelectedAsync` |
| `ReorderSuggestionsViewModel` | Purchasing | `AcceptAsync` |
| `VendorListViewModel` | Purchasing | `SaveVendorAsync`, `DeleteSelectedAsync` |
| `TransactionHistoryViewModel` | POS | `ProcessReturnAsync` |

The five UX-06 VMs (`SalesCartViewModel`, `APLedgerViewModel`, `GoodsReceivingViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`) already had `IConflictPresenter` injected from UX-06; UX-14 consolidated their inline catch blocks onto the shared primitive.

## UX-20 — Confirmation-Guard Dependency Notes

> `IConfirmationPresenter` (Singleton, registered above) is injected as a constructor parameter into the following VMs to gate destructive, irreversible, or financial actions:

| ViewModel | Module | Routed Paths Gated | Confirm Level |
|---|---|---|---|
| `ProductManagementViewModel` | Inventory | `DeleteCategoryAsync` | Plain |
| `PurchaseOrderListViewModel` | Purchasing | `DeleteSelectedAsync`, `SubmitSelectedAsync`, `SubmitFromEditorAsync` | Plain |
| `VendorListViewModel` | Purchasing | `DeleteSelectedAsync` | Typed (Vendor Name) |
| `VendorCatalogViewModel` | Purchasing | `DeleteEntryAsync` | Plain |
| `TransactionHistoryViewModel` | POS | `ProcessReturnAsync` | Plain |

