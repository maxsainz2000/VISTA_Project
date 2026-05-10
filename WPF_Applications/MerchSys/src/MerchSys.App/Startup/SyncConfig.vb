Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.App.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Startup

    Public Module SyncConfig

        <System.Runtime.CompilerServices.Extension>
        Public Sub AddSyncServices(services As IServiceCollection, sqliteConnectionString As String)
            services.AddOptions(Of SyncSettings)().BindConfiguration("Sync")

            services.AddDbContext(Of SyncJournalDbContext)(
                Sub(options) options.UseSqlite(sqliteConnectionString))

            ' MariaDB context — connection string read from IConfiguration at resolve time
            ' so it is not required at startup (probe must succeed before the context is used).
            services.AddDbContext(Of MariaDbSyncContext)(
                Sub(sp, options)
                    Dim cfg = sp.GetRequiredService(Of IConfiguration)()
                    Dim conn = cfg.GetSection("Sync")("MariaDbConnection")
                    If Not String.IsNullOrWhiteSpace(conn) Then
                        options.UseMySql(conn, ServerVersion.Parse("11.4.0-mariadb"))
                    End If
                End Sub)

            services.AddSingleton(Of ISyncProbe, DualConditionSyncProbe)()
            services.AddSingleton(Of INotificationService, DefaultNotificationService)()
            services.AddScoped(Of IConflictResolver, ConflictResolver)()
            services.AddScoped(Of SyncOrchestrator)()
            services.AddHostedService(Of SyncWorker)()
        End Sub

    End Module

End Namespace
