Imports MerchSys.Inventory.Entities

Namespace Services

    Public Interface IExpiryTrackingService
        Function GetNearExpiryBatchesAsync(daysThreshold As Integer) As Task(Of List(Of ExpiryAlertDto))
        Function GetExpiredBatchesAsync() As Task(Of List(Of ExpiryAlertDto))
        Function GetExpiryStatusForProductAsync(productId As Integer) As Task(Of ProductExpiryStatusDto)
        Function WriteOffExpiredBatchAsync(batchId As Integer, reason As String) As Task(Of ShrinkageRecord)
    End Interface

    Public Class ExpiryAlertDto
        Public Property BatchId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QtyRemaining As Integer
        Public Property UnitCost As Decimal
        Public Property TotalValue As Decimal
        Public Property ExpiryDate As DateTime
        Public Property DaysUntilExpiry As Integer
        ''' <summary>"NearExpiry" or "Expired".</summary>
        Public Property Status As String
    End Class

    Public Class ProductExpiryStatusDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property NearExpiryBatchCount As Integer
        Public Property ExpiredBatchCount As Integer
        Public Property TotalNearExpiryQty As Integer
        Public Property TotalExpiredQty As Integer
        Public Property NearExpiryValue As Decimal
        Public Property ExpiredValue As Decimal
        Public Property NearExpiryAlerts As List(Of ExpiryAlertDto) = New List(Of ExpiryAlertDto)()
        Public Property ExpiredAlerts As List(Of ExpiryAlertDto) = New List(Of ExpiryAlertDto)()
    End Class

End Namespace
