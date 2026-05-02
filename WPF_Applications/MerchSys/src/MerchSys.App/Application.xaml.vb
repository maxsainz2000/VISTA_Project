Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting

''' <summary>
''' Application entry point for MerchSys.
'''
''' DI SETUP PLACEHOLDER:
''' Application_Startup builds the IHost using Microsoft.Extensions.Hosting.
''' Each module's services will be registered here in subsequent plans (INFRA-02+).
''' MediatR, EF Core DbContexts, and module-specific services are wired up
''' once the individual module plans are implemented.
''' </summary>
Class Application

    Private _host As IHost

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureServices(Sub(services)
                                      ' TODO (INFRA-02): Register MediatR
                                      ' TODO (INFRA-02): Register module DbContexts
                                      ' TODO (INFRA-02): Register module services and ViewModels
                                  End Sub)

        _host = builder.Build()
        _host.Start()
    End Sub

    Private Sub Application_Exit(sender As Object, e As ExitEventArgs)
        _host?.StopAsync().GetAwaiter().GetResult()
        _host?.Dispose()
    End Sub

End Class
