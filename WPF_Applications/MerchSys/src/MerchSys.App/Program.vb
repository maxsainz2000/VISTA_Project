Imports System.Windows.Forms
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.DependencyInjection

Module Program
    <STAThread>
    Sub Main()
        System.Windows.Forms.Application.EnableVisualStyles()
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)

        Dim builder = Host.CreateDefaultBuilder()

        builder.ConfigureServices(Sub(context, services)
                                      services.AddSingleton(Of MainWindow)()
                                      ' Placeholder for DI setup (TODO INFRA-02)
                                  End Sub)

        Dim hostApp = builder.Build()

        Using scope = hostApp.Services.CreateScope()
            Dim services = scope.ServiceProvider
            Dim mainWindow = services.GetRequiredService(Of MainWindow)()
            System.Windows.Forms.Application.Run(mainWindow)
        End Using
    End Sub
End Module
