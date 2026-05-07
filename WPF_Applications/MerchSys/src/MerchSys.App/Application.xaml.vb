Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Data
Imports MerchSys.App.Services
Imports MerchSys.App.Startup
Imports MerchSys.Inventory.Services
Imports MerchSys.Inventory.ViewModels
Imports MerchSys.POS.Services
Imports MerchSys.POS.ViewModels
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.Purchasing.Extensions

Class Application

    Private _host As IHost

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureServices(Sub(services)

                                      ' Infrastructure: DbContexts
                                      services.AddModuleDbContexts()

                                      ' Infrastructure: MediatR (all module handler assemblies)
                                      services.AddMediatRServices()

                                      ' ── Purchasing ────────────────────────────────────────
                                      services.AddPurchasingServices()

                                      ' ── Inventory ─────────────────────────────────────────
                                      services.AddScoped(Of IExpiryTrackingService, ExpiryTrackingService)()
                                      services.AddScoped(Of IStockDashboardService, StockDashboardService)()
                                      services.AddScoped(Of ILowStockAlertService, LowStockAlertService)()
                                      services.AddSingleton(Of ILowStockNotifier, WpfLowStockNotifier)()
                                      services.AddScoped(Of IShrinkageService, ShrinkageService)()
                                      services.AddScoped(Of IVelocityService, VelocityService)()
                                      services.AddScoped(Of IStockoutEstimationService, StockoutEstimationService)()
                                      services.AddTransient(Of StockDashboardViewModel)()
                                      services.AddTransient(Of ProductManagementViewModel)()
                                      services.AddTransient(Of ExpiryMonitorViewModel)()
                                      services.AddTransient(Of ShrinkageViewModel)()

                                      ' ── POS ───────────────────────────────────────────────
                                      services.AddScoped(Of IReceiptService, ReceiptService)()
                                      services.AddScoped(Of IDailySummaryService, DailySummaryService)()
                                      services.AddTransient(Of SalesCartViewModel)()
                                      services.AddTransient(Of CreditManagementViewModel)()
                                      services.AddTransient(Of TransactionHistoryViewModel)()
                                      services.AddTransient(Of DailySummaryViewModel)()

                                      ' ── Accounting ────────────────────────────────────────
                                      services.AddTransient(Of FinancialOverviewViewModel)()
                                      services.AddTransient(Of IncomeStatementViewModel)()
                                      services.AddTransient(Of SalesSummaryViewModel)()

                                  End Sub)

        _host = builder.Build()
        _host.Start()
    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs)
        _host?.StopAsync().GetAwaiter().GetResult()
        _host?.Dispose()
    End Sub

End Class
