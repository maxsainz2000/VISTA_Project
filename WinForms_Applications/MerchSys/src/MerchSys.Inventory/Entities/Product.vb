Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Represents a product in the Villon Farm Supply catalog.
    ''' Stock levels are derived by summing <see cref="StockBatch.QuantityRemaining"/> across non-expired batches.
    ''' </summary>
    Public Class Product
        Inherits SoftDeletableEntity

        ''' <summary>Display name of the product.</summary>
        Public Property Name As String

        ''' <summary>Stock Keeping Unit code — unique identifier used on shelf labels and purchase orders.</summary>
        Public Property Sku As String

        ''' <summary>Foreign key to <see cref="ProductCategory"/>.</summary>
        Public Property CategoryId As Integer

        ''' <summary>Optional extended description.</summary>
        Public Property Description As String

        ''' <summary>Current retail selling price in Philippine Peso.</summary>
        Public Property RetailPrice As Decimal

        ''' <summary>Unit of measure displayed to staff (e.g., "kg", "bag", "bottle", "pack").</summary>
        Public Property Unit As String

        ''' <summary>True for products that carry an expiry date (e.g., pesticides, seeds, feeds).</summary>
        Public Property HasExpiry As Boolean

        ''' <summary>Quantity threshold that triggers a low-stock alert.</summary>
        Public Property MinimumThreshold As Integer

        ''' <summary>False when the product has been discontinued and should not appear in POS searches.</summary>
        Public Property IsActive As Boolean = True

        ''' <summary>Category this product belongs to.</summary>
        Public Property Category As ProductCategory

        ''' <summary>All stock batches ever received for this product.</summary>
        Public Property StockBatches As ICollection(Of StockBatch) = New List(Of StockBatch)()

        ''' <summary>All shrinkage records logged against this product.</summary>
        Public Property ShrinkageRecords As ICollection(Of ShrinkageRecord) = New List(Of ShrinkageRecord)()

    End Class

End Namespace
