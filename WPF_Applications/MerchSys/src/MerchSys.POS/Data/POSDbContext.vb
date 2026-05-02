Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the POS module.
    ''' All tables in this context use the <c>Pos_</c> prefix to prevent naming collisions
    ''' with other modules in the shared <c>merchsys.db</c> SQLite file.
    ''' DbSet properties are added by subsequent POS data-access plans.
    ''' </summary>
    Public Class POSDbContext
        Inherits BaseDbContext

        Public Sub New(options As DbContextOptions(Of POSDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(POSDbContext).Assembly)
        End Sub

    End Class

End Namespace
