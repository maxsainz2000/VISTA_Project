Namespace Queries

    ''' <summary>
    ''' Response to <see cref="GetCurrentStockQuery"/>.
    ''' Contains current stock levels and reorder threshold status for each requested product.
    ''' </summary>
    Public Class GetCurrentStockResult

        ''' <summary>Stock level entries for all requested products.</summary>
        Public Property Items As List(Of StockLevel)

        Public Sub New()
            Items = New List(Of StockLevel)()
        End Sub

        ''' <summary>Current stock position for a single product.</summary>
        Public Class StockLevel

            ''' <summary>Product identifier.</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name.</summary>
            Public Property ProductName As String

            ''' <summary>Total units currently on hand across all FIFO batches.</summary>
            Public Property CurrentQuantity As Integer

            ''' <summary>Reorder threshold configured for this product.</summary>
            Public Property MinimumThreshold As Integer

            ''' <summary>True when CurrentQuantity is at or below MinimumThreshold.</summary>
            Public Property IsBelowThreshold As Boolean

        End Class

    End Class

End Namespace
