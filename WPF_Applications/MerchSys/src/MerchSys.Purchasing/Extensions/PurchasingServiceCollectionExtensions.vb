Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Purchasing.Services
Imports System.Runtime.CompilerServices

Namespace Extensions

    Public Module PurchasingServiceCollectionExtensions

        <Extension()>
        Public Function AddPurchasingServices(services As IServiceCollection) As IServiceCollection
            services.AddScoped(Of IPurchaseOrderService, PurchaseOrderService)()
            Return services
        End Function

    End Module

End Namespace
