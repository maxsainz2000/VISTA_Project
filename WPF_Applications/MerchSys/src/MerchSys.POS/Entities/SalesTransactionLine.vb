Namespace Entities

    ''' <summary>
    ''' Represents a single product line within a <see cref="SalesTransaction"/>.
    ''' Unit price and product name are denormalized at the time of sale so that
    ''' historical receipts remain accurate even if the product record changes later.
    ''' </summary>
    Public Class SalesTransactionLine
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the parent <see cref="SalesTransaction"/>.</summary>
        Public Property TransactionId As Integer

        ''' <summary>Cross-module reference to the product (Inventory module ID).</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalized product name captured at the time of sale.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units sold.</summary>
        Public Property Quantity As Integer

        ''' <summary>Retail price per unit at the time of sale.</summary>
        Public Property UnitPrice As Decimal

        ''' <summary>Per-line discount applied to this item.</summary>
        Public Property DiscountAmount As Decimal

        ''' <summary>Computed line total: (Quantity × UnitPrice) − DiscountAmount.</summary>
        Public Property LineTotal As Decimal

        ' --- Navigation ---

        ''' <summary>The parent transaction that owns this line.</summary>
        Public Property Transaction As SalesTransaction

    End Class

End Namespace
