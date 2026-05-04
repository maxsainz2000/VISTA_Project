Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities

Namespace Services

    Public Class ExpiryTrackingService
        Implements IExpiryTrackingService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of ExpiryTrackingService)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of ExpiryTrackingService))
            _db = db
            _logger = logger
        End Sub

        ''' <summary>
        ''' Returns batches for HasExpiry products where 0 &lt; DaysUntilExpiry &lt;= daysThreshold.
        ''' </summary>
        Public Async Function GetNearExpiryBatchesAsync(daysThreshold As Integer) As Task(Of List(Of ExpiryAlertDto)) Implements IExpiryTrackingService.GetNearExpiryBatchesAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim thresholdDate As DateTime = today.AddDays(daysThreshold)

            Dim batches As List(Of StockBatch) = Await _db.StockBatches _
                .Include(Function(b) b.Product) _
                .Where(Function(b) b.Product.HasExpiry AndAlso
                                   b.QuantityRemaining > 0 AndAlso
                                   b.ExpiryDate.HasValue AndAlso
                                   b.ExpiryDate.Value >= today AndAlso
                                   b.ExpiryDate.Value <= thresholdDate) _
                .OrderBy(Function(b) b.ExpiryDate) _
                .ToListAsync()

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

            Dim batches As List(Of StockBatch) = Await _db.StockBatches _
                .Include(Function(b) b.Product) _
                .Where(Function(b) b.Product.HasExpiry AndAlso
                                   b.QuantityRemaining > 0 AndAlso
                                   b.ExpiryDate.HasValue AndAlso
                                   b.ExpiryDate.Value < today) _
                .OrderBy(Function(b) b.ExpiryDate) _
                .ToListAsync()

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
