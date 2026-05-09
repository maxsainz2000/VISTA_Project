Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class PurchasingDbContextFactory
        Implements IDesignTimeDbContextFactory(Of PurchasingDbContext)

        Public Function CreateDbContext(args As String()) As PurchasingDbContext _
            Implements IDesignTimeDbContextFactory(Of PurchasingDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of PurchasingDbContext)()
            optionsBuilder.UseSqlite("Data Source=design_time.db")
            Return New PurchasingDbContext(optionsBuilder.Options)
        End Function

    End Class

End Namespace
