' Rule 3 known-good: Select anonymous projection then OrderByDescending then ToListAsync
' Even though OrderByDescending is the last step before ToListAsync, the chain
' contains a .Select(Function(g) New With {...}) that projects away from full entity.
' Source: IncomeStatementService.vb:73 (verified false positive in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetProductMarginsAsync() As Task(Of List(Of Object))
            Dim result = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= DateTime.UtcNow.AddMonths(-3)) _
                .GroupBy(Function(r) New With {Key .ProductId = r.ProductId, Key .ProductName = r.ProductName}) _
                .Select(Function(g) New With {
                    Key .ProductId = g.Key.ProductId,
                    Key .ProductName = g.Key.ProductName,
                    Key .Revenue = g.Sum(Function(r) r.NetAmount)
                }) _
                .OrderByDescending(Function(g) g.Revenue) _
                .ToListAsync()
            Return result.Cast(Of Object)().ToList()
        End Function
    End Class
End Namespace
