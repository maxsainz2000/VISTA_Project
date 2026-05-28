Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class AccountingDbContextFactory
        Implements IDesignTimeDbContextFactory(Of AccountingDbContext)

        Public Function CreateDbContext(args As String()) As AccountingDbContext _
            Implements IDesignTimeDbContextFactory(Of AccountingDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of AccountingDbContext)()
            optionsBuilder.UseMySQL("Server=localhost;Database=design_time;User Id=root;Password=;")
            Return New AccountingDbContext(optionsBuilder.Options)
        End Function

    End Class

End Namespace
