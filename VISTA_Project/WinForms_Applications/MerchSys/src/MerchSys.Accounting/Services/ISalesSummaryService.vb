Namespace Services

    Public Interface ISalesSummaryService
        Function GetDailySummaryAsync(targetDate As DateTime) As Task(Of AccountingSalesSummaryDto)
        Function GetWeeklySummaryAsync(weekStart As DateTime) As Task(Of AccountingSalesSummaryDto)
        Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of AccountingSalesSummaryDto)
        Function GetPaymentMethodTrendAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of PaymentTrendDto))
    End Interface

    Public Class AccountingSalesSummaryDto
        Public Property PeriodDescription As String
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property TotalGrossSales As Decimal
        Public Property TotalDiscounts As Decimal
        Public Property TotalReturns As Decimal
        ''' <summary>TotalGrossSales - TotalDiscounts - TotalReturns</summary>
        Public Property TotalNetSales As Decimal
        Public Property TransactionCount As Integer
        Public Property PaymentBreakdown As List(Of PaymentBreakdownDto)
        ''' <summary>Per-day totals populated for weekly and monthly summaries.</summary>
        Public Property DailyBreakdown As List(Of DailySalesDto)
    End Class

    Public Class PaymentBreakdownDto
        Public Property PaymentMethod As String
        Public Property TransactionCount As Integer
        Public Property GrossAmount As Decimal
        Public Property NetAmount As Decimal
        ''' <summary>NetAmount as a percentage of total net revenue; 0 when total is 0.</summary>
        Public Property Percentage As Decimal
    End Class

    Public Class DailySalesDto
        Public Property SalesDate As DateTime
        Public Property GrossSales As Decimal
        Public Property Discounts As Decimal
        Public Property Returns As Decimal
        Public Property NetSales As Decimal
        Public Property TransactionCount As Integer
    End Class

    Public Class PaymentTrendDto
        Public Property [Date] As DateTime
        Public Property CashTotal As Decimal
        Public Property GCashTotal As Decimal
        Public Property BankTransferTotal As Decimal
        Public Property CreditTotal As Decimal
    End Class

End Namespace
