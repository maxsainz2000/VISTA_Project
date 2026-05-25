' Rule 3 known-good: Scalar projection (.Select(Function(x) x.Id).ToListAsync())
' Rule wiki explicitly excludes scalar projections — these are not affected by the EF bug.

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetActiveProductIdsAsync() As Task(Of List(Of Integer))
            Return Await _db.Products _
                .Where(Function(p) p.IsActive AndAlso Not p.IsDeleted) _
                .Select(Function(p) p.Id) _
                .ToListAsync()
        End Function
    End Class
End Namespace
