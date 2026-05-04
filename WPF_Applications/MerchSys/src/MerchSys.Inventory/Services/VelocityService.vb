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

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of VelocityService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function ClassifyAllProductsAsync(daysToAnalyze As Integer) As Task(Of List(Of ProductVelocityDto)) Implements IVelocityService.ClassifyAllProductsAsync
            Dim products As List(Of Product) = Await _db.Products.
                Where(Function(p) Not p.IsDeleted AndAlso p.IsActive).
                Include(Function(p) p.Category).
                Include(Function(p) p.StockBatches).
                Include(Function(p) p.ShrinkageRecords).
                OrderBy(Function(p) p.Name).
                ToListAsync()

            Dim results As New List(Of ProductVelocityDto)()
            For Each product In products
                results.Add(ComputeVelocity(product, daysToAnalyze))
            Next

            _logger.LogInformation(
                "Velocity classification complete: {Count} products analysed over {Days} days.",
                results.Count, daysToAnalyze)

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

            Return ComputeVelocity(product, daysToAnalyze)
        End Function

        Private Function ComputeVelocity(product As Product, daysToAnalyze As Integer) As ProductVelocityDto
            Dim today As DateTime = DateTime.UtcNow.Date

            Dim batches As List(Of StockBatch) = product.StockBatches.ToList()
            Dim shrinkageRecords As List(Of ShrinkageRecord) = product.ShrinkageRecords.ToList()

            ' StockBatch has no per-deduction timestamps. TotalDeducted uses lifetime batch totals
            ' as the velocity numerator; daysToAnalyze provides the denominator for the daily rate.
            Dim totalDeducted As Integer = batches.Sum(Function(b) b.QuantityReceived - b.QuantityRemaining)
            Dim totalShrinkage As Integer = shrinkageRecords.Sum(Function(s) s.QuantityLost)
            Dim totalUnitsSold As Integer = Math.Max(0, totalDeducted - totalShrinkage)

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
