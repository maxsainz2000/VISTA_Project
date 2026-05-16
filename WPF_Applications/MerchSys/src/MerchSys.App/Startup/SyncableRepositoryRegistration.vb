Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Accounting.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.Purchasing.Data
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Startup

    Public Module SyncableRepositoryRegistration

        <System.Runtime.CompilerServices.Extension>
        Public Function AddSyncableRepositories(services As IServiceCollection) As IServiceCollection
            ' Generic write-path registrations — injected into module services for SaveChangesWithJournalAsync.
            services.AddScoped(Of ISyncableRepository(Of PurchasingDbContext), PurchasingSyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of InventoryDbContext), InventorySyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of POSDbContext), PosSyncableRepository)()
            services.AddScoped(Of ISyncableRepository(Of AccountingDbContext), AccountingSyncableRepository)()

            ' Non-generic consumer registrations — resolved as IEnumerable(Of ISyncableRepository)
            ' by SyncOrchestrator to enumerate pending journal entries across all modules. (INFRA-13)
            services.AddScoped(Of ISyncableRepository, PurchasingSyncableRepository)()
            services.AddScoped(Of ISyncableRepository, InventorySyncableRepository)()
            services.AddScoped(Of ISyncableRepository, PosSyncableRepository)()
            services.AddScoped(Of ISyncableRepository, AccountingSyncableRepository)()

            Return services
        End Function

    End Module

End Namespace
