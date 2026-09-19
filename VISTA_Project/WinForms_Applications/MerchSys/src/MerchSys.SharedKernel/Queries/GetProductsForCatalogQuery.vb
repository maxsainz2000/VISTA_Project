Imports MediatR
Imports System.Collections.Generic

Namespace Queries

    Public Class GetProductsForCatalogQuery
        Implements IRequest(Of IReadOnlyList(Of ProductLookupDto))

        Public Property SearchTerm As String      ' nullable; empty => return all
    End Class

    Public Class ProductLookupDto
        Public Property Id As Integer
        Public Property Name As String
        Public Property Sku As String
    End Class

End Namespace
