Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging
Imports MerchSys.App.Services
Imports MerchSys.App.ViewModels.Shell
Imports MerchSys.App.Views.Shell

Namespace Startup

    ''' <summary>
    ''' DI registration for the connection health monitor and its associated UI components.
    ''' Call AddConnectionHealthMonitor from ConfigureServices in Application.xaml.vb.
    ''' </summary>
    Public Module ConnectionConfig

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddConnectionHealthMonitor(services As IServiceCollection)
            services.AddSingleton(Of IConnectionHealthMonitor)(
                Function(sp)
                    Dim cfg = sp.GetRequiredService(Of IConfiguration)()
                    Dim connStr = cfg.GetConnectionString("MerchSysCentral")
                    Dim logger = sp.GetRequiredService(Of ILogger(Of ConnectionHealthMonitor))()
                    Return New ConnectionHealthMonitor(connStr, cfg, logger)
                End Function)

            services.AddTransient(Of ConnectionStatusViewModel)()
            services.AddTransient(Of ConnectionStatusIndicator)()
        End Sub

    End Module

End Namespace
