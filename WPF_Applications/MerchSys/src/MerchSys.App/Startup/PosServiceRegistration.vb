Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.POS.Services
Imports MerchSys.POS.ViewModels

Namespace Startup

    Public Module PosServiceRegistration

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddPosModule(services As IServiceCollection)
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
            services.AddTransient(Of SalesCartViewModel)()
            services.AddTransient(Of CreditManagementViewModel)()
            services.AddTransient(Of TransactionHistoryViewModel)()
            services.AddTransient(Of DailySummaryViewModel)()
            services.AddTransient(Of VatSettingsViewModel)()
        End Sub

    End Module

End Namespace
