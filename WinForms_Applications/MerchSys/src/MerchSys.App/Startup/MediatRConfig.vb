Imports MediatR
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.SharedKernel.Events
Imports MerchSys.Purchasing.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.POS.Data
Imports MerchSys.Accounting.Data

Namespace Startup

    ''' <summary>
    ''' Extension method that registers MediatR and all handler assemblies with the DI container.
    ''' Call this from the application's service-configuration entry point.
    ''' </summary>
    Public Module MediatRConfig

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddMediatRServices(services As IServiceCollection)
            services.AddMediatR(Sub(cfg As MediatRServiceConfiguration)
                                    ' SharedKernel assembly — event/query contracts
                                    cfg.RegisterServicesFromAssembly(GetType(GoodsReceivedEvent).Assembly)
                                    ' Module assemblies — notification/request handlers
                                    cfg.RegisterServicesFromAssembly(GetType(PurchasingDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(GetType(InventoryDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(GetType(POSDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(GetType(AccountingDbContext).Assembly)
                                End Sub)
        End Sub

    End Module

End Namespace
