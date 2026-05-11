Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Purchasing.Services
Imports MerchSys.Purchasing.Services.Vat
Imports System.Runtime.CompilerServices

Namespace Extensions

    Public Module PurchasingServiceCollectionExtensions

        <Extension()>
        Public Function AddPurchasingServices(services As IServiceCollection) As IServiceCollection
            services.AddScoped(Of GoodsReceiptVatCalculator)()
            services.AddScoped(Of IPurchaseOrderService, PurchaseOrderService)()
            services.AddScoped(Of IPriceChangeService, PriceChangeService)()
            services.AddScoped(Of IGoodsReceivingService, GoodsReceivingService)()
            services.AddScoped(Of IVendorService, VendorService)()
            services.AddScoped(Of IAccountsPayableService, AccountsPayableService)()
            services.AddScoped(Of IReorderService, ReorderService)()
            services.AddTransient(Of ViewModels.APLedgerViewModel)()
            services.AddTransient(Of ViewModels.ReorderSuggestionsViewModel)()
            Return services
        End Function

    End Module

End Namespace
