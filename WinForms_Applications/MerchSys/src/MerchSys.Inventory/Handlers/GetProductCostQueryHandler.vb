Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetProductCostQuery"/> sent by the Accounting module.
    ''' Returns the current FIFO unit cost — the UnitCost of the oldest available non-expired stock batch.
    ''' </summary>
    Public Class GetProductCostQueryHandler
        Implements IRequestHandler(Of GetProductCostQuery, GetProductCostResult)

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of GetProductCostQueryHandler)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of GetProductCostQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetProductCostQuery, cancellationToken As CancellationToken) As Task(Of GetProductCostResult) Implements IRequestHandler(Of GetProductCostQuery, GetProductCostResult).Handle
            Dim now As DateTime = DateTime.UtcNow

            Dim oldestBatch = Await _db.StockBatches _
                .Where(Function(b) b.ProductId = request.ProductId AndAlso
                                   b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                .OrderBy(Function(b) b.ReceiptDate) _
                .FirstOrDefaultAsync(cancellationToken)

            Dim fifoUnitCost As Decimal = If(oldestBatch IsNot Nothing, oldestBatch.UnitCost, 0D)

            _logger.LogDebug("GetProductCostQuery for ProductId={ProductId}: FifoUnitCost={Cost}.",
                request.ProductId, fifoUnitCost)

            Return New GetProductCostResult() With {
                .ProductId = request.ProductId,
                .FifoUnitCost = fifoUnitCost
            }
        End Function

    End Class

End Namespace
