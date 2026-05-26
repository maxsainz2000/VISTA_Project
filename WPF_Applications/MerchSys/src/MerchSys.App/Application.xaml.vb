Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Configuration
Imports MerchSys.App.Data
Imports MerchSys.App.Services
Imports MerchSys.App.Startup
Imports MerchSys.App.ViewModels
Imports MerchSys.App.ViewModels.Shell
Imports MerchSys.App.Views
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
    Private _loginView As LoginView = Nothing
    Private _mainWindow As MainWindow = Nothing

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureAppConfiguration(Sub(ctx, cfg)
                                              cfg.AddProductionOverlay()
                                          End Sub)

        builder.ConfigureServices(Sub(services)

                                      ' Infrastructure: Session (LoginSessionService replaces DefaultSessionService in all builds)
                                      services.AddSingleton(Of LoginSessionService)()
                                      services.AddSingleton(Of ISessionService)(Function(sp) sp.GetRequiredService(Of LoginSessionService)())

#If DEBUG Then
                                      ' ── DEBUG bypass: set VISTA_BYPASS_LOGIN=1 to skip authentication ────
                                      ' The #If DEBUG guard ensures this code cannot ship to production.
                                      ' DefaultSessionService is kept only for developer iteration speed.
                                      If Environment.GetEnvironmentVariable("VISTA_BYPASS_LOGIN") = "1" Then
                                          ' Re-register ISessionService with the stub so the login view is bypassed
                                          services.AddSingleton(Of ISessionService, DefaultSessionService)()
                                      End If
#End If

                                      ' Infrastructure: Authentication
                                      services.AddTransient(Of IAuthenticationService)(
                                          Function(sp) New AuthenticationService($"Data Source={DatabaseConfig.DatabasePath}"))

                                      ' Infrastructure: Login UI
                                      services.AddTransient(Of LoginViewModel)()
                                      services.AddTransient(Of LoginView)()

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
                                      services.AddScoped(Of IVatReliefReportService, VatReliefReportService)()
                                      services.AddTransient(Of FinancialOverviewViewModel)()
                                      services.AddTransient(Of IncomeStatementViewModel)()
                                      services.AddTransient(Of SalesSummaryViewModel)()
                                      services.AddTransient(Of VatReturnViewModel)()
                                      services.AddTransient(Of TamperAuditReportViewModel)()
                                      services.AddTransient(Of VatReliefReportViewModel)()

                                      ' ── Owner Dashboard (INFRA-16) ────────────────────────
                                      services.AddTransient(Of OwnerDashboardViewModel)()
                                      services.AddTransient(Of Views.OwnerDashboardView)()

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
                                      services.AddTransient(Of Views.Accounting.VatReliefReportView)()
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

#If DEBUG Then
        DebugHostHolder.CurrentHost = _host
#End If

        DatabaseInitializer.Initialize($"Data Source={DatabaseConfig.DatabasePath}")

        _host.Services.GetRequiredService(Of ILowStockNotifier)()

        ' Wire MainWindowViewModel.LogoutRequested once (singleton)
        Dim mainVm = _host.Services.GetRequiredService(Of MainWindowViewModel)()
        AddHandler mainVm.LogoutRequested, AddressOf HandleLogoutRequested

        ShowLoginView()
    End Sub

    ' ── Login / logout flow ───────────────────────────────────────────────────

    Private Sub ShowLoginView()
        _loginView = _host.Services.GetRequiredService(Of LoginView)()
        _loginView.ViewModel.Reset()
        AddHandler _loginView.ViewModel.LoginSucceeded, AddressOf HandleLoginSucceeded
        AddHandler _loginView.Closed, AddressOf HandleLoginViewClosed
        _loginView.Show()
    End Sub

    Private Sub HandleLoginSucceeded(sender As Object, e As EventArgs)
        RemoveHandler _loginView.ViewModel.LoginSucceeded, AddressOf HandleLoginSucceeded
        RemoveHandler _loginView.Closed, AddressOf HandleLoginViewClosed
        _loginView.Hide()

        If _mainWindow Is Nothing Then
            _mainWindow = _host.Services.GetRequiredService(Of MainWindow)()
            AddHandler _mainWindow.Closed, AddressOf HandleMainWindowClosed
        End If

        Dim mainVm = _host.Services.GetRequiredService(Of MainWindowViewModel)()
        mainVm.RefreshNavigation()
        _mainWindow.Show()
        mainVm.NavigateToDefault()
    End Sub

    Private Sub HandleLogoutRequested(sender As Object, e As EventArgs)
        _mainWindow.Hide()
        ShowLoginView()
    End Sub

    Private Sub HandleMainWindowClosed(sender As Object, e As EventArgs)
        Shutdown()
    End Sub

    ' Closing the login view without logging in shuts the application down.
    Private Sub HandleLoginViewClosed(sender As Object, e As EventArgs)
        Shutdown()
    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs)
        _host?.StopAsync().GetAwaiter().GetResult()
        _host?.Dispose()
    End Sub

End Class
