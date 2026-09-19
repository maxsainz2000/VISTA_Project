Namespace Entities

    ''' <summary>
    ''' Records a product return against an existing <see cref="SalesTransaction"/>.
    ''' A reason is always required. When <see cref="IsRestocked"/> is True the returned
    ''' quantity must be added back to inventory (handled by the service layer via MediatR).
    ''' </summary>
    Public Class SalesReturn
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the original <see cref="SalesTransaction"/> being reversed.</summary>
        Public Property OriginalTransactionId As Integer

        ''' <summary>Date and time the return was processed.</summary>
        Public Property ReturnDate As DateTime

        ''' <summary>Cross-module reference to the returned product (Inventory module ID).</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalized product name captured at the time of return.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units returned.</summary>
        Public Property QuantityReturned As Integer

        ''' <summary>Unit price from the original sale, used to calculate the refund.</summary>
        Public Property UnitPrice As Decimal

        ''' <summary>Total amount refunded to the customer: QuantityReturned × UnitPrice.</summary>
        Public Property RefundAmount As Decimal

        ''' <summary>Required explanation for why the item is being returned.</summary>
        Public Property Reason As String

        ''' <summary>True when the returned item has been added back to inventory stock.</summary>
        Public Property IsRestocked As Boolean

        ' --- Navigation ---

        ''' <summary>The original transaction from which this return originates.</summary>
        Public Property OriginalTransaction As SalesTransaction

    End Class

End Namespace
