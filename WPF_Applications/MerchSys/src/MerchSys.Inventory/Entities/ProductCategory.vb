Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Groups products into broad agricultural supply categories (e.g., Fertilizers, Pesticides, Seeds, Animal Feeds).
    ''' </summary>
    Public Class ProductCategory
        Inherits SoftDeletableEntity

        ''' <summary>Display name of this category.</summary>
        Public Property Name As String

        ''' <summary>Optional description of the category.</summary>
        Public Property Description As String

        ''' <summary>Products belonging to this category.</summary>
        Public Property Products As ICollection(Of Product) = New List(Of Product)()

    End Class

End Namespace
