Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports MerchSys.App.Configuration
Imports MerchSys.App.Services
Imports MerchSys.App.Services.Sync
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Startup

    Public Module SyncConfig

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddSyncServices(services As IServiceCollection, sqliteConnectionString As String)
            services.AddOptions(Of SyncSettings)().BindConfiguration("Sync")

            services.AddDbContext(Of SyncJournalDbContext)(
                Sub(options) options.UseSqlite(sqliteConnectionString))

            ' MariaDB context — connection string assembled from the production overlay at resolve time.
            ' If no valid overlay is present the context is registered but UseMySql is never called;
            ' the SyncWorker's TCP probe will gate any actual connection attempt.
            services.AddDbContext(Of MariaDbSyncContext)(
                Sub(sp, options)
                    Dim cfg = sp.GetRequiredService(Of IConfiguration)()
                    Dim logger = sp.GetRequiredService(Of ILogger(Of SyncWorker))()
                    Dim conn = ConnectionStringLoader.GetMariaDbConnectionString(cfg, logger)
                    If Not String.IsNullOrWhiteSpace(conn) Then
                        options.UseMySql(conn, ServerVersion.Parse("11.4.0-mariadb"))
                    End If
                End Sub)

            services.AddSingleton(Of ISyncProbe, DualConditionSyncProbe)()
            services.AddSingleton(Of INotificationService, DefaultNotificationService)()
            services.AddScoped(Of IConflictResolver, ConflictResolver)()
            services.AddScoped(Of ISyncTransmitter, MariaDbSyncTransmitter)()
            services.AddScoped(Of SyncOrchestrator)()
            services.AddHostedService(Of SyncWorker)()
        End Sub

    End Module

End Namespace
