Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.DependencyInjection
Imports System.Windows.Forms

Friend Module Program

    <STAThread()>
    Friend Sub Main(args As String())
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        Dim builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
        builder.ConfigureServices(Sub(context, services)
                                   ' TODO INFRA-02: Register ViewModels and Services here
                                   services.AddTransient(Of Form1)()
                               End Sub)

        Dim host As IHost = builder.Build()

        Dim mainForm = host.Services.GetRequiredService(Of Form1)()
        Application.Run(mainForm)
    End Sub

End Module
