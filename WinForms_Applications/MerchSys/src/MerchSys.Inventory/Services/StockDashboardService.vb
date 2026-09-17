Imports MySqlConnector
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
        ' _dashboardProductList removed — was a field causing race conditions on timer re-entry; now local

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

            Dim _dashboardProductList As New List(Of Product)()
            Dim dashConnStr = _db.Database.GetConnectionString()
            Using dashConn As New MySqlConnection(dashConnStr)
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
            Dim products As List(Of Product) = _dashboardProductList  ' local snapshot

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

            Dim detConnStr = _db.Database.GetConnectionString()
            Dim productName As String = String.Empty
            Dim categoryName As String = String.Empty
            Dim retailPrice As Decimal = 0D
            Dim unit As String = String.Empty
            Dim hasExpiry As Boolean = False
            Dim batches As New List(Of StockBatch)()
            Dim shrinkageList As New List(Of ShrinkageRecord)()

            Using detConn As New MySqlConnection(detConnStr)
                Await detConn.OpenAsync()

                ' Load product + category
                Using pCmd = detConn.CreateCommand()
                    pCmd.CommandText =
                        "SELECT p.Id, p.Name, p.RetailPrice, p.Unit, p.HasExpiry, " &
                        "COALESCE(c.Name, '') AS CategoryName " &
                        "FROM Inv_Products p LEFT JOIN Inv_ProductCategories c ON c.Id = p.CategoryId " &
                        "WHERE p.Id = @id AND p.IsDeleted = 0"
                    pCmd.Parameters.AddWithValue("@id", productId)
                    Using r = pCmd.ExecuteReader()
                        If Not r.Read() Then Throw New InvalidOperationException($"Product {productId} not found.")
                        productName = r.GetString(1)
                        retailPrice = r.GetDecimal(2)
                        unit = If(r.IsDBNull(3), String.Empty, r.GetString(3))
                        hasExpiry = r.GetBoolean(4)
                        categoryName = r.GetString(5)
                    End Using
                End Using

                ' Load stock batches
                Using bCmd = detConn.CreateCommand()
                    bCmd.CommandText =
                        "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                        "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                        "FROM Inv_StockBatches WHERE ProductId = @id ORDER BY ReceiptDate"
                    bCmd.Parameters.AddWithValue("@id", productId)
                    Using r = bCmd.ExecuteReader()
                        While r.Read()
                            batches.Add(StockService.ReadStockBatch(r))
                        End While
                    End Using
                End Using

                ' Load shrinkage records
                Using sCmd = detConn.CreateCommand()
                    sCmd.CommandText =
                        "SELECT Id, ProductId, StockBatchId, QuantityLost, UnitCost, TotalValue, Reason, Notes, " &
                        "RecordedDate, CreatedBy, CreatedAt, ModifiedAt " &
                        "FROM Inv_ShrinkageRecords WHERE ProductId = @id ORDER BY RecordedDate DESC"
                    sCmd.Parameters.AddWithValue("@id", productId)
                    Using r = sCmd.ExecuteReader()
                        While r.Read()
                            Dim s As New ShrinkageRecord With {
                                .Id = r.GetInt32(0),
                                .ProductId = r.GetInt32(1),
                                .StockBatchId = If(r.IsDBNull(2), CType(Nothing, Integer?), r.GetInt32(2)),
                                .QuantityLost = r.GetInt32(3),
                                .UnitCost = r.GetDecimal(4),
                                .TotalValue = r.GetDecimal(5),
                                .Reason = r.GetString(6),
                                .Notes = If(r.IsDBNull(7), String.Empty, r.GetString(7)),
                                .RecordedDate = r.GetDateTime(8)
                            }
                            shrinkageList.Add(s)
                        End While
                    End Using
                End Using
            End Using

            Dim batchDtos = batches.
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

            Dim shrinkageDtos = shrinkageList.
                Select(Function(s) New ShrinkageSummaryDto With {
                    .RecordId = s.Id,
                    .RecordedDate = s.RecordedDate,
                    .QuantityLost = s.QuantityLost,
                    .UnitCost = s.UnitCost,
                    .TotalValue = s.TotalValue,
                    .Reason = s.Reason,
                    .Notes = s.Notes
                }).ToList()

            Dim recentInflows = batches.
                Where(Function(b) b.ReceiptDate >= movementCutoff).
                Select(Function(b) New StockMovementDto With {
                    .MovementType = "Inflow",
                    .MovementDate = b.ReceiptDate,
                    .Quantity = b.QuantityReceived,
                    .UnitCost = b.UnitCost,
                    .Notes = If(b.SourcePurchaseOrderId.HasValue, $"PO#{b.SourcePurchaseOrderId}", "Manual receipt")
                })

            Dim recentShrinkage = shrinkageList.
                Where(Function(s) s.RecordedDate >= movementCutoff).
                Select(Function(s) New StockMovementDto With {
                    .MovementType = "Shrinkage",
                    .MovementDate = s.RecordedDate,
                    .Quantity = s.QuantityLost,
                    .UnitCost = s.UnitCost,
                    .Notes = $"{s.Reason}: {s.Notes}"
                })

            Dim recentMovements = recentInflows.Concat(recentShrinkage).OrderByDescending(Function(m) m.MovementDate).ToList()

            Dim nonExpiredBatches = batches.Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                                               (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= today))
            Dim currentStock As Integer = nonExpiredBatches.Sum(Function(b) b.QuantityRemaining)
            Dim stockValue As Decimal = nonExpiredBatches.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)

            Return New ProductDetailDto With {
                .ProductId = productId,
                .ProductName = productName,
                .Category = categoryName,
                .CurrentStock = currentStock,
                .RetailPrice = retailPrice,
                .StockValue = stockValue,
                .Unit = unit,
                .HasExpiry = hasExpiry,
                .StockBatches = batchDtos,
                .ShrinkageHistory = shrinkageDtos,
                .RecentMovements = recentMovements
            }
        End Function

    End Class

End Namespace
