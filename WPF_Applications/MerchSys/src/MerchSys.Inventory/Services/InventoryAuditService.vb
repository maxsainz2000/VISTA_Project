Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities

Namespace Services

    Public Class InventoryAuditService
        Implements IInventoryAuditService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of InventoryAuditService)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of InventoryAuditService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function PerformStockCountAsync(productId As Integer, physicalCount As Integer, performedBy As String, notes As String) As Task(Of StockAuditRecord) Implements IInventoryAuditService.PerformStockCountAsync
            If physicalCount < 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(physicalCount), "Physical count cannot be negative.")
            End If
            If String.IsNullOrWhiteSpace(performedBy) Then
                Throw New ArgumentException("PerformedBy is required.", NameOf(performedBy))
            End If

            Dim product As Product = Await _db.Products _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)
            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim expectedQty As Integer = If(
                Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = productId) _
                    .Select(Function(b) CType(b.QuantityRemaining, Integer?)) _
                    .SumAsync(),
                0)

            Dim variance As Integer = physicalCount - expectedQty
            Dim now As DateTime = DateTime.UtcNow

            Dim record As New StockAuditRecord With {
                .ProductId = productId,
                .ExpectedQuantity = expectedQty,
                .PhysicalCount = physicalCount,
                .Variance = variance,
                .Reason = "Stock Count",
                .Notes = If(notes, String.Empty),
                .PerformedBy = performedBy,
                .AuditedAt = now,
                .CreatedBy = performedBy,
                .CreatedAt = now
            }
            _db.StockAuditRecords.Add(record)

            If variance <> 0 Then
                _db.StockMovements.Add(New StockMovement With {
                    .ProductId = productId,
                    .MovementType = MovementType.Adjustment,
                    .Quantity = variance,
                    .OccurredAt = now,
                    .CreatedBy = performedBy,
                    .CreatedAt = now
                })
            End If

            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Stock count performed: ProductId={ProductId}, Expected={Expected}, Physical={Physical}, Variance={Variance}, By={By}",
                productId, expectedQty, physicalCount, variance, performedBy)

            Return record
        End Function

        Public Async Function RecordAdjustmentAsync(productId As Integer, adjustedQuantity As Integer, reason As String, performedBy As String) As Task(Of StockAuditRecord) Implements IInventoryAuditService.RecordAdjustmentAsync
            If String.IsNullOrWhiteSpace(reason) Then
                Throw New ArgumentException("Reason is required.", NameOf(reason))
            End If
            If String.IsNullOrWhiteSpace(performedBy) Then
                Throw New ArgumentException("PerformedBy is required.", NameOf(performedBy))
            End If

            Dim product As Product = Await _db.Products _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)
            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim currentQty As Integer = If(
                Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = productId) _
                    .Select(Function(b) CType(b.QuantityRemaining, Integer?)) _
                    .SumAsync(),
                0)

            Dim now As DateTime = DateTime.UtcNow

            Dim record As New StockAuditRecord With {
                .ProductId = productId,
                .ExpectedQuantity = currentQty,
                .PhysicalCount = adjustedQuantity,
                .Variance = adjustedQuantity - currentQty,
                .Reason = reason,
                .Notes = String.Empty,
                .PerformedBy = performedBy,
                .AuditedAt = now,
                .CreatedBy = performedBy,
                .CreatedAt = now
            }
            _db.StockAuditRecords.Add(record)

            Dim variance As Integer = adjustedQuantity - currentQty
            If variance <> 0 Then
                _db.StockMovements.Add(New StockMovement With {
                    .ProductId = productId,
                    .MovementType = MovementType.Adjustment,
                    .Quantity = variance,
                    .OccurredAt = now,
                    .CreatedBy = performedBy,
                    .CreatedAt = now
                })
            End If

            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Stock adjustment recorded: ProductId={ProductId}, Adjusted={Adjusted}, Variance={Variance}, Reason={Reason}, By={By}",
                productId, adjustedQuantity, variance, reason, performedBy)

            Return record
        End Function

        Public Async Function GetAuditHistoryAsync(Optional productId As Integer? = Nothing, Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetAuditHistoryAsync
            Dim query = _db.StockAuditRecords _
                .Include(Function(a) a.Product) _
                .AsQueryable()

            If productId.HasValue Then
                query = query.Where(Function(a) a.ProductId = productId.Value)
            End If
            If startDate.HasValue Then
                query = query.Where(Function(a) a.AuditedAt >= startDate.Value)
            End If
            If endDate.HasValue Then
                query = query.Where(Function(a) a.AuditedAt <= endDate.Value)
            End If

            Return Await query.OrderByDescending(Function(a) a.AuditedAt).ToListAsync()
        End Function

        Public Async Function GetLatestAuditPerProductAsync() As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetLatestAuditPerProductAsync
            Return Await _db.StockAuditRecords _
                .Include(Function(a) a.Product) _
                .GroupBy(Function(a) a.ProductId) _
                .Select(Function(g) g.OrderByDescending(Function(a) a.AuditedAt).First()) _
                .ToListAsync()
        End Function

    End Class

End Namespace
