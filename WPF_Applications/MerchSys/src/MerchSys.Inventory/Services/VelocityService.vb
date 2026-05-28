Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities

Namespace Services

    Public Class VelocityService
        Implements IVelocityService

        Private Const FastThreshold As Decimal = 2.0D
        Private Const ModerateThreshold As Decimal = 0.5D

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of VelocityService)
        Private _velocityProductList As List(Of Product)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of VelocityService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function ClassifyAllProductsAsync(daysToAnalyze As Integer) As Task(Of List(Of ProductVelocityDto)) Implements IVelocityService.ClassifyAllProductsAsync
            _velocityProductList = New List(Of Product)()
            Dim velConnStr = _db.Database.GetConnectionString()
            Using velConn As New MySqlConnection(velConnStr)
                Await velConn.OpenAsync()
                Using velCmd = velConn.CreateCommand()
                    velCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                         "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                         "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Inv_Products WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY Name"
                    Using velReader = velCmd.ExecuteReader()
                        While velReader.Read()
                            _velocityProductList.Add(StockService.ReadProduct(velReader))
                        End While
                    End Using
                End Using

                If _velocityProductList.Count > 0 Then
                    Dim pIds = String.Join(",", _velocityProductList.Select(Function(p) p.Id))

                    Dim batchMap As New Dictionary(Of Integer, List(Of StockBatch))()
                    Using bCmd = velConn.CreateCommand()
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

                    Dim shrinkageMap As New Dictionary(Of Integer, List(Of ShrinkageRecord))()
                    Using sCmd = velConn.CreateCommand()
                        sCmd.CommandText = "SELECT Id, ProductId, StockBatchId, QuantityLost, UnitCost, TotalValue, " &
                                           "Reason, Notes, RecordedDate, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_ShrinkageRecords WHERE ProductId IN ({pIds})"
                        Using sReader = sCmd.ExecuteReader()
                            While sReader.Read()
                                Dim s As New ShrinkageRecord With {
                                    .Id = sReader.GetInt32(0),
                                    .ProductId = sReader.GetInt32(1),
                                    .StockBatchId = If(sReader.IsDBNull(2), CType(Nothing, Integer?), sReader.GetInt32(2)),
                                    .QuantityLost = sReader.GetInt32(3),
                                    .UnitCost = sReader.GetDecimal(4),
                                    .TotalValue = sReader.GetDecimal(5),
                                    .Reason = sReader.GetString(6),
                                    .Notes = If(sReader.IsDBNull(7), Nothing, sReader.GetString(7)),
                                    .RecordedDate = sReader.GetDateTime(8),
                                    .CreatedBy = sReader.GetString(9),
                                    .CreatedAt = sReader.GetDateTime(10),
                                    .ModifiedBy = If(sReader.IsDBNull(11), Nothing, sReader.GetString(11)),
                                    .ModifiedAt = If(sReader.IsDBNull(12), Nothing, CType(sReader.GetDateTime(12), DateTime?))
                                }
                                If Not shrinkageMap.ContainsKey(s.ProductId) Then shrinkageMap(s.ProductId) = New List(Of ShrinkageRecord)()
                                shrinkageMap(s.ProductId).Add(s)
                            End While
                        End Using
                    End Using

                    Dim catIds = String.Join(",", _velocityProductList.Select(Function(p) p.CategoryId).Distinct())
                    Dim categoryMap As New Dictionary(Of Integer, ProductCategory)()
                    Using cCmd = velConn.CreateCommand()
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

                    For Each p In _velocityProductList
                        Dim pBatches As List(Of StockBatch) = Nothing
                        p.StockBatches = If(batchMap.TryGetValue(p.Id, pBatches), pBatches, New List(Of StockBatch)())
                        Dim pShrinkages As List(Of ShrinkageRecord) = Nothing
                        p.ShrinkageRecords = If(shrinkageMap.TryGetValue(p.Id, pShrinkages), pShrinkages, New List(Of ShrinkageRecord)())
                        Dim cat As ProductCategory = Nothing
                        If categoryMap.TryGetValue(p.CategoryId, cat) Then p.Category = cat
                    Next
                End If
            End Using
            Dim products As List(Of Product) = _velocityProductList

            Dim windowStart As DateTime = DateTime.UtcNow.Date.AddDays(-daysToAnalyze)
            Dim movementCounts = Await _db.StockMovements.
                Where(Function(m) m.MovementType = MovementType.Sale AndAlso m.OccurredAt >= windowStart).
                GroupBy(Function(m) m.ProductId).
                Select(Function(g) New With {.ProductId = g.Key, .Total = g.Sum(Function(m) m.Quantity)}).
                ToListAsync()
            Dim movementLookup = movementCounts.ToDictionary(Function(x) x.ProductId, Function(x) x.Total)

            Dim results As New List(Of ProductVelocityDto)()
            For Each product In products
                results.Add(ComputeVelocity(product, daysToAnalyze, movementLookup))
            Next

            _logger.LogInformation(
                "Velocity classification complete: {Count} products analysed over {Days} days (time-windowed={UseWindow}).",
                results.Count, daysToAnalyze, movementLookup.Count > 0)

            Return results
        End Function

        Public Async Function GetVelocityForProductAsync(productId As Integer, daysToAnalyze As Integer) As Task(Of ProductVelocityDto) Implements IVelocityService.GetVelocityForProductAsync
            Dim product As Product = Await _db.Products.
                Include(Function(p) p.Category).
                Include(Function(p) p.StockBatches).
                Include(Function(p) p.ShrinkageRecords).
                FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)

            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim windowStart As DateTime = DateTime.UtcNow.Date.AddDays(-daysToAnalyze)
            Dim windowSales As Integer = Await _db.StockMovements.
                Where(Function(m) m.ProductId = productId AndAlso
                                  m.MovementType = MovementType.Sale AndAlso
                                  m.OccurredAt >= windowStart).
                SumAsync(Function(m) m.Quantity)

            Dim lookup = If(windowSales > 0,
                            New Dictionary(Of Integer, Integer) From {{productId, windowSales}},
                            New Dictionary(Of Integer, Integer)())

            Return ComputeVelocity(product, daysToAnalyze, lookup)
        End Function

        Private Function ComputeVelocity(product As Product,
                                         daysToAnalyze As Integer,
                                         movementLookup As Dictionary(Of Integer, Integer)) As ProductVelocityDto
            Dim today As DateTime = DateTime.UtcNow.Date

            Dim batches As List(Of StockBatch) = product.StockBatches.ToList()
            Dim shrinkageRecords As List(Of ShrinkageRecord) = product.ShrinkageRecords.ToList()

            Dim totalUnitsSold As Integer
            Dim windowSales As Integer = 0
            If movementLookup.TryGetValue(product.Id, windowSales) Then
                ' Prefer time-windowed sale movements when available
                totalUnitsSold = windowSales
            Else
                ' Fall back to lifetime batch-total approximation
                Dim totalDeducted As Integer = batches.Sum(Function(b) b.QuantityReceived - b.QuantityRemaining)
                Dim totalShrinkage As Integer = shrinkageRecords.Sum(Function(s) s.QuantityLost)
                totalUnitsSold = Math.Max(0, totalDeducted - totalShrinkage)
            End If

            Dim avgDailySales As Decimal = 0D
            If daysToAnalyze > 0 Then
                avgDailySales = Math.Round(CDec(totalUnitsSold) / daysToAnalyze, 4)
            End If

            Dim nonExpiredBatches = batches.Where(
                Function(b) b.QuantityRemaining > 0 AndAlso
                            (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= today))
            Dim currentStock As Integer = nonExpiredBatches.Sum(Function(b) b.QuantityRemaining)

            Dim daysOfStockRemaining As Decimal? = Nothing
            If avgDailySales > 0D Then
                daysOfStockRemaining = Math.Round(CDec(currentStock) / avgDailySales, 1)
            End If

            Return New ProductVelocityDto With {
                .ProductId = product.Id,
                .ProductName = product.Name,
                .Category = If(product.Category IsNot Nothing, product.Category.Name, String.Empty),
                .TotalUnitsSold = totalUnitsSold,
                .AvgDailySales = avgDailySales,
                .DaysAnalyzed = daysToAnalyze,
                .Classification = Classify(avgDailySales),
                .CurrentStock = currentStock,
                .DaysOfStockRemaining = daysOfStockRemaining
            }
        End Function

        Private Shared Function Classify(avgDailySales As Decimal) As String
            If avgDailySales = 0D Then Return "Dead"
            If avgDailySales < ModerateThreshold Then Return "Slow"
            If avgDailySales < FastThreshold Then Return "Moderate"
            Return "Fast"
        End Function

    End Class

End Namespace
