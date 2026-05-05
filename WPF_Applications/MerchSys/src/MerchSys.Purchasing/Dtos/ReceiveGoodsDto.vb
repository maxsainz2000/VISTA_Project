Namespace Dtos

    Public Class ReceiveGoodsLineDto

        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityOrdered As Integer
        Public Property QuantityReceived As Integer

        ''' <summary>Actual landed cost per unit at the time of receipt; may differ from the PO agreed cost.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Optional expiry date; Nothing for non-perishable products.</summary>
        Public Property ExpiryDate As DateTime?

        ''' <summary>Required when QuantityReceived differs from QuantityOrdered.</summary>
        Public Property DiscrepancyNotes As String

    End Class

End Namespace
