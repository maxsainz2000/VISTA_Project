Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Data
Imports MerchSys.App.Services
Imports MerchSys.App.Startup
Imports MerchSys.App.ViewModels
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.Inventory.ViewModels
Imports MerchSys.POS.Services
Imports MerchSys.POS.ViewModels
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.Purchasing.Extensions
Imports MerchSys.Purchasing.ViewModels

Class Application

    Private _host As IHost

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureServices(Sub(services)

                                      ' Infrastructure: Session
                                      services.AddSingleton(Of ISessionService, DefaultSessionService)()

                                      ' Infrastructure: EventBus (MediatR adapter — required by POS services)
                                      services.AddScoped(Of IEventBus, MediatREventBus)()

                                      ' Infrastructure: DbContexts
                                      services.AddModuleDbContexts()

                                      ' Infrastructure: MediatR (all module handler assemblies)
                                      services.AddMediatRServices()

                                      ' Infrastructure: Sync worker & probe
                                      services.AddSyncServices($"Data Source={DatabaseConfig.DatabasePath}")

                                      ' ── Purchasing ────────────────────────────────────────
                                      services.AddPurchasingServices()

                                      ' Missing Purchasing ViewModels (not in AddPurchasingServices)
                                      services.AddTransient(Of PurchaseOrderListViewModel)()
                                      services.AddTransient(Of GoodsReceivingViewModel)()
                                      services.AddTransient(Of VendorListViewModel)()

                                      ' ── Inventory ─────────────────────────────────────────
                                      services.AddScoped(Of IStockService, StockService)()
                                      services.AddScoped(Of IExpiryTrackingService, ExpiryTrackingService)()
                                      services.AddScoped(Of IStockDashboardService, StockDashboardService)()
                                      services.AddScoped(Of ILowStockAlertService, LowStockAlertService)()
                                      services.AddSingleton(Of ILowStockNotifier, WpfLowStockNotifier)()
                                      services.AddScoped(Of IShrinkageService, ShrinkageService)()
                                      services.AddScoped(Of IVelocityService, VelocityService)()
                                      services.AddScoped(Of IStockoutEstimationService, StockoutEstimationService)()
                                      services.AddScoped(Of IInventoryAuditService, InventoryAuditService)()
                                      services.AddTransient(Of StockDashboardViewModel)()
                                      services.AddTransient(Of ProductManagementViewModel)()
                                      services.AddTransient(Of ExpiryMonitorViewModel)()
                                      services.AddTransient(Of ShrinkageViewModel)()

                                      ' ── POS ───────────────────────────────────────────────
                                      services.AddScoped(Of ICartService, CartService)()
                                      services.AddScoped(Of IPaymentService, PaymentService)()
                                      services.AddScoped(Of ICreditService, CreditService)()
                                      services.AddScoped(Of ISalesReturnService, SalesReturnService)()
                                      services.AddScoped(Of IReceiptService, ReceiptService)()
                                      services.AddScoped(Of IDailySummaryService, DailySummaryService)()
                                      services.AddTransient(Of SalesCartViewModel)()
                                      services.AddTransient(Of CreditManagementViewModel)()
                                      services.AddTransient(Of TransactionHistoryViewModel)()
                                      services.AddTransient(Of DailySummaryViewModel)()

                                      ' ── Accounting ────────────────────────────────────────
                                      services.AddScoped(Of IFinancialOverviewService, FinancialOverviewService)()
                                      services.AddScoped(Of IIncomeStatementService, IncomeStatementService)()
                                      services.AddScoped(Of ISalesSummaryService, SalesSummaryService)()
                                      services.AddScoped(Of IWhatThisMeansService, WhatThisMeansService)()
                                      services.AddTransient(Of FinancialOverviewViewModel)()
                                      services.AddTransient(Of IncomeStatementViewModel)()
                                      services.AddTransient(Of SalesSummaryViewModel)()

                                      ' ── Views (UserControls) ──────────────────────────────
                                      services.AddTransient(Of Views.POS.SalesCartView)()
                                      services.AddTransient(Of Views.POS.CreditManagementView)()
                                      services.AddTransient(Of Views.POS.TransactionHistoryView)()
                                      services.AddTransient(Of Views.POS.DailySummaryView)()
                                      services.AddTransient(Of Views.Purchasing.PurchaseOrderListView)()
                                      services.AddTransient(Of Views.Purchasing.GoodsReceivingView)()
                                      services.AddTransient(Of Views.Purchasing.VendorDirectoryView)()
                                      services.AddTransient(Of Views.Purchasing.APLedgerView)()
                                      services.AddTransient(Of Views.Purchasing.ReorderSuggestionsView)()
                                      services.AddTransient(Of Views.Inventory.StockDashboardView)()
                                      services.AddTransient(Of Views.Inventory.ProductManagementView)()
                                      services.AddTransient(Of Views.Inventory.ExpiryMonitorView)()
                                      services.AddTransient(Of Views.Inventory.ShrinkageView)()
                                      services.AddTransient(Of Views.Accounting.FinancialOverviewView)()
                                      services.AddTransient(Of Views.Accounting.IncomeStatementView)()
                                      services.AddTransient(Of Views.Accounting.SalesSummaryView)()

                                      ' ── Shell ─────────────────────────────────────────────
                                      services.AddSingleton(Of MainWindowViewModel)()
                                      services.AddSingleton(Of MainWindow)()

                                  End Sub)

        _host = builder.Build()
        _host.Start()

        ' Apply database migrations (EF Core 10 CLI cannot discover VB.NET migrations)
        DatabaseInitializer.Initialize($"Data Source={DatabaseConfig.DatabasePath}")

        ' Initialise Notification.Wpf NotificationManager on the UI thread
        _host.Services.GetRequiredService(Of ILowStockNotifier)()

        ' Show the main window
        Dim window = _host.Services.GetRequiredService(Of MainWindow)()
        window.Show()
    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs)
        _host?.StopAsync().GetAwaiter().GetResult()
        _host?.Dispose()
    End Sub

End Class
