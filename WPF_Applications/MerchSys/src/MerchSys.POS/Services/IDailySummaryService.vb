Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class PaymentMethodBreakdownDto
        Public Property PaymentMethod As PaymentMethod
        Public Property Count As Integer
        Public Property Total As Decimal
        Public Property Percentage As Decimal
    End Class

    Public Class TopProductDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantitySold As Integer
        Public Property TotalRevenue As Decimal
    End Class

    Public Class DailySummaryDto
        Public Property [Date] As DateTime
        Public Property TotalSales As Decimal
        Public Property TransactionCount As Integer
        Public Property AverageTransactionValue As Decimal
        Public Property PaymentBreakdown As List(Of PaymentMethodBreakdownDto)
        Public Property TopSellingProducts As List(Of TopProductDto)
        Public Property ReturnCount As Integer
        Public Property ReturnValue As Decimal
    End Class

    Public Class PeriodSummaryDto
        Public Property PeriodStart As DateTime
        Public Property PeriodEnd As DateTime
        Public Property TotalSales As Decimal
        Public Property TransactionCount As Integer
        Public Property AverageTransactionValue As Decimal
        Public Property PaymentBreakdown As List(Of PaymentMethodBreakdownDto)
        Public Property TopSellingProducts As List(Of TopProductDto)
        Public Property ReturnCount As Integer
        Public Property ReturnValue As Decimal
        Public Property DailyBreakdown As List(Of DailySummaryDto)
    End Class

    Public Interface IDailySummaryService
        Function GetDailySummaryAsync(targetDate As DateTime) As Task(Of DailySummaryDto)
        Function GetWeeklySummaryAsync(weekStartDate As DateTime) As Task(Of PeriodSummaryDto)
        Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of PeriodSummaryDto)
    End Interface

End Namespace
