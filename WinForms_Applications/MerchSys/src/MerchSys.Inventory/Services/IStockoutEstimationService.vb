Namespace Services

    Public Interface IStockoutEstimationService
        Function EstimateAllAsync() As Task(Of List(Of StockoutEstimateDto))
        Function EstimateForProductAsync(productId As Integer) As Task(Of StockoutEstimateDto)
        Function GetCriticalProductsAsync(daysThreshold As Integer) As Task(Of List(Of StockoutEstimateDto))
    End Interface

    Public Class StockoutEstimateDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentStock As Integer
        Public Property AvgDailySales As Decimal
        ''' <summary>CurrentStock / AvgDailySales. Nothing when AvgDailySales = 0 and CurrentStock > 0 (dead stock — infinite runway).</summary>
        Public Property EstimatedDaysUntilStockout As Decimal?
        ''' <summary>"Critical" (≤7 days or zero stock + zero sales), "Warning" (8–14 days), "OK" (>14 days or dead stock with stock on hand).</summary>
        Public Property RiskLevel As String
        ''' <summary>Nothing when stockout is not driven by sales (dead stock).</summary>
        Public Property EstimatedStockoutDate As DateTime?
    End Class

End Namespace
