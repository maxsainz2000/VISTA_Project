Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities

Namespace Services

    ''' <summary>
    ''' Thrown when a FIFO deduction is requested but insufficient non-expired stock is available.
    ''' </summary>
    Public Class InsufficientStockException
        Inherits Exception

        Public Property ProductId As Integer
        Public Property RequestedQuantity As Integer
        Public Property AvailableQuantity As Integer

        Public Sub New(productId As Integer, requestedQty As Integer, availableQty As Integer)
            MyBase.New($"Insufficient stock for product {productId}. Requested: {requestedQty}, Available: {availableQty}.")
            Me.ProductId = productId
            Me.RequestedQuantity = requestedQty
            Me.AvailableQuantity = availableQty
        End Sub

    End Class

    Public Class StockService
        Implements IStockService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of StockService)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of StockService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function AddStockBatchAsync(productId As Integer, qty As Integer, unitCost As Decimal, receiptDate As DateTime, expiryDate As DateTime?, sourcePOId As Integer?, Optional movementType As MovementType = MovementType.Receipt) As Task(Of StockBatch) Implements IStockService.AddStockBatchAsync
            Dim batch As New StockBatch With {
                .ProductId = productId,
                .QuantityReceived = qty,
                .QuantityRemaining = qty,
                .UnitCost = unitCost,
                .ReceiptDate = receiptDate,
                .ExpiryDate = expiryDate,
                .SourcePurchaseOrderId = sourcePOId
            }
            _db.StockBatches.Add(batch)
            _db.StockMovements.Add(New StockMovement With {
                .ProductId = productId,
                .MovementType = movementType,
                .Quantity = qty,
                .OccurredAt = DateTime.UtcNow
            })
            Await _db.SaveChangesAsync()
            _logger.LogInformation("Stock batch added: ProductId={ProductId}, Qty={Qty}, UnitCost={UnitCost}", productId, qty, unitCost)
            Return batch
        End Function

        ''' <summary>
        ''' Deducts stock using FIFO: consumes oldest non-expired batches first.
        ''' Throws <see cref="InsufficientStockException"/> if stock is insufficient.
        ''' </summary>
        Public Async Function DeductStockFIFOAsync(productId As Integer, quantity As Integer) As Task(Of List(Of FIFODeductionResult)) Implements IStockService.DeductStockFIFOAsync
            Dim now As DateTime = DateTime.UtcNow
            Dim batches As List(Of StockBatch) = Await _db.StockBatches _
                .Where(Function(b) b.ProductId = productId AndAlso
                                   b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                .OrderBy(Function(b) b.ReceiptDate) _
                .ToListAsync()

            Dim results As New List(Of FIFODeductionResult)()
            Dim remaining As Integer = quantity

            For Each batch In batches
                If remaining = 0 Then Exit For
                Dim deduct As Integer = Math.Min(batch.QuantityRemaining, remaining)
                batch.QuantityRemaining -= deduct
                remaining -= deduct
                results.Add(New FIFODeductionResult With {
                    .BatchId = batch.Id,
                    .QuantityDeducted = deduct,
                    .UnitCost = batch.UnitCost,
                    .COGS = CDec(deduct) * batch.UnitCost
                })
            Next

            If remaining > 0 Then
                Throw New InsufficientStockException(productId, quantity, quantity - remaining)
            End If

            _db.StockMovements.Add(New StockMovement With {
                .ProductId = productId,
                .MovementType = MovementType.Sale,
                .Quantity = -quantity,
                .OccurredAt = DateTime.UtcNow
            })
            Await _db.SaveChangesAsync()
            Return results
        End Function

        Public Async Function GetCurrentStockAsync(productId As Integer?) As Task(Of List(Of StockLevelDto)) Implements IStockService.GetCurrentStockAsync
            Dim now As DateTime = DateTime.UtcNow
            Dim query = _db.Products _
                .Where(Function(p) Not p.IsDeleted AndAlso p.IsActive) _
                .Include(Function(p) p.StockBatches) _
                .AsQueryable()

            If productId.HasValue Then
                query = query.Where(Function(p) p.Id = productId.Value)
            End If

            Dim products As List(Of Product) = Await query.ToListAsync()

            Return products.Select(Function(p)
                Dim currentQty As Integer = p.StockBatches _
                    .Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                       (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                    .Sum(Function(b) b.QuantityRemaining)
                Return New StockLevelDto With {
                    .ProductId = p.Id,
                    .ProductName = p.Name,
                    .CurrentQuantity = currentQty,
                    .MinimumThreshold = p.MinimumThreshold,
                    .IsBelowThreshold = currentQty <= p.MinimumThreshold
                }
            End Function).ToList()
        End Function

        Public Async Function GetStockBatchesAsync(productId As Integer) As Task(Of List(Of StockBatch)) Implements IStockService.GetStockBatchesAsync
            Return Await _db.StockBatches _
                .Where(Function(b) b.ProductId = productId) _
                .OrderBy(Function(b) b.ReceiptDate) _
                .ToListAsync()
        End Function

        Public Async Function GetTotalValuationAsync() As Task(Of Decimal) Implements IStockService.GetTotalValuationAsync
            Dim now As DateTime = DateTime.UtcNow
            Return Await _db.StockBatches _
                .Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                .SumAsync(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)
        End Function

    End Class

End Namespace
