Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Represents a historical retail price change log for a product.
    ''' This entity is append-only by design and does not support updates or soft deletes.
    ''' </summary>
    Public Class ProductPriceHistory
        Inherits BaseEntity

        ''' <summary>The product whose price changed.</summary>
        Public Property ProductId As Integer

        ''' <summary>The retail price before the change.</summary>
        Public Property OldPrice As Decimal

        ''' <summary>The retail price after the change.</summary>
        Public Property NewPrice As Decimal

        ''' <summary>UTC timestamp when the price change occurred.</summary>
        Public Property ChangedAt As DateTime

        ''' <summary>The user who performed the price change.</summary>
        Public Property ChangedBy As String

        ''' <summary>Optional user-supplied reason for the price change.</summary>
        Public Property Reason As String

        ''' <summary>Navigation property back to the product.</summary>
        Public Overridable Property Product As Product

    End Class

End Namespace
