Imports MerchSys.Inventory.Entities

Namespace Services

    Public Interface IShrinkageService
        Function RecordShrinkageAsync(productId As Integer, quantity As Integer, reason As String, notes As String, Optional batchId As Integer? = Nothing) As Task(Of ShrinkageRecord)
        Function GetShrinkageHistoryAsync(Optional productId As Integer? = Nothing) As Task(Of List(Of ShrinkageRecord))
        Function GetTotalShrinkageValueAsync(startDate As DateTime, endDate As DateTime) As Task(Of Decimal)
    End Interface

End Namespace
