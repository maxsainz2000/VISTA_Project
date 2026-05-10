Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Startup

    Public Module SyncConfig

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddSyncServices(services As IServiceCollection, connectionString As String)
            services.AddOptions(Of SyncSettings)().BindConfiguration("Sync")

            services.AddDbContext(Of SyncJournalDbContext)(
                Sub(options) options.UseSqlite(connectionString))

            services.AddSingleton(Of ISyncProbe, DualConditionSyncProbe)()
            services.AddSingleton(Of INotificationService, DefaultNotificationService)()
            services.AddScoped(Of SyncOrchestrator)()
            services.AddHostedService(Of SyncWorker)()
        End Sub

    End Module

End Namespace
