Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports MerchSys.App.Configuration
Imports MerchSys.App.Data
Imports MerchSys.App.Services
Imports MerchSys.App.Startup
Imports MerchSys.App.Services.Theming
Imports MerchSys.App.ViewModels
Imports MerchSys.App.Views
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Data
Imports MerchSys.Inventory.ViewModels
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.Purchasing.Extensions
Imports MerchSys.Purchasing.ViewModels
Imports MerchSys.Accounting.Services.Insights
Imports QuestPDF.Infrastructure
Imports MerchSys.Accounting.Services.Reporting
Imports System.Windows.Threading

Class Application

    Private _host As IHost
    Private _loginView As LoginView = Nothing
    Private _mainWindow As MainWindow = Nothing
    Private _idleMonitor As IIdleMonitor
    Private _warningView As SessionTimeoutWarningView
    Private _warningTimer As DispatcherTimer

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureAppConfiguration(Sub(ctx, cfg)
                                              cfg.AddProductionOverlay()
                                          End Sub)

        builder.ConfigureServices(Sub(context, services)

                                      ' Infrastructure: Session (LoginSessionService replaces DefaultSessionService in all builds)
                                      services.AddSingleton(Of LoginSessionService)()
                                      services.AddSingleton(Of ISessionService)(Function(sp) sp.GetRequiredService(Of LoginSessionService)())

                                      ' Infrastructure: Write context + role guard (DA5)
                                      services.AddSingleton(Of IWriteContextScope, WriteContextScope)()
                                      services.AddScoped(Of RoleGuardInterceptor)()

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
                                          Function(sp) New AuthenticationService(
                                              context.Configuration.GetConnectionString("MerchSysCentral"),
                                              sp.GetRequiredService(Of ISessionService)(),
                                              sp.GetRequiredService(Of IWriteContextScope)()))

                                      ' Infrastructure: Login UI
                                      services.AddTransient(Of LoginViewModel)()
                                      services.AddTransient(Of LoginView)()

                                      ' Infrastructure: EventBus (MediatR adapter — required by POS services)
                                      services.AddScoped(Of IEventBus, MediatREventBus)()

                                      ' Infrastructure: DbContexts
                                      services.AddModuleDbContexts(context.Configuration)

                                      ' Infrastructure: MediatR (all module handler assemblies)
                                      services.AddMediatRServices()



                                      ' Infrastructure: Idle monitor (DA2)
                                      services.AddSingleton(Of IdleMonitorOptions)(
                                          Function(sp)
                                              Dim cfg = sp.GetRequiredService(Of IConfiguration)()
                                              Return New IdleMonitorOptions With {
                                                  .IdleTimeoutMinutes = cfg.GetValue(Of Integer)("Session:IdleTimeoutMinutes", 20),
                                                  .WarningLeadSeconds = cfg.GetValue(Of Integer)("Session:WarningLeadSeconds", 60)
                                              }
                                          End Function)
#If DEBUG Then
                                      If Environment.GetEnvironmentVariable("VISTA_DISABLE_IDLE_TIMEOUT") = "1" Then
                                          services.AddSingleton(Of IIdleMonitor, NoOpIdleMonitor)()
                                      Else
                                          services.AddSingleton(Of IIdleMonitor, WpfIdleMonitor)()
                                      End If
#Else
                                      services.AddSingleton(Of IIdleMonitor, WpfIdleMonitor)()
