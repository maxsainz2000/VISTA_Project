Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Accounting.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.POS.Data
Imports MerchSys.Purchasing.Data
Imports MerchSys.SharedKernel.Data

Namespace Data

    ''' <summary>
    ''' Provides the SQLite connection string and DI registration for all module DbContexts.
    ''' <para>
    ''' The shared database file is stored at <c>%LOCALAPPDATA%\MerchSys\merchsys.db</c>.
    ''' This location survives application updates and is writable without elevation on all
    ''' supported Windows versions. The directory is created on first access if absent.
    ''' </para>
    ''' <para>
    ''' All four module DbContexts share the single SQLite file. Table-name prefixes
    ''' (<c>Pur_</c>, <c>Inv_</c>, <c>Pos_</c>, <c>Acc_</c>) prevent collisions.
    ''' </para>
    ''' </summary>
    Public Module DatabaseConfig

        ''' <summary>Full path to the shared SQLite database file.</summary>
        Public ReadOnly Property DatabasePath As String
            Get
                Dim dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MerchSys")
                Directory.CreateDirectory(dataDir)
                Return Path.Combine(dataDir, "merchsys.db")
            End Get
        End Property

        ''' <summary>
        ''' Registers all four module DbContexts with the SQLite provider, each pointed at
        ''' the shared <c>%LOCALAPPDATA%\MerchSys\merchsys.db</c> file.
        ''' </summary>
        <Extension>
        Public Sub AddModuleDbContexts(services As IServiceCollection)
            Dim connectionString = $"Data Source={DatabasePath}"

            services.AddDbContext(Of PurchasingDbContext)(
                Sub(sp, options)
                    options.UseSqlite(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of InventoryDbContext)(
                Sub(sp, options)
                    options.UseSqlite(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of POSDbContext)(
                Sub(sp, options)
                    options.UseSqlite(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of AccountingDbContext)(
                Sub(sp, options)
                    options.UseSqlite(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)
        End Sub

    End Module

End Namespace
