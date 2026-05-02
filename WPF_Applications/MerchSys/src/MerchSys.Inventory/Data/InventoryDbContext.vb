Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the Inventory module.
    ''' All tables in this context use the <c>Inv_</c> prefix to prevent naming collisions
    ''' with other modules in the shared <c>merchsys.db</c> SQLite file.
    ''' DbSet properties are added by subsequent inventory data-access plans.
    ''' </summary>
    Public Class InventoryDbContext
        Inherits BaseDbContext

        Public Sub New(options As DbContextOptions(Of InventoryDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(InventoryDbContext).Assembly)
        End Sub

    End Class

End Namespace
