Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the POS module to retrieve the product catalog for cart item search.
    ''' Handled exclusively by the Inventory module's GetProductCatalogQueryHandler.
    ''' Pass a SearchTerm to filter by name or SKU; pass Nothing to retrieve all active products.
    ''' </summary>
    Public Class GetProductCatalogQuery
        Implements IRequest(Of GetProductCatalogResult)

        ''' <summary>Optional text filter applied against product name and SKU.</summary>
        Public Property SearchTerm As String

        ''' <summary>Optional product filter. Nothing returns all matching products.</summary>
        Public Property ProductId As Integer?

    End Class

End Namespace
