Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class InventoryDbContextFactory
        Implements IDesignTimeDbContextFactory(Of InventoryDbContext)

        Public Function CreateDbContext(args As String()) As InventoryDbContext _
            Implements IDesignTimeDbContextFactory(Of InventoryDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of InventoryDbContext)()
            optionsBuilder.UseMySQL("Server=localhost;Database=design_time;User Id=root;Password=;")
            Return New InventoryDbContext(optionsBuilder.Options, Nothing)
        End Function

    End Class

End Namespace
