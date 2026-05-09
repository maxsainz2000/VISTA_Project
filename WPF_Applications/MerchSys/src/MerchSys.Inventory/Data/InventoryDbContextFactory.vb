Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class InventoryDbContextFactory
        Implements IDesignTimeDbContextFactory(Of InventoryDbContext)

        Public Function CreateDbContext(args As String()) As InventoryDbContext _
            Implements IDesignTimeDbContextFactory(Of InventoryDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of InventoryDbContext)()
            optionsBuilder.UseSqlite("Data Source=design_time.db")
            Return New InventoryDbContext(optionsBuilder.Options)
        End Function

    End Class

End Namespace
