Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class ShrinkageService
        Implements IShrinkageService

        Private Shared ReadOnly ValidReasons As String() = {"Damage", "Spoilage", "Expiry", "Admin Error"}

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _logger As ILogger(Of ShrinkageService)
        Private ReadOnly _repository As ISyncableRepository(Of InventoryDbContext)

        Public Sub New(db As InventoryDbContext,
                       mediator As IMediator,
                       logger As ILogger(Of ShrinkageService),
                       repository As ISyncableRepository(Of InventoryDbContext))
            _db = db
            _mediator = mediator
            _logger = logger
            _repository = repository
        End Sub

        ''' <summary>
        ''' Records stock loss against a specific batch or via FIFO, then publishes
        ''' <see cref="ShrinkageRecordedEvent"/> so Accounting can post the write-off expense.
        ''' Returns the first <see cref="ShrinkageRecord"/> created (one per batch consumed).
        ''' </summary>
        Public Async Function RecordShrinkageAsync(productId As Integer, quantity As Integer, reason As String, notes As String, Optional batchId As Integer? = Nothing) As Task(Of ShrinkageRecord) Implements IShrinkageService.RecordShrinkageAsync
            If Not ValidReasons.Contains(reason) Then
                Throw New ArgumentException($"Invalid shrinkage reason '{reason}'. Valid values: {String.Join(", ", ValidReasons)}.")
            End If
            If quantity <= 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(quantity), "Quantity must be greater than zero.")
            End If

            Dim product As Product = Await _db.Products _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)
            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim created As New List(Of ShrinkageRecord)()
            Dim now As DateTime = DateTime.UtcNow

            If batchId.HasValue Then
                Dim batch As StockBatch = Await _db.StockBatches _
                    .FirstOrDefaultAsync(Function(b) b.Id = batchId.Value AndAlso b.ProductId = productId)
                If batch Is Nothing Then
                    Throw New InvalidOperationException($"StockBatch {batchId.Value} not found for product {productId}.")
                End If
                If batch.QuantityRemaining < quantity Then
                    Throw New InsufficientStockException(productId, quantity, batch.QuantityRemaining)
                End If

                batch.QuantityRemaining -= quantity
                Dim record As New ShrinkageRecord With {
                    .ProductId = productId,
                    .StockBatchId = batchId,
                    .QuantityLost = quantity,
                    .UnitCost = batch.UnitCost,
                    .TotalValue = CDec(quantity) * batch.UnitCost,
                    .Reason = reason,
                    .Notes = notes,
                    .RecordedDate = now
                }
                _db.ShrinkageRecords.Add(record)
                created.Add(record)
            Else
                ' FIFO: consume oldest batches first regardless of expiry status
                Dim batches As List(Of StockBatch) = Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = productId AndAlso b.QuantityRemaining > 0) _
                    .OrderBy(Function(b) b.ReceiptDate) _
                    .ToListAsync()

                Dim remaining As Integer = quantity
                For Each batch In batches
                    If remaining = 0 Then Exit For
                    Dim deduct As Integer = Math.Min(batch.QuantityRemaining, remaining)
                    batch.QuantityRemaining -= deduct
                    remaining -= deduct
                    Dim record As New ShrinkageRecord With {
                        .ProductId = productId,
                        .StockBatchId = batch.Id,
                        .QuantityLost = deduct,
                        .UnitCost = batch.UnitCost,
                        .TotalValue = CDec(deduct) * batch.UnitCost,
                        .Reason = reason,
                        .Notes = notes,
                        .RecordedDate = now
                    }
                    _db.ShrinkageRecords.Add(record)
                    created.Add(record)
                Next

                If remaining > 0 Then
                    Throw New InsufficientStockException(productId, quantity, quantity - remaining)
                End If
            End If

            Dim totalQtyForMovement As Integer = created.Sum(Function(r) r.QuantityLost)
            _db.StockMovements.Add(New StockMovement With {
                .ProductId = productId,
                .MovementType = MovementType.Shrinkage,
                .Quantity = -totalQtyForMovement,
                .OccurredAt = now
            })
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Dim totalQty As Integer = created.Sum(Function(r) r.QuantityLost)
            Dim totalValue As Decimal = created.Sum(Function(r) r.TotalValue)
            Dim avgUnitCost As Decimal = If(totalQty > 0, totalValue / CDec(totalQty), 0D)

            Await _mediator.Publish(New ShrinkageRecordedEvent With {
                .ProductId = productId,
                .ProductName = product.Name,
                .QuantityLost = totalQty,
                .UnitCost = avgUnitCost,
                .TotalValue = totalValue,
                .Reason = reason,
                .RecordedDate = now
            })

            _logger.LogInformation(
                "Shrinkage recorded: ProductId={ProductId}, Qty={Qty}, TotalValue={TotalValue}, Reason={Reason}",
                productId, totalQty, totalValue, reason)

            Return created.First()
        End Function

        Public Async Function GetShrinkageHistoryAsync(Optional productId As Integer? = Nothing) As Task(Of List(Of ShrinkageRecord)) Implements IShrinkageService.GetShrinkageHistoryAsync
            Dim query = _db.ShrinkageRecords _
                .Include(Function(r) r.Product) _
                .Include(Function(r) r.StockBatch) _
                .AsQueryable()

            If productId.HasValue Then
                query = query.Where(Function(r) r.ProductId = productId.Value)
            End If

            Return Await query.OrderByDescending(Function(r) r.RecordedDate).ToListAsync()
        End Function

        Public Async Function GetTotalShrinkageValueAsync(startDate As DateTime, endDate As DateTime) As Task(Of Decimal) Implements IShrinkageService.GetTotalShrinkageValueAsync
            Return Await _db.ShrinkageRecords _
                .Where(Function(r) r.RecordedDate >= startDate AndAlso r.RecordedDate <= endDate) _
                .SumAsync(Function(r) r.TotalValue)
        End Function

    End Class

End Namespace
