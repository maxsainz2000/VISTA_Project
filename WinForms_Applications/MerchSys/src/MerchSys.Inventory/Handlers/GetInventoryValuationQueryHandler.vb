Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetInventoryValuationQuery"/> sent by the Accounting module.
    ''' Computes the real-time FIFO total inventory valuation and itemized breakdown.
    ''' </summary>
    Public Class GetInventoryValuationQueryHandler
        Implements IRequestHandler(Of GetInventoryValuationQuery, GetInventoryValuationResult)

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of GetInventoryValuationQueryHandler)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of GetInventoryValuationQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetInventoryValuationQuery, cancellationToken As CancellationToken) As Task(Of GetInventoryValuationResult) Implements IRequestHandler(Of GetInventoryValuationQuery, GetInventoryValuationResult).Handle
            Dim now As DateTime = DateTime.UtcNow
            _logger.LogInformation("Calculating FIFO inventory valuation...")

            ' Fetch all stock batches that have remaining quantities and are not expired
            Dim activeBatches = Await _db.StockBatches _
                .Include(Function(b) b.Product) _
                .Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                .ToListAsync(cancellationToken)

            Dim result As New GetInventoryValuationResult()

            ' Group by ProductId
            Dim groups = activeBatches.GroupBy(Function(b) b.ProductId)

            For Each g In groups
                Dim firstBatch = g.First()
                Dim totalQty = g.Sum(Function(b) b.QuantityRemaining)
                Dim totalVal = g.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)

                result.Items.Add(New GetInventoryValuationResult.ProductValuation With {
                    .ProductId = g.Key,
                    .ProductName = If(firstBatch.Product IsNot Nothing, firstBatch.Product.Name, $"Product #{g.Key}"),
                    .TotalQuantity = totalQty,
                    .TotalValue = totalVal
                })
            Next

            result.TotalValue = result.Items.Sum(Function(i) i.TotalValue)
            _logger.LogInformation("FIFO Inventory Valuation calculated successfully: TotalValue={TotalValue}", result.TotalValue)
            Return result
        End Function

    End Class

End Namespace
