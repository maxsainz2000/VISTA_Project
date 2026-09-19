Namespace Queries

    ''' <summary>
    ''' Response to <see cref="GetProductCatalogQuery"/>.
    ''' Contains product catalog entries with pricing and current stock levels.
    ''' </summary>
    Public Class GetProductCatalogResult

        ''' <summary>Product catalog entries matching the query criteria.</summary>
        Public Property Items As List(Of ProductCatalogItem)

        Public Sub New()
            Items = New List(Of ProductCatalogItem)()
        End Sub

        ''' <summary>A single product available for sale at the POS.</summary>
        Public Class ProductCatalogItem

            ''' <summary>Product identifier (maps to Inventory module's product ID).</summary>
            Public Property ProductId As Integer

            ''' <summary>Display name of the product.</summary>
            Public Property ProductName As String

            ''' <summary>Stock-keeping unit code.</summary>
            Public Property Sku As String

            ''' <summary>Current selling price per unit.</summary>
            Public Property UnitPrice As Decimal

            ''' <summary>Units currently on hand.</summary>
            Public Property AvailableStock As Integer

            ''' <summary>True when AvailableStock is at or below the reorder threshold.</summary>
            Public Property IsLowStock As Boolean

        End Class

    End Class

End Namespace
