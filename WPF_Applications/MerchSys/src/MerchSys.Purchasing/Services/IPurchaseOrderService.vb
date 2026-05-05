Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class CreatePOLineDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityOrdered As Integer
        Public Property UnitCost As Decimal
    End Class

    Public Interface IPurchaseOrderService
        Function CreateDraftAsync(vendorId As Integer,
                                  lines As List(Of CreatePOLineDto),
                                  Optional notes As String = Nothing,
                                  Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder)
        Function GetByIdAsync(id As Integer) As Task(Of PurchaseOrder)
        Function GetAllAsync(Optional status As PurchaseOrderStatus? = Nothing) As Task(Of List(Of PurchaseOrder))
        Function UpdateDraftAsync(id As Integer,
                                  lines As List(Of CreatePOLineDto),
                                  Optional notes As String = Nothing,
                                  Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder)
        Function SubmitAsync(id As Integer) As Task(Of PurchaseOrder)
        Function MarkReceivedAsync(id As Integer) As Task(Of PurchaseOrder)
        Function VerifyAsync(id As Integer) As Task(Of PurchaseOrder)
        Function CloseAsync(id As Integer) As Task(Of PurchaseOrder)
        Function DeleteDraftAsync(id As Integer) As Task(Of Boolean)
    End Interface

End Namespace
