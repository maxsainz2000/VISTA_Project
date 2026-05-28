Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    ''' <summary>
    ''' Thrown when a FIFO deduction is requested but insufficient non-expired stock is available.
    ''' </summary>
    Public Class InsufficientStockException
        Inherits Exception

        Public Property ProductId As Integer
        Public Property RequestedQuantity As Integer
        Public Property AvailableQuantity As Integer

        Public Sub New(product As Integer, requested As Integer, available As Integer)
            MyBase.New($"Insufficient stock for product {product}. Requested: {requested}, Available: {available}.")
            Me.ProductId = product
            Me.RequestedQuantity = requested
            Me.AvailableQuantity = available
        End Sub

    End Class

    Public Class StockService
        Implements IStockService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of StockService)
        Private _batchesForFIFO As List(Of StockBatch)
        Private _batchesForProduct As List(Of StockBatch)
        Private _productStockList As List(Of Product)

        Public Sub New(db As InventoryDbContext,
                       logger As ILogger(Of StockService))
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
            _batchesForFIFO = New List(Of StockBatch)()
            Dim fifoConnStr = _db.Database.GetConnectionString()
            Dim results As New List(Of FIFODeductionResult)()

            Using fifoConn As New MySqlConnection(fifoConnStr)
                Await fifoConn.OpenAsync()
                ' Begin explicit database transaction (Read Committed isolation is default and fits perfectly)
                Using tx = Await fifoConn.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
                    ' 1. Retrieve the oldest non-depleted, non-expired batches for this product and lock them (FOR UPDATE)
                    Using fifoCmd = fifoConn.CreateCommand()
                        fifoCmd.Transaction = tx
                        fifoCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                              "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                              "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                              "FROM Inv_StockBatches " &
                                              "WHERE ProductId = @productId AND QuantityRemaining > 0 " &
                                              "AND (ExpiryDate IS NULL OR ExpiryDate >= @now) " &
                                              "ORDER BY ReceiptDate ASC, Id ASC " &
                                              "FOR UPDATE"
                        fifoCmd.Parameters.Add(New MySqlParameter("@productId", productId))
                        fifoCmd.Parameters.Add(New MySqlParameter("@now", now))
                        Using fifoReader = Await fifoCmd.ExecuteReaderAsync()
                            While Await fifoReader.ReadAsync()
                                _batchesForFIFO.Add(ReadStockBatch(fifoReader))
                            End While
                        End Using
                    End Using

                    Dim batches As List(Of StockBatch) = _batchesForFIFO
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

                    ' 2. Persist the updated batch stock levels back to the DB on the same transaction
                    Dim touchedBatchIds = results.Select(Function(r) r.BatchId).ToHashSet()
                    For Each b In batches.Where(Function(x) touchedBatchIds.Contains(x.Id))
                        Using updateCmd = fifoConn.CreateCommand()
                            updateCmd.Transaction = tx
                            updateCmd.CommandText = "UPDATE Inv_StockBatches SET QuantityRemaining = @qty, ModifiedAt = @now WHERE Id = @id"
                            updateCmd.Parameters.Add(New MySqlParameter("@qty", b.QuantityRemaining))
                            updateCmd.Parameters.Add(New MySqlParameter("@now", DateTime.UtcNow))
                            updateCmd.Parameters.Add(New MySqlParameter("@id", b.Id))
                            Await updateCmd.ExecuteNonQueryAsync()
                        End Using
                    Next

                    ' 3. Commit the transaction to apply changes and release row locks
                    Await tx.CommitAsync()
                End Using
            End Using

            ' 4. Record stock movement
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
            _productStockList = New List(Of Product)()
            Dim csConnStr = _db.Database.GetConnectionString()
            Using csConn As New MySqlConnection(csConnStr)
                Await csConn.OpenAsync()
                Dim productSql = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                 "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                 "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                 "FROM Inv_Products WHERE IsDeleted = 0 AND IsActive = 1"
                If productId.HasValue Then
                    productSql &= " AND Id = @productId"
                End If
                Using csCmd = csConn.CreateCommand()
                    csCmd.CommandText = productSql
                    If productId.HasValue Then
                        csCmd.Parameters.Add(New MySqlParameter("@productId", productId.Value))
                    End If
                    Using csReader = csCmd.ExecuteReader()
                        While csReader.Read()
                            _productStockList.Add(ReadProduct(csReader))
                        End While
                    End Using
                End Using

                If _productStockList.Count > 0 Then
                    Dim productIds = String.Join(",", _productStockList.Select(Function(p) p.Id))
                    Dim batchMap As New Dictionary(Of Integer, List(Of StockBatch))()
                    Using batchCmd = csConn.CreateCommand()
                        batchCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                               "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                               "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                               $"FROM Inv_StockBatches WHERE ProductId IN ({productIds})"
                        Using batchReader = batchCmd.ExecuteReader()
                            While batchReader.Read()
                                Dim b = ReadStockBatch(batchReader)
                                If Not batchMap.ContainsKey(b.ProductId) Then batchMap(b.ProductId) = New List(Of StockBatch)()
                                batchMap(b.ProductId).Add(b)
                            End While
                        End Using
                    End Using
                    For Each p In _productStockList
                        Dim batches As List(Of StockBatch) = Nothing
                        If batchMap.TryGetValue(p.Id, batches) Then
                            p.StockBatches = batches
                        End If
                    Next
                End If
            End Using
            Dim products As List(Of Product) = _productStockList

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
            _batchesForProduct = New List(Of StockBatch)()
            Dim bpConnStr = _db.Database.GetConnectionString()
            Using bpConn As New MySqlConnection(bpConnStr)
                Await bpConn.OpenAsync()
                Using bpCmd = bpConn.CreateCommand()
                    bpCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                        "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Inv_StockBatches WHERE ProductId = @productId ORDER BY ReceiptDate"
                    bpCmd.Parameters.Add(New MySqlParameter("@productId", productId))
                    Using bpReader = bpCmd.ExecuteReader()
                        While bpReader.Read()
                            _batchesForProduct.Add(ReadStockBatch(bpReader))
                        End While
                    End Using
                End Using
            End Using
            Return _batchesForProduct
        End Function

        Public Async Function GetTotalValuationAsync() As Task(Of Decimal) Implements IStockService.GetTotalValuationAsync
            Dim now As DateTime = DateTime.UtcNow
            Return Await _db.StockBatches _
                .Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                .SumAsync(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)
        End Function

        Friend Shared Function ReadStockBatch(r As MySqlConnector.MySqlDataReader) As StockBatch
            Return New StockBatch With {
                .Id = r.GetInt32(0),
                .ProductId = r.GetInt32(1),
                .QuantityReceived = r.GetInt32(2),
                .QuantityRemaining = r.GetInt32(3),
                .UnitCost = r.GetDecimal(4),
                .ReceiptDate = r.GetDateTime(5),
                .ExpiryDate = If(r.IsDBNull(6), CType(Nothing, DateTime?), CType(r.GetDateTime(6), DateTime?)),
                .SourcePurchaseOrderId = If(r.IsDBNull(7), CType(Nothing, Integer?), r.GetInt32(7)),
                .CreatedBy = r.GetString(8),
                .CreatedAt = r.GetDateTime(9),
                .ModifiedBy = If(r.IsDBNull(10), Nothing, r.GetString(10)),
                .ModifiedAt = If(r.IsDBNull(11), Nothing, CType(r.GetDateTime(11), DateTime?))
            }
        End Function

        Friend Shared Function ReadProduct(r As MySqlConnector.MySqlDataReader) As Product
            Return New Product With {
                .Id = r.GetInt32(0),
                .Name = r.GetString(1),
                .Sku = r.GetString(2),
                .CategoryId = r.GetInt32(3),
                .Description = If(r.IsDBNull(4), Nothing, r.GetString(4)),
                .RetailPrice = r.GetDecimal(5),
                .Unit = If(r.IsDBNull(6), Nothing, r.GetString(6)),
                .HasExpiry = r.GetBoolean(7),
                .MinimumThreshold = r.GetInt32(8),
                .IsActive = r.GetBoolean(9),
                .IsDeleted = r.GetBoolean(10),
                .DeletedBy = If(r.IsDBNull(11), Nothing, r.GetString(11)),
                .DeletedAt = If(r.IsDBNull(12), Nothing, CType(r.GetDateTime(12), DateTime?)),
                .CreatedBy = r.GetString(13),
                .CreatedAt = r.GetDateTime(14),
                .ModifiedBy = If(r.IsDBNull(15), Nothing, r.GetString(15)),
                .ModifiedAt = If(r.IsDBNull(16), Nothing, CType(r.GetDateTime(16), DateTime?))
            }
        End Function

    End Class

End Namespace
