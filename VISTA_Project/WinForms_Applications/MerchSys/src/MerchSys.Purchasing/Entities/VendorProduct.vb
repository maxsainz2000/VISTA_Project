Namespace Entities

    ''' <summary>
    ''' Represents an entry in the catalog of products supplied by a specific vendor.
    ''' </summary>
    Public Class VendorProduct
        Inherits MerchSys.SharedKernel.Entities.SoftDeletableEntity

        Public Property VendorId As Integer
        Public Property ProductId As Integer                ' Cross-module ref (no EF FK)
        Public Property ProductName As String               ' Denormalized for display
        Public Property LastUnitCost As Decimal
        Public Property Notes As String

        ' --- Navigation ---
        Public Overridable Property Vendor As Vendor

    End Class

End Namespace
