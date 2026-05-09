Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data

    Public Class AccountingDbContextFactory
        Implements IDesignTimeDbContextFactory(Of AccountingDbContext)

        Public Function CreateDbContext(args As String()) As AccountingDbContext _
            Implements IDesignTimeDbContextFactory(Of AccountingDbContext).CreateDbContext

            Dim optionsBuilder = New DbContextOptionsBuilder(Of AccountingDbContext)()
            optionsBuilder.UseSqlite("Data Source=design_time.db")
            Return New AccountingDbContext(optionsBuilder.Options)
        End Function

    End Class

End Namespace
