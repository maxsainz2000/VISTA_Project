Imports System.Runtime.CompilerServices
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.Accounting.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.POS.Data
Imports MerchSys.Purchasing.Data
Imports MerchSys.SharedKernel.Data

Namespace Data

    ''' <summary>
    ''' DI registration for all four module DbContexts against the centralized MariaDB instance.
    ''' Table-name prefixes (<c>Pur_</c>, <c>Inv_</c>, <c>Pos_</c>, <c>Acc_</c>) prevent collisions.
    ''' </summary>
    Public Module DatabaseConfig

        ''' <summary>
        ''' Registers all four module DbContexts with the Oracle MySQL/MariaDB provider,
        ''' each using the centralized connection string from <c>ConnectionStrings:MerchSysCentral</c>.
        ''' </summary>
        <Extension>
        Public Sub AddModuleDbContexts(services As IServiceCollection, configuration As IConfiguration)
            Dim connectionString = configuration.GetConnectionString("MerchSysCentral")

            services.AddDbContext(Of PurchasingDbContext)(
                Sub(sp, options)
                    options.UseMySQL(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of InventoryDbContext)(
                Sub(sp, options)
                    options.UseMySQL(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of POSDbContext)(
                Sub(sp, options)
                    options.UseMySQL(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)

            services.AddDbContext(Of AccountingDbContext)(
                Sub(sp, options)
                    options.UseMySQL(connectionString)
                    options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
                End Sub)
        End Sub

    End Module

End Namespace
