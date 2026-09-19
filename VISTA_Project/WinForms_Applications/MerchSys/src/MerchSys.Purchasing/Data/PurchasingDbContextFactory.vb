Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class PurchasingDbContextFactory
        Implements IDesignTimeDbContextFactory(Of PurchasingDbContext)

        Public Function CreateDbContext(args As String()) As PurchasingDbContext _
            Implements IDesignTimeDbContextFactory(Of PurchasingDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of PurchasingDbContext)()
            optionsBuilder.UseMySQL("Server=127.0.0.1;Database=design_time;User Id=root;Password=;")
            Return New PurchasingDbContext(optionsBuilder.Options, Nothing)
        End Function

    End Class

End Namespace
