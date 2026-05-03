Imports MediatR

Namespace Events

    ''' <summary>
    ''' Published by POS when a sales return is processed with <c>shouldRestock = True</c>.
    ''' Consumed by Inventory (to add the returned quantity back into stock).
    ''' </summary>
    Public Class StockReturnedEvent
        Implements INotification

        ''' <summary>ID of the <see cref="MerchSys.POS.Entities.SalesReturn"/> record.</summary>
        Public Property ReturnId As Integer

        ''' <summary>ID of the original POS transaction being reversed.</summary>
        Public Property OriginalTransactionId As Integer

        ''' <summary>UTC date/time the return was processed.</summary>
        Public Property ReturnDate As DateTime

        ''' <summary>Product identifier (matches Inv_ tables).</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalized product name captured at the time of return.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units being added back to inventory.</summary>
        Public Property QuantityReturned As Integer

        ''' <summary>Unit price from the original sale (used as the restock cost basis).</summary>
        Public Property UnitPrice As Decimal

    End Class

End Namespace
