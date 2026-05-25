Imports System.Threading
Imports MediatR
Imports Microsoft.Data.Sqlite
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
        Private _batchesForShrinkage As List(Of StockBatch)
        Private _shrinkageHistoryList As List(Of ShrinkageRecord)

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
                _batchesForShrinkage = New List(Of StockBatch)()
                Dim shrConnStr = _db.Database.GetConnectionString()
                Using shrConn As New SqliteConnection(shrConnStr)
                    Await shrConn.OpenAsync()
                    Using shrCmd = shrConn.CreateCommand()
                        shrCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                             "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                             "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                             "FROM Inv_StockBatches " &
                                             "WHERE ProductId = @productId AND QuantityRemaining > 0 " &
                                             "ORDER BY ReceiptDate"
                        shrCmd.Parameters.Add(New SqliteParameter("@productId", productId))
                        Using shrReader = shrCmd.ExecuteReader()
                            While shrReader.Read()
                                _batchesForShrinkage.Add(StockService.ReadStockBatch(shrReader))
                            End While
                        End Using
                    End Using
                End Using
                Dim batches As List(Of StockBatch) = _batchesForShrinkage

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
            _shrinkageHistoryList = New List(Of ShrinkageRecord)()
            Dim shConnStr = _db.Database.GetConnectionString()
            Using shConn As New SqliteConnection(shConnStr)
                Await shConn.OpenAsync()

                Dim shSql = "SELECT Id, ProductId, StockBatchId, QuantityLost, UnitCost, TotalValue, " &
                             "Reason, Notes, RecordedDate, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                             "FROM Inv_ShrinkageRecords"
                If productId.HasValue Then
                    shSql &= " WHERE ProductId = @productId"
                End If
                shSql &= " ORDER BY RecordedDate DESC"
                Using shCmd = shConn.CreateCommand()
                    shCmd.CommandText = shSql
                    If productId.HasValue Then
                        shCmd.Parameters.Add(New SqliteParameter("@productId", productId.Value))
                    End If
                    Using shReader = shCmd.ExecuteReader()
                        While shReader.Read()
                            _shrinkageHistoryList.Add(New ShrinkageRecord With {
                                .Id = shReader.GetInt32(0),
                                .ProductId = shReader.GetInt32(1),
                                .StockBatchId = If(shReader.IsDBNull(2), CType(Nothing, Integer?), shReader.GetInt32(2)),
                                .QuantityLost = shReader.GetInt32(3),
                                .UnitCost = shReader.GetDecimal(4),
                                .TotalValue = shReader.GetDecimal(5),
                                .Reason = shReader.GetString(6),
                                .Notes = If(shReader.IsDBNull(7), Nothing, shReader.GetString(7)),
                                .RecordedDate = shReader.GetDateTime(8),
                                .CreatedBy = shReader.GetString(9),
                                .CreatedAt = shReader.GetDateTime(10),
                                .ModifiedBy = If(shReader.IsDBNull(11), Nothing, shReader.GetString(11)),
                                .ModifiedAt = If(shReader.IsDBNull(12), Nothing, CType(shReader.GetDateTime(12), DateTime?))
                            })
                        End While
                    End Using
                End Using

                If _shrinkageHistoryList.Count > 0 Then
                    Dim productIds = String.Join(",", _shrinkageHistoryList.Select(Function(s) s.ProductId).Distinct())
                    Dim productMap As New Dictionary(Of Integer, Product)()
                    Using pCmd = shConn.CreateCommand()
                        pCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                           "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_Products WHERE Id IN ({productIds})"
                        Using pReader = pCmd.ExecuteReader()
                            While pReader.Read()
                                Dim p = StockService.ReadProduct(pReader)
                                productMap(p.Id) = p
                            End While
                        End Using
                    End Using

                    Dim batchIds = _shrinkageHistoryList.Where(Function(s) s.StockBatchId.HasValue) _
                                                         .Select(Function(s) s.StockBatchId.Value).Distinct().ToList()
                    Dim batchMap As New Dictionary(Of Integer, StockBatch)()
                    If batchIds.Count > 0 Then
                        Dim batchIdList = String.Join(",", batchIds)
                        Using bCmd = shConn.CreateCommand()
                            bCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                               "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                               "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                               $"FROM Inv_StockBatches WHERE Id IN ({batchIdList})"
                            Using bReader = bCmd.ExecuteReader()
                                While bReader.Read()
                                    Dim b = StockService.ReadStockBatch(bReader)
                                    batchMap(b.Id) = b
                                End While
                            End Using
                        End Using
                    End If

                    For Each sr In _shrinkageHistoryList
                        Dim prod As Product = Nothing
                        If productMap.TryGetValue(sr.ProductId, prod) Then sr.Product = prod
                        If sr.StockBatchId.HasValue Then
                            Dim bat As StockBatch = Nothing
                            If batchMap.TryGetValue(sr.StockBatchId.Value, bat) Then sr.StockBatch = bat
                        End If
                    Next
                End If
            End Using
            Return _shrinkageHistoryList
        End Function

        Public Async Function GetTotalShrinkageValueAsync(startDate As DateTime, endDate As DateTime) As Task(Of Decimal) Implements IShrinkageService.GetTotalShrinkageValueAsync
            Return Await _db.ShrinkageRecords _
                .Where(Function(r) r.RecordedDate >= startDate AndAlso r.RecordedDate <= endDate) _
                .SumAsync(Function(r) r.TotalValue)
        End Function

    End Class

End Namespace
