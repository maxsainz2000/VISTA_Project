Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Accounting.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.Purchasing.Data
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Persistence

Namespace Startup

    Public Module SyncableRepositoryRegistration

        <System.Runtime.CompilerServices.Extension>
        Public Function AddSyncableRepositories(services As IServiceCollection) As IServiceCollection
            services.AddScoped(Of ISyncableRepository(Of PurchasingDbContext), PurchasingSyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of InventoryDbContext), InventorySyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of POSDbContext), PosSyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of AccountingDbContext), AccountingSyncableRepository)()
            Return services
        End Function

    End Module

End Namespace
