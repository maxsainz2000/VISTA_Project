Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging
Imports MerchSys.POS.Services
Imports MerchSys.POS.Services.Archival
Imports MerchSys.POS.Services.ReceiptRendering
Imports MerchSys.POS.ViewModels
Imports QuestPDF.Infrastructure

Namespace Startup

    Public Module PosServiceRegistration

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddPosModule(services As IServiceCollection)
            ' QuestPDF (POS-19 PdfReceiptRenderer) requires a license declaration before first
            ' use. Villon Farm Supply qualifies for the Community License (free for organisations
            ' with annual revenue under USD 1M). Set once at module registration so any code path
            ' that resolves IReceiptRenderer can construct a PDF document.
            QuestPDF.Settings.License = LicenseType.Community

            services.AddScoped(Of ICartService, CartService)()
            services.AddScoped(Of IPaymentService, PaymentService)()
            services.AddScoped(Of ICreditService, CreditService)()
            services.AddScoped(Of ISalesReturnService, SalesReturnService)()
            services.AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)()
            services.AddScoped(Of IReceiptBodyComposer, BirCompliantReceiptBodyComposer)()
            services.AddScoped(Of ReceiptService)()
            services.AddScoped(Of IReceiptService, VatAwareReceiptService)()
            services.AddScoped(Of IVatCalculator, VatCalculator)()
            services.AddSingleton(Of VatConfigurationLoader)()
            services.AddScoped(Of IDailySummaryService, DailySummaryService)()
            services.AddScoped(Of IVatConfigurationWriter, VatConfigurationWriter)()

            ' Receipt rendering configurations and services (POS-19)
            services.AddOptions(Of ReceiptPdfOptions)().BindConfiguration("POS:Receipt:Pdf")
            services.AddScoped(Of ConsoleReceiptRenderer)()
            services.AddScoped(Of PdfReceiptRenderer)()
            services.AddScoped(Of IReceiptRenderer)(
                Function(sp)
                    Dim configuration = sp.GetRequiredService(Of IConfiguration)()
                    Dim target = configuration.GetValue(Of String)("POS:Receipt:Renderer", "Console")
                    Dim parsed As ReceiptRenderTarget
                    If Not [Enum].TryParse(target, True, parsed) Then
                        parsed = ReceiptRenderTarget.Console
                        Dim logger = sp.GetRequiredService(Of ILogger(Of ReceiptService))()
                        logger.LogWarning("Unknown POS:Receipt:Renderer '{0}' — falling back to Console.", target)
                    End If

                    Select Case parsed
                        Case ReceiptRenderTarget.Pdf
                            Return sp.GetRequiredService(Of PdfReceiptRenderer)()
                        Case Else
                            Return sp.GetRequiredService(Of ConsoleReceiptRenderer)()
                    End Select
                End Function)

            ' Receipt archival background service (POS-16)
            services.AddOptions(Of ReceiptArchivalOptions)().BindConfiguration("Receipts:Archival")
            services.AddHostedService(Of ReceiptArchivalService)()
            services.AddScoped(Of IReceiptArchivalService, ReceiptArchivalService)()

            services.AddTransient(Of SalesCartViewModel)()
            services.AddTransient(Of CreditManagementViewModel)()
            services.AddTransient(Of TransactionHistoryViewModel)()
            services.AddTransient(Of DailySummaryViewModel)()
            services.AddTransient(Of VatSettingsViewModel)()
        End Sub

    End Module

End Namespace
