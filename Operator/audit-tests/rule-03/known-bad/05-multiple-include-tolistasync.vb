' Rule 3 known-bad: Multiple .Include() chains ending in .ToListAsync()
' Source: VelocityService.vb:29 (verified true positive in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetProductsWithAllNavigationsAsync() As Task(Of List(Of Product))
            Dim products As List(Of Product) = Await _db.Products _
                .Where(Function(p) Not p.IsDeleted AndAlso p.IsActive) _
                .Include(Function(p) p.Category) _
                .Include(Function(p) p.StockBatches) _
                .Include(Function(p) p.ShrinkageRecords) _
                .OrderBy(Function(p) p.Name) _
                .ToListAsync()
            Return products
        End Function
    End Class
End Namespace
