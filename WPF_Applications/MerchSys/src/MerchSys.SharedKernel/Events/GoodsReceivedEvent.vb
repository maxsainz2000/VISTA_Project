Imports MediatR

Namespace Events

    ''' <summary>
    ''' Published by Purchasing when a purchase order is received and goods are confirmed into stock.
    ''' Consumed by Inventory (to update stock levels and FIFO batches) and Accounting (to record AP liability).
    ''' Fires after the receiving clerk saves the goods receipt in the Purchasing module.
    ''' </summary>
    Public Class GoodsReceivedEvent
        Implements INotification

        ''' <summary>ID of the purchase order that was received.</summary>
        Public Property PurchaseOrderId As Integer

        ''' <summary>UTC date/time the goods were physically received.</summary>
        Public Property ReceivedDate As DateTime

        ''' <summary>Line items included in this receipt.</summary>
        Public Property Items As List(Of GoodsReceivedItem)

        Public Sub New()
            Items = New List(Of GoodsReceivedItem)()
        End Sub

        ''' <summary>A single product line within a goods receipt.</summary>
        Public Class GoodsReceivedItem

            ''' <summary>Product identifier (matches Inv_ tables).</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name, denormalized for event consumers.</summary>
            Public Property ProductName As String

            ''' <summary>Number of units received.</summary>
            Public Property QuantityReceived As Integer

            ''' <summary>Purchase cost per unit (used to create FIFO batch).</summary>
            Public Property UnitCost As Decimal

            ''' <summary>Optional expiry date. Nothing for non-perishable products.</summary>
            Public Property ExpiryDate As DateTime?

        End Class

    End Class

End Namespace
