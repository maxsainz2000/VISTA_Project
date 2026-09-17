Namespace Entities

    ''' <summary>
    ''' Represents a supplier from whom purchase orders are placed.
    ''' Soft-deletable so that historical POs remain linked even after a vendor is retired.
    ''' </summary>
    Public Class Vendor
        Inherits MerchSys.SharedKernel.Entities.SoftDeletableEntity

        ''' <summary>Registered business name of the vendor.</summary>
        Public Property Name As String

        ''' <summary>Name of the primary contact person at this vendor.</summary>
        Public Property ContactPerson As String

        ''' <summary>Contact phone number for this vendor.</summary>
        Public Property Phone As String

        ''' <summary>Contact email address; Nothing if not provided.</summary>
        Public Property Email As String

        ''' <summary>Full postal or street address of the vendor.</summary>
        Public Property Address As String

        ''' <summary>Typical number of days between placing a PO and receiving delivery.</summary>
        Public Property DefaultLeadTimeDays As Integer

        ''' <summary>Free-text notes about this vendor; Nothing if not provided.</summary>
        Public Property Notes As String

        ' --- Navigation ---

        ''' <summary>All purchase orders placed with this vendor.</summary>
        Public Property PurchaseOrders As ICollection(Of PurchaseOrder) = New List(Of PurchaseOrder)()

    End Class

End Namespace
