Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class POSDbContextFactory
        Implements IDesignTimeDbContextFactory(Of POSDbContext)

        Public Function CreateDbContext(args As String()) As POSDbContext _
            Implements IDesignTimeDbContextFactory(Of POSDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of POSDbContext)()
            optionsBuilder.UseMySQL("Server=localhost;Database=design_time;User Id=root;Password=;")
            Return New POSDbContext(optionsBuilder.Options, Nothing)
        End Function

    End Class

End Namespace
