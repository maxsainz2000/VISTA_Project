Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities

Namespace Services

    Public Class StockDashboardService
        Implements IStockDashboardService

        Private Const NearExpiryDays As Integer = 30

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of StockDashboardService)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of StockDashboardService))
            _db = db
            _logger = logger
        End Sub

        ''' <summary>
        ''' Aggregates all active products into a single dashboard snapshot: stock levels,
        ''' FIFO valuation, low-stock count, and batch-level expiry counts.
        ''' </summary>
        Public Async Function GetDashboardDataAsync() As Task(Of StockDashboardDto) Implements IStockDashboardService.GetDashboardDataAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim nearExpiryThreshold As DateTime = today.AddDays(NearExpiryDays)

            Dim products As List(Of Product) = Await _db.Products.
                Where(Function(p) Not p.IsDeleted AndAlso p.IsActive).
                Include(Function(p) p.Category).
                Include(Function(p) p.StockBatches).
                OrderBy(Function(p) p.Name).
                ToListAsync()

            Dim summaries As New List(Of ProductSummaryDto)()
            Dim totalStockValue As Decimal = 0D
            Dim lowStockCount As Integer = 0
            Dim nearExpiryBatchCount As Integer = 0
            Dim expiredBatchCount As Integer = 0

            For Each product In products
                Dim activeBatches = product.StockBatches.
                    Where(Function(b) b.QuantityRemaining > 0).
                    ToList()

                Dim nonExpiredBatches = activeBatches.
                    Where(Function(b) Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= today).
                    ToList()

                Dim currentStock As Integer = nonExpiredBatches.Sum(Function(b) b.QuantityRemaining)
                Dim stockValue As Decimal = nonExpiredBatches.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)
                totalStockValue += stockValue

                Dim stockStatus As String
                If currentStock = 0 Then
                    stockStatus = "Out"
                ElseIf currentStock <= product.MinimumThreshold Then
                    stockStatus = "Low"
                Else
                    stockStatus = "Normal"
                End If

                If currentStock <= product.MinimumThreshold Then
                    lowStockCount += 1
                End If

                Dim expiryStatus As String = "OK"
                If product.HasExpiry Then
                    Dim hasExpiredBatch As Boolean = activeBatches.
                        Any(Function(b) b.ExpiryDate.HasValue AndAlso b.ExpiryDate.Value < today)
                    Dim hasNearExpiryBatch As Boolean = activeBatches.
                        Any(Function(b) b.ExpiryDate.HasValue AndAlso
                                        b.ExpiryDate.Value >= today AndAlso
                                        b.ExpiryDate.Value <= nearExpiryThreshold)

                    nearExpiryBatchCount += Enumerable.Count(activeBatches, Function(b) b.ExpiryDate.HasValue AndAlso b.ExpiryDate.Value >= today AndAlso b.ExpiryDate.Value <= nearExpiryThreshold)
                    expiredBatchCount += Enumerable.Count(activeBatches, Function(b) b.ExpiryDate.HasValue AndAlso b.ExpiryDate.Value < today)

                    If hasExpiredBatch Then
                        expiryStatus = "HasExpired"
                    ElseIf hasNearExpiryBatch Then
                        expiryStatus = "NearExpiry"
                    End If
                End If

                summaries.Add(New ProductSummaryDto With {
                    .ProductId = product.Id,
                    .ProductName = product.Name,
                    .Category = If(product.Category IsNot Nothing, product.Category.Name, String.Empty),
                    .CurrentStock = currentStock,
                    .RetailPrice = product.RetailPrice,
                    .StockValue = stockValue,
                    .Unit = product.Unit,
                    .MinThreshold = product.MinimumThreshold,
                    .StockStatus = stockStatus,
                    .ExpiryStatus = expiryStatus,
                    .HasExpiry = product.HasExpiry
                })
            Next

            _logger.LogInformation(
                "Dashboard data built: Products={Count}, TotalValue={Value}, LowStock={Low}, NearExpiry={NearExpiry}, Expired={Expired}",
                summaries.Count, totalStockValue, lowStockCount, nearExpiryBatchCount, expiredBatchCount)

            Return New StockDashboardDto With {
                .TotalProducts = summaries.Count,
                .TotalStockValue = totalStockValue,
                .LowStockCount = lowStockCount,
                .NearExpiryCount = nearExpiryBatchCount,
                .ExpiredCount = expiredBatchCount,
                .Products = summaries
            }
        End Function

        ''' <summary>
        ''' Returns full batch-level breakdown, shrinkage history, and last-30-day movement log for a single product.
        ''' </summary>
        Public Async Function GetProductDetailAsync(productId As Integer) As Task(Of ProductDetailDto) Implements IStockDashboardService.GetProductDetailAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim movementCutoff As DateTime = today.AddDays(-30)

            Dim product As Product = Await _db.Products.
                Include(Function(p) p.Category).
                Include(Function(p) p.StockBatches).
                Include(Function(p) p.ShrinkageRecords).
                FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)

            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim batchDtos = product.StockBatches.
                OrderBy(Function(b) b.ReceiptDate).
                Select(Function(b) New StockBatchSummaryDto With {
                    .BatchId = b.Id,
                    .ReceiptDate = b.ReceiptDate,
                    .QuantityReceived = b.QuantityReceived,
                    .QuantityRemaining = b.QuantityRemaining,
                    .UnitCost = b.UnitCost,
                    .ExpiryDate = b.ExpiryDate,
                    .IsExpired = b.ExpiryDate.HasValue AndAlso b.ExpiryDate.Value < today,
                    .SourcePurchaseOrderId = b.SourcePurchaseOrderId
                }).ToList()

            Dim shrinkageDtos = product.ShrinkageRecords.
                OrderByDescending(Function(s) s.RecordedDate).
                Select(Function(s) New ShrinkageSummaryDto With {
                    .RecordId = s.Id,
                    .RecordedDate = s.RecordedDate,
                    .QuantityLost = s.QuantityLost,
                    .UnitCost = s.UnitCost,
                    .TotalValue = s.TotalValue,
                    .Reason = s.Reason,
                    .Notes = s.Notes
                }).ToList()

            Dim recentInflows = product.StockBatches.
                Where(Function(b) b.ReceiptDate >= movementCutoff).
                Select(Function(b) New StockMovementDto With {
                    .MovementType = "Inflow",
                    .MovementDate = b.ReceiptDate,
                    .Quantity = b.QuantityReceived,
                    .UnitCost = b.UnitCost,
                    .Notes = If(b.SourcePurchaseOrderId.HasValue,
                                $"PO#{b.SourcePurchaseOrderId}",
                                "Manual receipt")
                })

            Dim recentShrinkage = product.ShrinkageRecords.
                Where(Function(s) s.RecordedDate >= movementCutoff).
                Select(Function(s) New StockMovementDto With {
                    .MovementType = "Shrinkage",
                    .MovementDate = s.RecordedDate,
                    .Quantity = s.QuantityLost,
                    .UnitCost = s.UnitCost,
                    .Notes = $"{s.Reason}: {s.Notes}"
                })

            Dim recentMovements = recentInflows.
                Concat(recentShrinkage).
                OrderByDescending(Function(m) m.MovementDate).
                ToList()

            Dim nonExpiredBatches = product.StockBatches.
                Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                   (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= today))
            Dim currentStock As Integer = nonExpiredBatches.Sum(Function(b) b.QuantityRemaining)
            Dim stockValue As Decimal = nonExpiredBatches.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)

            Return New ProductDetailDto With {
                .ProductId = product.Id,
                .ProductName = product.Name,
                .Category = If(product.Category IsNot Nothing, product.Category.Name, String.Empty),
                .CurrentStock = currentStock,
                .RetailPrice = product.RetailPrice,
                .StockValue = stockValue,
                .Unit = product.Unit,
                .HasExpiry = product.HasExpiry,
                .StockBatches = batchDtos,
                .ShrinkageHistory = shrinkageDtos,
                .RecentMovements = recentMovements
            }
        End Function

    End Class

End Namespace
