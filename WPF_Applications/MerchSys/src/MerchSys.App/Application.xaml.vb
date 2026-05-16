Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Configuration
Imports MerchSys.App.Data
Imports MerchSys.App.Services
Imports MerchSys.App.Startup
Imports MerchSys.App.ViewModels
Imports MerchSys.App.ViewModels.Shell
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.Inventory.ViewModels
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.Purchasing.Extensions
Imports MerchSys.Purchasing.ViewModels
Imports MerchSys.Accounting.Services.Insights

Class Application

    Private _host As IHost

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        ' Overlay user-level production config (%LOCALAPPDATA%\VISTA\appsettings.Production.json)
        ' on top of the committed appsettings.json defaults. File is optional; missing = local-only mode.
        builder.ConfigureAppConfiguration(Sub(ctx, cfg)
                                              cfg.AddProductionOverlay()
                                          End Sub)

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

                                      ' Infrastructure: per-module syncable repositories (INFRA-09)
                                      services.AddSyncableRepositories()

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
                                      services.AddPosModule()

                                      ' ── Accounting ────────────────────────────────────────
                                      ' ACC-12: register concrete FinancialOverviewService so the decorator can resolve it
                                      services.AddScoped(Of FinancialOverviewService)()
                                      services.AddScoped(Of IFinancialOverviewService)(Function(sp)
                                                                                           Dim inner = sp.GetRequiredService(Of FinancialOverviewService)()
                                                                                           Dim providers = sp.GetServices(Of IKpiProvider)()
                                                                                           Return New VatEnrichedFinancialOverviewService(inner, providers)
                                                                                       End Function)
                                      services.AddScoped(Of IKpiProvider, VatPayableKpiProvider)()
                                      services.AddScoped(Of IFinancialInsightProvider, VatPayableInsightProvider)()
                                      services.AddScoped(Of IIncomeStatementService, IncomeStatementService)()
                                      services.AddScoped(Of ISalesSummaryService, SalesSummaryService)()
                                      services.AddScoped(Of IWhatThisMeansService, WhatThisMeansService)()
                                      services.AddScoped(Of IVatReportingService, VatReportingService)()
                                      services.AddScoped(Of IVatReturnExporter, VatReturnExporter)()
                                      services.AddScoped(Of ITamperAuditQueryService, TamperAuditQueryService)()
                                      services.AddTransient(Of FinancialOverviewViewModel)()
                                      services.AddTransient(Of IncomeStatementViewModel)()
                                      services.AddTransient(Of SalesSummaryViewModel)()
                                      services.AddTransient(Of VatReturnViewModel)()
                                      services.AddTransient(Of TamperAuditReportViewModel)()

                                      ' ── Views (UserControls) ──────────────────────────────
                                      services.AddTransient(Of Views.POS.SalesCartView)()
                                      services.AddTransient(Of Views.POS.CreditManagementView)()
                                      services.AddTransient(Of Views.POS.TransactionHistoryView)()
                                      services.AddTransient(Of Views.POS.DailySummaryView)()
                                      services.AddTransient(Of Views.POS.VatSettingsView)()
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
                                      services.AddTransient(Of Views.Accounting.VatReturnView)()
                                      services.AddTransient(Of Views.Accounting.TamperAuditReportView)()
                                      services.AddTransient(Of Views.Accounting.Components.VatPayableTile)()

                                      ' ── Shell ─────────────────────────────────────────────
                                      services.AddSingleton(Of SyncStatusIndicatorViewModel)()
                                      services.AddSingleton(Of Views.Shell.SyncStatusIndicator)()
                                      services.AddSingleton(Of MainWindowViewModel)()
                                      services.AddSingleton(Of MainWindow)()

#If DEBUG Then
                                      ' ── Developer Tools (Debug builds only, ACC-17) ────────
                                      services.AddDebugServices()
#End If

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
