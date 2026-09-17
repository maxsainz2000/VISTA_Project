Namespace Queries

    ''' <summary>
    ''' Response to <see cref="GetInventoryValuationQuery"/>.
    ''' Contains the aggregate FIFO inventory value and a per-product breakdown.
    ''' </summary>
    Public Class GetInventoryValuationResult

        ''' <summary>Sum of TotalValue across all products.</summary>
        Public Property TotalValue As Decimal

        ''' <summary>Per-product valuation details.</summary>
        Public Property Items As List(Of ProductValuation)

        Public Sub New()
            Items = New List(Of ProductValuation)()
        End Sub

        ''' <summary>FIFO valuation for a single product.</summary>
        Public Class ProductValuation

            ''' <summary>Product identifier.</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name.</summary>
            Public Property ProductName As String

            ''' <summary>Total units currently on hand.</summary>
            Public Property TotalQuantity As Integer

            ''' <summary>Total FIFO cost value of current on-hand stock.</summary>
            Public Property TotalValue As Decimal

        End Class

    End Class

End Namespace
