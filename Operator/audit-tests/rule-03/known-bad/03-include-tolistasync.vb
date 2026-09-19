' Rule 3 known-bad: .Include().ToListAsync() — full entity with navigation properties
' Source: StockService.vb:120 (verified true positive — highest-risk pattern in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetProductsWithBatchesAsync() As Task(Of List(Of Product))
            Dim products As List(Of Product) = Await _db.Products _
                .Where(Function(p) Not p.IsDeleted AndAlso p.IsActive) _
                .Include(Function(p) p.StockBatches) _
                .ToListAsync()
            Return products
        End Function
    End Class
End Namespace
