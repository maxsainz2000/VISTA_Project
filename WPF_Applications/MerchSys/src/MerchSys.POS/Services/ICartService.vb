Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Paging

Namespace Services

    Public Class CartLineDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountAmount As Decimal
        Public Property LineTotal As Decimal
    End Class

    Public Class CartDto
        Public Property CartId As Guid
        Public Property Lines As List(Of CartLineDto) = New List(Of CartLineDto)()
        Public Property SubTotal As Decimal
        Public Property DiscountTotal As Decimal
        Public Property VatAmount As Decimal
        Public Property GrandTotal As Decimal
    End Class

    Public Interface ICartService
        Function CreateCartAsync() As Task(Of CartDto)
        Function GetCartAsync(cartId As Guid) As Task(Of CartDto)
        Function AddLineAsync(cartId As Guid, productId As Integer, productName As String, quantity As Integer, unitPrice As Decimal) As Task(Of CartDto)
        Function UpdateLineQuantityAsync(cartId As Guid, lineIndex As Integer, newQuantity As Integer) As Task(Of CartDto)
        Function RemoveLineAsync(cartId As Guid, lineIndex As Integer) As Task(Of CartDto)
        Function ApplyLineDiscountAsync(cartId As Guid, lineIndex As Integer, discountAmount As Decimal) As Task(Of CartDto)
        Function FinalizeAsync(cartId As Guid, paymentMethod As PaymentMethod, amountTendered As Decimal, Optional customerId As Integer? = Nothing) As Task(Of SalesTransaction)
        Function VoidTransactionAsync(transactionId As Integer, reason As String) As Task
        Function GetTransactionHistoryAsync(Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of SalesTransaction))

        ''' <summary>
        ''' Keyset-paged transaction history (INFRA-34). Returns one page (newest first) plus the
        ''' cursor for the next page. Pass the prior page's NextCursor back in <paramref name="request"/>
        ''' to load more. The optional date range in the request bounds the result set.
        ''' </summary>
        Function GetTransactionHistoryPageAsync(request As PageRequest) As Task(Of PagedResult(Of SalesTransaction))
    End Interface

End Namespace
