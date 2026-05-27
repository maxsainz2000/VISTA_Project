Imports Microsoft.Data.Sqlite
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
        Private _dashboardProductList As List(Of Product)

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

            _dashboardProductList = New List(Of Product)()
            Dim dashConnStr = _db.Database.GetConnectionString()
            Using dashConn As New SqliteConnection(dashConnStr)
                Await dashConn.OpenAsync()
                Using dashCmd = dashConn.CreateCommand()
                    dashCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                          "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                          "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                          "FROM Inv_Products WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY Name"
                    Using dashReader = dashCmd.ExecuteReader()
                        While dashReader.Read()
                            _dashboardProductList.Add(StockService.ReadProduct(dashReader))
                        End While
                    End Using
                End Using

                If _dashboardProductList.Count > 0 Then
                    Dim pIds = String.Join(",", _dashboardProductList.Select(Function(p) p.Id))

                    Dim batchMap As New Dictionary(Of Integer, List(Of StockBatch))()
                    Using bCmd = dashConn.CreateCommand()
                        bCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                           "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_StockBatches WHERE ProductId IN ({pIds})"
                        Using bReader = bCmd.ExecuteReader()
                            While bReader.Read()
                                Dim b = StockService.ReadStockBatch(bReader)
                                If Not batchMap.ContainsKey(b.ProductId) Then batchMap(b.ProductId) = New List(Of StockBatch)()
                                batchMap(b.ProductId).Add(b)
                            End While
                        End Using
                    End Using

                    Dim catIds = String.Join(",", _dashboardProductList.Select(Function(p) p.CategoryId).Distinct())
                    Dim categoryMap As New Dictionary(Of Integer, ProductCategory)()
                    Using cCmd = dashConn.CreateCommand()
                        cCmd.CommandText = "SELECT Id, Name, Description, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_ProductCategories WHERE Id IN ({catIds})"
                        Using cReader = cCmd.ExecuteReader()
                            While cReader.Read()
                                Dim cat As New ProductCategory With {
                                    .Id = cReader.GetInt32(0),
                                    .Name = cReader.GetString(1),
                                    .Description = If(cReader.IsDBNull(2), Nothing, cReader.GetString(2)),
                                    .IsDeleted = cReader.GetBoolean(3),
                                    .DeletedBy = If(cReader.IsDBNull(4), Nothing, cReader.GetString(4)),
                                    .DeletedAt = If(cReader.IsDBNull(5), Nothing, CType(cReader.GetDateTime(5), DateTime?)),
                                    .CreatedBy = cReader.GetString(6),
                                    .CreatedAt = cReader.GetDateTime(7),
                                    .ModifiedBy = If(cReader.IsDBNull(8), Nothing, cReader.GetString(8)),
                                    .ModifiedAt = If(cReader.IsDBNull(9), Nothing, CType(cReader.GetDateTime(9), DateTime?))
                                }
                                categoryMap(cat.Id) = cat
                            End While
                        End Using
                    End Using

                    For Each p In _dashboardProductList
                        Dim pBatches As List(Of StockBatch) = Nothing
                        p.StockBatches = If(batchMap.TryGetValue(p.Id, pBatches), pBatches, New List(Of StockBatch)())
                        Dim cat As ProductCategory = Nothing
                        If categoryMap.TryGetValue(p.CategoryId, cat) Then p.Category = cat
                    Next
                End If
            End Using
            Dim products As List(Of Product) = _dashboardProductList

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

                Dim weightedCost As Decimal = 0D
                Dim fifoOldestCost As Decimal = 0D
                If currentStock > 0 Then
                    weightedCost = stockValue / CDec(currentStock)
                    Dim oldestBatch = nonExpiredBatches.OrderBy(Function(b) b.ReceiptDate).FirstOrDefault()
                    If oldestBatch IsNot Nothing Then
                        fifoOldestCost = oldestBatch.UnitCost
                    End If
                End If

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
                    .HasExpiry = product.HasExpiry,
                    .AverageUnitCost = weightedCost,
                    .FifoOldestUnitCost = fifoOldestCost
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
