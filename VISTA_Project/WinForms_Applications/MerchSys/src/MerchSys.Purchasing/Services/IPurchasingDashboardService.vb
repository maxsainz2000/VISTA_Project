Imports System.Threading.Tasks

Namespace Services

    Public Class TrendBarItem
        Public Property Month As String
        Public Property Spend As Decimal
        Public Property SpendBarHeight As Double
    End Class

    Public Class TopVendorItem
        Public Property VendorId As Integer
        Public Property VendorName As String
        Public Property POCount As Integer
        Public Property TotalSpend As Decimal
    End Class

    Public Interface IPurchasingDashboardService
        Function GetMonthlyTrendAsync(trendStart As DateTime) As Task(Of List(Of TrendBarItem))
        Function GetTopVendorsAsync() As Task(Of List(Of TopVendorItem))
    End Interface

End Namespace
