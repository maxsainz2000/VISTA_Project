Namespace Services

    Public Interface IVelocityService
        Function ClassifyAllProductsAsync(daysToAnalyze As Integer) As Task(Of List(Of ProductVelocityDto))
        Function GetVelocityForProductAsync(productId As Integer, daysToAnalyze As Integer) As Task(Of ProductVelocityDto)
    End Interface

    Public Class ProductVelocityDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Category As String
        ''' <summary>Lifetime units sold (deducted minus shrinkage), used as the velocity numerator.</summary>
        Public Property TotalUnitsSold As Integer
        ''' <summary>Average units sold per day: TotalUnitsSold / DaysAnalyzed.</summary>
        Public Property AvgDailySales As Decimal
        Public Property DaysAnalyzed As Integer
        ''' <summary>"Fast" ≥2.0/day, "Moderate" 0.5–2.0, "Slow" 0–0.5, "Dead" = zero sales.</summary>
        Public Property Classification As String
        Public Property CurrentStock As Integer
        ''' <summary>CurrentStock / AvgDailySales. Nothing when AvgDailySales = 0 (infinite runway).</summary>
        Public Property DaysOfStockRemaining As Decimal?
    End Class

End Namespace
