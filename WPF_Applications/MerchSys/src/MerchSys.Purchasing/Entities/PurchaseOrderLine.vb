Namespace Entities

    ''' <summary>
    ''' Represents a single product line within a <see cref="PurchaseOrder"/>.
    ''' Product name is denormalized so display remains accurate if the Inventory record changes.
    ''' Lines are deleted when the parent PO is deleted, so no soft-delete is needed here.
    ''' </summary>
    Public Class PurchaseOrderLine
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the parent <see cref="PurchaseOrder"/>.</summary>
        Public Property PurchaseOrderId As Integer

        ''' <summary>Cross-module reference to the product (Inventory module ID only; no EF navigation).</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalized product name captured at order time to avoid cross-module queries.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units ordered from the vendor.</summary>
        Public Property QuantityOrdered As Integer

        ''' <summary>Agreed purchase price per unit.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Total cost for this line: QuantityOrdered × UnitCost.</summary>
        Public Property LineTotal As Decimal

        ' --- Navigation ---

        ''' <summary>The parent purchase order that owns this line.</summary>
        Public Property PurchaseOrder As PurchaseOrder

    End Class

End Namespace