#End If
                                      services.AddTransient(Of SessionTimeoutWarningViewModel)()
                                      services.AddTransient(Of SessionTimeoutWarningView)()

                                      ' Infrastructure: Notification toasts (Notification.Wpf)
                                      services.AddSingleton(Of MerchSys.SharedKernel.Interfaces.INotificationService, Services.DefaultNotificationService)()

                                      ' Infrastructure: Concurrency conflict presenter
                                      services.AddSingleton(Of MerchSys.SharedKernel.Interfaces.IConflictPresenter, Services.DefaultConflictPresenter)()
                                      services.AddSingleton(Of MerchSys.SharedKernel.Interfaces.IConfirmationPresenter, Services.DefaultConfirmationPresenter)()

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
                                      services.AddTransient(Of ProductPriceHistoryViewModel)()

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

                                      ' QuestPDF Community License declaration for Accounting exports.
                                      ' Setting it here is idempotent with PosServiceRegistration; both modules ship the same
                                      ' Community License declaration so neither depends on the other being loaded first.
                                      QuestPDF.Settings.License = LicenseType.Community

                                      services.AddOptions(Of TamperReportExportOptions)().BindConfiguration("Accounting:TamperReport:Export")
                                      services.AddScoped(Of ITamperReportExporter, TamperReportExporter)()
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
                                      services.AddTransient(Of Views.Purchasing.PurchasingDashboardView)()
                                      services.AddTransient(Of Views.Purchasing.PurchaseOrderListView)()
                                      services.AddTransient(Of Views.Purchasing.GoodsReceivingView)()
                                      services.AddTransient(Of Views.Purchasing.VendorDirectoryView)()
                                      services.AddTransient(Of Views.Purchasing.VendorCatalogView)()
                                      services.AddTransient(Of Views.Purchasing.APLedgerView)()
                                      services.AddTransient(Of Views.Purchasing.ReorderSuggestionsView)()
                                      services.AddTransient(Of Views.Inventory.StockDashboardView)()
                                      services.AddTransient(Of Views.Inventory.ProductManagementView)()
                                      services.AddTransient(Of Views.Inventory.ExpiryMonitorView)()
                                      services.AddTransient(Of Views.Inventory.ShrinkageView)()
                                      services.AddTransient(Of Views.Inventory.ProductPriceHistoryView)()
                                      services.AddTransient(Of Views.Accounting.FinancialOverviewView)()
                                      services.AddTransient(Of Views.Accounting.IncomeStatementView)()
                                      services.AddTransient(Of Views.Accounting.SalesSummaryView)()
                                      services.AddTransient(Of Views.Accounting.VatReturnView)()
                                      services.AddTransient(Of Views.Accounting.TamperAuditReportView)()
                                      services.AddTransient(Of Views.Accounting.VatReliefReportView)()
                                      services.AddTransient(Of Views.Accounting.Components.VatPayableTile)()

                                      ' ── Connection health monitor (INFRA-28) ──────────────
                                      services.AddConnectionHealthMonitor()

                                      ' ── Shell ─────────────────────────────────────────────
                                      services.AddSingleton(Of MainWindowViewModel)()
                                      services.AddSingleton(Of ViewModels.Shell.CommandPaletteViewModel)()
                                      services.AddSingleton(Of Views.Shell.CommandPalette)()
                                      ' INFRA-30: Activity Rail + Module Detail Panel (Singleton — created once with MainWindow)
                                      services.AddSingleton(Of ViewModels.Shell.ActivityRailViewModel)()
                                      services.AddSingleton(Of Views.Shell.ActivityRail)()
                                      services.AddSingleton(Of Views.Shell.ModuleDetailPanel)()
                                      services.AddSingleton(Of MainWindow)()

                                       ' ── UI Settings store (UX-23) — shared by ThemeService and WindowPlacementService ──
                                       services.AddSingleton(Of UiSettingsStore)()
                                       services.AddSingleton(Of WindowPlacementService)()

                                       ' ── Theming Foundation (UX-01) ────────────────────────
                                       services.AddSingleton(Of IThemeService, ThemeService)()

                                       ' ── Developer Tools (Developer role only; all build configurations) ────────
                                       services.AddDebugServices()

                                  End Sub)

        _host = builder.Build()
        _host.Start()

        ' Developer Tools harnesses resolve production services through this holder.
        DebugHostHolder.CurrentHost = _host

        Dim config = _host.Services.GetRequiredService(Of IConfiguration)()
        Dim connStr = config.GetConnectionString("MerchSysCentral")
        Dim loggerFactory = _host.Services.GetRequiredService(Of ILoggerFactory)()
        Dim startupLogger = loggerFactory.CreateLogger("Startup")

        If config.GetValue(Of Boolean)("Schema:RunBootstrap", True) Then
            Try
                MariaDbSchemaInitializer.Initialize(connStr, startupLogger)
            Catch ex As Exception
                MessageBox.Show($"Cannot initialize database schema: {ex.Message}", "VISTA — Fatal", MessageBoxButton.OK, MessageBoxImage.Error)
                Shutdown(1)
                Return
            End Try
        End If

        ' Start connection health monitor — must run before MainWindow is shown
        Dim connMonitor = _host.Services.GetRequiredService(Of IConnectionHealthMonitor)()
        ConnectionHealthMonitorLocator.Current = connMonitor
        connMonitor.Start()

        _host.Services.GetRequiredService(Of ILowStockNotifier)()

        ' Wire MainWindowViewModel.LogoutRequested once (singleton)
        Dim mainVm = _host.Services.GetRequiredService(Of MainWindowViewModel)()
        AddHandler mainVm.LogoutRequested, AddressOf HandleLogoutRequested

        ' Wire idle monitor — monitoring starts only after successful login (DA2)
        _idleMonitor = _host.Services.GetRequiredService(Of IIdleMonitor)()
        AddHandler _idleMonitor.WarningShown, AddressOf HandleIdleWarning
        AddHandler _idleMonitor.SessionExpired, AddressOf HandleSessionExpired

        ' Load and apply persisted theme before showing UI (UX-01)
        Dim themeService = _host.Services.GetRequiredService(Of IThemeService)()
        Dim savedTheme = themeService.LoadPersisted()
        themeService.Apply(savedTheme)

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
        ' Idle monitoring begins here; no clock runs while LoginView is active (DA2)
        _idleMonitor.Start()
    End Sub

    Private Sub HandleLogoutRequested(sender As Object, e As EventArgs)
        _idleMonitor.Stop()
        CloseWarningDialogIfOpen()
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
        ' Stop monitors before disposing the host so no background callbacks fire
        ' against a disposed DI container.
        If _idleMonitor IsNot Nothing Then _idleMonitor.Stop()
        Dim mon = ConnectionHealthMonitorLocator.Current
        If mon IsNot Nothing Then mon.[Stop]()
        _host?.StopAsync().GetAwaiter().GetResult()
        _host?.Dispose()
    End Sub

    ' ── Idle timeout (DA2) ────────────────────────────────────────────────────

    Private Sub HandleIdleWarning(sender As Object, e As IdleMonitorWarningEventArgs)
        ' Edge case: if MainWindow is not visible the user already logged out
        If _mainWindow Is Nothing OrElse Not _mainWindow.IsVisible Then
            HandleSessionExpired(Me, EventArgs.Empty)
            Return
        End If

        CloseWarningDialogIfOpen()

        _warningView = _host.Services.GetRequiredService(Of SessionTimeoutWarningView)()
        _warningView.Owner = _mainWindow
        _warningView.ViewModel.Tick(e.RemainingSeconds)

        AddHandler _warningView.ViewModel.StayRequested, AddressOf HandleStayRequested
        AddHandler _warningView.ViewModel.SignOutRequested, AddressOf HandleWarningSignOut

        _warningTimer = New DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(1)}
        AddHandler _warningTimer.Tick, AddressOf OnWarningTimerTick
        _warningTimer.Start()

        _warningView.Show()
    End Sub

    Private Sub OnWarningTimerTick(sender As Object, e As EventArgs)
        If _warningView IsNot Nothing Then
            _warningView.ViewModel.Tick(_idleMonitor.RemainingSeconds)
        End If
    End Sub

    Private Sub HandleStayRequested(sender As Object, e As EventArgs)
        _idleMonitor.RecordActivity()
        CloseWarningDialogIfOpen()
    End Sub

    Private Sub HandleWarningSignOut(sender As Object, e As EventArgs)
        CloseWarningDialogIfOpen()
        Dim mainVm = _host.Services.GetRequiredService(Of MainWindowViewModel)()
        mainVm.LogoutCommand.Execute(Nothing)
    End Sub

    Private Sub HandleSessionExpired(sender As Object, e As EventArgs)
        CloseWarningDialogIfOpen()
        Dim session = _host.Services.GetRequiredService(Of LoginSessionService)()
        session.ClearUser()
        Dim mainVm = _host.Services.GetRequiredService(Of MainWindowViewModel)()
        mainVm.LogoutCommand.Execute(Nothing)
    End Sub

    Private Sub CloseWarningDialogIfOpen()
        If _warningTimer IsNot Nothing Then
            _warningTimer.Stop()
            RemoveHandler _warningTimer.Tick, AddressOf OnWarningTimerTick
            _warningTimer = Nothing
        End If
        If _warningView IsNot Nothing Then
            Dim view = _warningView
            _warningView = Nothing
            RemoveHandler view.ViewModel.StayRequested, AddressOf HandleStayRequested
            RemoveHandler view.ViewModel.SignOutRequested, AddressOf HandleWarningSignOut
            view.MarkDecisionMade()
            view.Close()
        End If
    End Sub

End Class
