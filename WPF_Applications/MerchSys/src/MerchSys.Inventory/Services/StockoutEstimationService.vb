Imports Microsoft.Extensions.Logging

Namespace Services

    Public Class StockoutEstimationService
        Implements IStockoutEstimationService

        Private Const DefaultAnalysisDays As Integer = 30
        Private Const CriticalThresholdDays As Integer = 7
        Private Const WarningThresholdDays As Integer = 14

        Private ReadOnly _velocityService As IVelocityService
        Private ReadOnly _logger As ILogger(Of StockoutEstimationService)

        Public Sub New(velocityService As IVelocityService, logger As ILogger(Of StockoutEstimationService))
            _velocityService = velocityService
            _logger = logger
        End Sub

        Public Async Function EstimateAllAsync() As Task(Of List(Of StockoutEstimateDto)) Implements IStockoutEstimationService.EstimateAllAsync
            Dim velocities As List(Of ProductVelocityDto) = Await _velocityService.ClassifyAllProductsAsync(DefaultAnalysisDays)
            Dim results As New List(Of StockoutEstimateDto)(velocities.Count)

            For Each v In velocities
                results.Add(MapToEstimate(v))
            Next

            _logger.LogInformation("Stockout estimation complete: {Count} products evaluated.", results.Count)
            Return results
        End Function

        Public Async Function EstimateForProductAsync(productId As Integer) As Task(Of StockoutEstimateDto) Implements IStockoutEstimationService.EstimateForProductAsync
            Dim velocity As ProductVelocityDto = Await _velocityService.GetVelocityForProductAsync(productId, DefaultAnalysisDays)
            Return MapToEstimate(velocity)
        End Function

        Public Async Function GetCriticalProductsAsync(daysThreshold As Integer) As Task(Of List(Of StockoutEstimateDto)) Implements IStockoutEstimationService.GetCriticalProductsAsync
            Dim all As List(Of StockoutEstimateDto) = Await EstimateAllAsync()

            ' Include products with a known days-until-stockout at or under the threshold,
            ' plus zero-stock/zero-sales products (EstimatedDaysUntilStockout = 0).
            Dim critical = all.
                Where(Function(e) e.EstimatedDaysUntilStockout.HasValue AndAlso
                                  e.EstimatedDaysUntilStockout.Value <= daysThreshold).
                OrderBy(Function(e) e.EstimatedDaysUntilStockout.Value).
                ToList()

            _logger.LogInformation(
                "Critical products query (threshold={Days} days): {Count} products returned.",
                daysThreshold, critical.Count)

            Return critical
        End Function

        Private Shared Function MapToEstimate(v As ProductVelocityDto) As StockoutEstimateDto
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim daysUntilStockout As Decimal?
            Dim stockoutDate As DateTime?
            Dim riskLevel As String

            If v.AvgDailySales = 0D Then
                If v.CurrentStock = 0 Then
                    ' No stock and no sales velocity — already out or imminently critical.
                    daysUntilStockout = 0D
                    stockoutDate = today
                    riskLevel = "Critical"
                Else
                    ' Dead stock: has inventory but no sales movement — will not stockout from sales.
                    daysUntilStockout = Nothing
                    stockoutDate = Nothing
                    riskLevel = "OK"
                End If
            Else
                Dim days As Decimal = Math.Round(CDec(v.CurrentStock) / v.AvgDailySales, 1)
                daysUntilStockout = days
                stockoutDate = today.AddDays(CDbl(days))

                If days <= CriticalThresholdDays Then
                    riskLevel = "Critical"
                ElseIf days <= WarningThresholdDays Then
                    riskLevel = "Warning"
                Else
                    riskLevel = "OK"
                End If
            End If

            Return New StockoutEstimateDto With {
                .ProductId = v.ProductId,
                .ProductName = v.ProductName,
                .CurrentStock = v.CurrentStock,
                .AvgDailySales = v.AvgDailySales,
                .EstimatedDaysUntilStockout = daysUntilStockout,
                .RiskLevel = riskLevel,
                .EstimatedStockoutDate = stockoutDate
            }
        End Function

    End Class

End Namespace
