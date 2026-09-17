Imports MerchSys.Accounting.Entities

Namespace Services

    Public Interface IFinancialOverviewService
        Function GetOverviewAsync() As Task(Of FinancialOverviewDto)
        Function RefreshSnapshotAsync() As Task(Of FinancialSnapshot)
    End Interface

    Public Class FinancialOverviewDto
        Public Property TodayRevenue As Decimal
        Public Property MonthToDateRevenue As Decimal
        Public Property YearToDateRevenue As Decimal
        Public Property TodayTransactions As Integer
        Public Property MonthTransactions As Integer
        ''' <summary>This month's gross margin as a percentage.</summary>
        Public Property CurrentGrossMargin As Decimal
        Public Property TotalAR As Decimal
        Public Property TotalAP As Decimal
        Public Property InventoryValue As Decimal
        Public Property MonthlyTrend As List(Of MonthlyTrendDto) = New List(Of MonthlyTrendDto)()
        Public Property TopProducts As List(Of TopProductDto) = New List(Of TopProductDto)()
        Public Property OverdueARCount As Integer
        Public Property OverdueAPCount As Integer
        Public Property LowStockAlertCount As Integer
    End Class

    Public Class MonthlyTrendDto
        ''' <summary>Display label, e.g. "Jan 2026".</summary>
        Public Property Month As String
        Public Property Revenue As Decimal
        Public Property COGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property GrossMarginPercent As Decimal
    End Class

    Public Class TopProductDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Revenue As Decimal
        Public Property COGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property GrossMarginPercent As Decimal
        Public Property QuantitySold As Integer
    End Class

End Namespace
