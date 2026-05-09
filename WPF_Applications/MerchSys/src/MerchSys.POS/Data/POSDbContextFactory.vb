Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class POSDbContextFactory
        Implements IDesignTimeDbContextFactory(Of POSDbContext)

        Public Function CreateDbContext(args As String()) As POSDbContext _
            Implements IDesignTimeDbContextFactory(Of POSDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of POSDbContext)()
            optionsBuilder.UseSqlite("Data Source=design_time.db")
            Return New POSDbContext(optionsBuilder.Options)
        End Function

    End Class

End Namespace
