Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class ExpiryTrackingService
        Implements IExpiryTrackingService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of ExpiryTrackingService)
        Private _nearExpiryBatchList As List(Of StockBatch)
        Private _expiredBatchList As List(Of StockBatch)

        Public Sub New(db As InventoryDbContext,
                       logger As ILogger(Of ExpiryTrackingService))
            _db = db
            _logger = logger
        End Sub

        ''' <summary>
        ''' Returns batches for HasExpiry products where 0 &lt; DaysUntilExpiry &lt;= daysThreshold.
        ''' </summary>
        Public Async Function GetNearExpiryBatchesAsync(daysThreshold As Integer) As Task(Of List(Of ExpiryAlertDto)) Implements IExpiryTrackingService.GetNearExpiryBatchesAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim thresholdDate As DateTime = today.AddDays(daysThreshold)
            _nearExpiryBatchList = New List(Of StockBatch)()
            Dim neConnStr = _db.Database.GetConnectionString()
            Using neConn As New MySqlConnection(neConnStr)
                Await neConn.OpenAsync()
                Using neCmd = neConn.CreateCommand()
                    neCmd.CommandText = "SELECT b.Id, b.ProductId, b.QuantityReceived, b.QuantityRemaining, b.UnitCost, " &
                                        "b.ReceiptDate, b.ExpiryDate, b.SourcePurchaseOrderId, " &
                                        "b.CreatedBy, b.CreatedAt, b.ModifiedBy, b.ModifiedAt " &
                                        "FROM Inv_StockBatches b " &
                                        "INNER JOIN Inv_Products p ON p.Id = b.ProductId " &
                                        "WHERE p.HasExpiry = 1 AND b.QuantityRemaining > 0 " &
                                        "AND b.ExpiryDate IS NOT NULL " &
                                        "AND b.ExpiryDate >= @today AND b.ExpiryDate <= @threshold " &
                                        "ORDER BY b.ExpiryDate"
                    neCmd.Parameters.Add(New MySqlParameter("@today", today.ToString("o")))
                    neCmd.Parameters.Add(New MySqlParameter("@threshold", thresholdDate.ToString("o")))
                    Using neReader = neCmd.ExecuteReader()
                        While neReader.Read()
                            _nearExpiryBatchList.Add(StockService.ReadStockBatch(neReader))
                        End While
                    End Using
                End Using
                If _nearExpiryBatchList.Count > 0 Then
                    Dim pIds = String.Join(",", _nearExpiryBatchList.Select(Function(b) b.ProductId).Distinct())
                    Dim productMap As New Dictionary(Of Integer, Product)()
                    Using pCmd = neConn.CreateCommand()
                        pCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                           "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_Products WHERE Id IN ({pIds})"
                        Using pReader = pCmd.ExecuteReader()
                            While pReader.Read()
                                Dim p = StockService.ReadProduct(pReader)
                                productMap(p.Id) = p
                            End While
                        End Using
                    End Using
                    For Each b In _nearExpiryBatchList
                        Dim prod As Product = Nothing
                        If productMap.TryGetValue(b.ProductId, prod) Then b.Product = prod
                    Next
                End If
            End Using
            Dim batches As List(Of StockBatch) = _nearExpiryBatchList

            Return batches.Select(Function(b)
                Dim days As Integer = CInt((b.ExpiryDate.Value.Date - today).TotalDays)
                Return New ExpiryAlertDto With {
                    .BatchId = b.Id,
                    .ProductId = b.ProductId,
                    .ProductName = b.Product.Name,
                    .QtyRemaining = b.QuantityRemaining,
                    .UnitCost = b.UnitCost,
                    .TotalValue = CDec(b.QuantityRemaining) * b.UnitCost,
                    .ExpiryDate = b.ExpiryDate.Value,
                    .DaysUntilExpiry = days,
                    .Status = "NearExpiry"
                }
            End Function).ToList()
        End Function

        ''' <summary>
        ''' Returns batches for HasExpiry products where ExpiryDate &lt; Today.
        ''' </summary>
        Public Async Function GetExpiredBatchesAsync() As Task(Of List(Of ExpiryAlertDto)) Implements IExpiryTrackingService.GetExpiredBatchesAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            _expiredBatchList = New List(Of StockBatch)()
            Dim expConnStr = _db.Database.GetConnectionString()
            Using expConn As New MySqlConnection(expConnStr)
                Await expConn.OpenAsync()
                Using expCmd = expConn.CreateCommand()
                    expCmd.CommandText = "SELECT b.Id, b.ProductId, b.QuantityReceived, b.QuantityRemaining, b.UnitCost, " &
                                         "b.ReceiptDate, b.ExpiryDate, b.SourcePurchaseOrderId, " &
                                         "b.CreatedBy, b.CreatedAt, b.ModifiedBy, b.ModifiedAt " &
                                         "FROM Inv_StockBatches b " &
                                         "INNER JOIN Inv_Products p ON p.Id = b.ProductId " &
                                         "WHERE p.HasExpiry = 1 AND b.QuantityRemaining > 0 " &
                                         "AND b.ExpiryDate IS NOT NULL AND b.ExpiryDate < @today " &
                                         "ORDER BY b.ExpiryDate"
                    expCmd.Parameters.Add(New MySqlParameter("@today", today.ToString("o")))
                    Using expReader = expCmd.ExecuteReader()
                        While expReader.Read()
                            _expiredBatchList.Add(StockService.ReadStockBatch(expReader))
                        End While
                    End Using
                End Using
                If _expiredBatchList.Count > 0 Then
                    Dim pIds = String.Join(",", _expiredBatchList.Select(Function(b) b.ProductId).Distinct())
                    Dim productMap As New Dictionary(Of Integer, Product)()
                    Using pCmd = expConn.CreateCommand()
                        pCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                           "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_Products WHERE Id IN ({pIds})"
                        Using pReader = pCmd.ExecuteReader()
                            While pReader.Read()
                                Dim p = StockService.ReadProduct(pReader)
                                productMap(p.Id) = p
                            End While
                        End Using
                    End Using
                    For Each b In _expiredBatchList
                        Dim prod As Product = Nothing
                        If productMap.TryGetValue(b.ProductId, prod) Then b.Product = prod
                    Next
                End If
            End Using
            Dim batches As List(Of StockBatch) = _expiredBatchList

            Return batches.Select(Function(b)
                Dim days As Integer = CInt((b.ExpiryDate.Value.Date - today).TotalDays)
                Return New ExpiryAlertDto With {
                    .BatchId = b.Id,
                    .ProductId = b.ProductId,
                    .ProductName = b.Product.Name,
                    .QtyRemaining = b.QuantityRemaining,
                    .UnitCost = b.UnitCost,
                    .TotalValue = CDec(b.QuantityRemaining) * b.UnitCost,
                    .ExpiryDate = b.ExpiryDate.Value,
                    .DaysUntilExpiry = days,
                    .Status = "Expired"
                }
            End Function).ToList()
        End Function

        ''' <summary>
        ''' Returns aggregated expiry status for a single product, split into near-expiry and expired alerts.
        ''' </summary>
        Public Async Function GetExpiryStatusForProductAsync(productId As Integer) As Task(Of ProductExpiryStatusDto) Implements IExpiryTrackingService.GetExpiryStatusForProductAsync
            Dim today As DateTime = DateTime.UtcNow.Date

            Dim product As Product = Await _db.Products _
                .Include(Function(p) p.StockBatches) _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)

            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim dto As New ProductExpiryStatusDto With {
                .ProductId = product.Id,
                .ProductName = product.Name
            }

            If Not product.HasExpiry Then
                Return dto
            End If

            For Each b In product.StockBatches
                If Not b.ExpiryDate.HasValue OrElse b.QuantityRemaining = 0 Then
                    Continue For
                End If

                Dim days As Integer = CInt((b.ExpiryDate.Value.Date - today).TotalDays)
                Dim alert As New ExpiryAlertDto With {
                    .BatchId = b.Id,
                    .ProductId = b.ProductId,
                    .ProductName = product.Name,
                    .QtyRemaining = b.QuantityRemaining,
                    .UnitCost = b.UnitCost,
                    .TotalValue = CDec(b.QuantityRemaining) * b.UnitCost,
                    .ExpiryDate = b.ExpiryDate.Value,
                    .DaysUntilExpiry = days
                }

                If b.ExpiryDate.Value < today Then
                    alert.Status = "Expired"
                    dto.ExpiredAlerts.Add(alert)
                    dto.ExpiredBatchCount += 1
                    dto.TotalExpiredQty += b.QuantityRemaining
                    dto.ExpiredValue += alert.TotalValue
                Else
                    alert.Status = "NearExpiry"
                    dto.NearExpiryAlerts.Add(alert)
                    dto.NearExpiryBatchCount += 1
                    dto.TotalNearExpiryQty += b.QuantityRemaining
                    dto.NearExpiryValue += alert.TotalValue
                End If
            Next

            Return dto
        End Function

        ''' <summary>
        ''' Zeroes out an expired batch and records the financial write-off as a ShrinkageRecord.
        ''' </summary>
        Public Async Function WriteOffExpiredBatchAsync(batchId As Integer, reason As String) As Task(Of ShrinkageRecord) Implements IExpiryTrackingService.WriteOffExpiredBatchAsync
            Dim batch As StockBatch = Await _db.StockBatches _
                .Include(Function(b) b.Product) _
                .FirstOrDefaultAsync(Function(b) b.Id = batchId)

            If batch Is Nothing Then
                Throw New InvalidOperationException($"StockBatch {batchId} not found.")
            End If

            If Not batch.ExpiryDate.HasValue OrElse batch.ExpiryDate.Value >= DateTime.UtcNow.Date Then
                Throw New InvalidOperationException($"StockBatch {batchId} is not expired and cannot be written off as expiry.")
            End If

            Dim qtyLost As Integer = batch.QuantityRemaining
            Dim totalValue As Decimal = CDec(qtyLost) * batch.UnitCost

            Dim shrinkage As New ShrinkageRecord With {
                .ProductId = batch.ProductId,
                .StockBatchId = batch.Id,
                .QuantityLost = qtyLost,
                .UnitCost = batch.UnitCost,
                .TotalValue = totalValue,
                .Reason = "Expiry",
                .Notes = reason,
                .RecordedDate = DateTime.UtcNow
            }

            batch.QuantityRemaining = 0
            _db.ShrinkageRecords.Add(shrinkage)
            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Expired batch written off: BatchId={BatchId}, ProductId={ProductId}, QtyLost={QtyLost}, TotalValue={TotalValue}",
                batchId, batch.ProductId, qtyLost, totalValue)

            Return shrinkage
        End Function

    End Class

End Namespace
