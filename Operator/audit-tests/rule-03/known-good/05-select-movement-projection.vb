' Rule 3 known-good: GroupBy then Select anonymous New With before ToListAsync
' Chain projects via New With — excluded by rule.
' Source: VelocityService.vb:36 (verified false positive in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetMovementCountsAsync() As Task(Of List(Of Object))
            Dim result = Await _db.StockMovements _
                .Where(Function(m) m.OccurredAt >= DateTime.UtcNow.AddDays(-30)) _
                .GroupBy(Function(m) m.ProductId) _
                .Select(Function(g) New With {.ProductId = g.Key, .Total = g.Sum(Function(m) m.Quantity)}) _
                .ToListAsync()
            Return result.Cast(Of Object)().ToList()
        End Function
    End Class
End Namespace
