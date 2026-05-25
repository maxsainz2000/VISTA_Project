' Rule 3 known-bad: .Where().ToListAsync() — full entity materialisation
' Source: StockService.vb:77 (verified true positive in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetAvailableBatchesAsync(productId As Integer) As Task(Of List(Of StockBatch))
            Dim batches As List(Of StockBatch) = Await _db.StockBatches _
                .Where(Function(b) b.ProductId = productId AndAlso b.QuantityRemaining > 0) _
                .OrderBy(Function(b) b.ReceiptDate) _
                .ToListAsync()
            Return batches
        End Function
    End Class
End Namespace
