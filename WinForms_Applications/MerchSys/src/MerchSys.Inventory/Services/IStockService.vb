Imports MerchSys.Inventory.Entities

Namespace Services

    Public Interface IStockService
        Function AddStockBatchAsync(productId As Integer, qty As Integer, unitCost As Decimal, receiptDate As DateTime, expiryDate As DateTime?, sourcePOId As Integer?, Optional movementType As MovementType = MovementType.Receipt) As Task(Of StockBatch)
        Function DeductStockFIFOAsync(productId As Integer, quantity As Integer) As Task(Of List(Of FIFODeductionResult))
        Function GetCurrentStockAsync(productId As Integer?) As Task(Of List(Of StockLevelDto))
        Function GetStockBatchesAsync(productId As Integer) As Task(Of List(Of StockBatch))
        Function GetTotalValuationAsync() As Task(Of Decimal)
        Function GetProductsWithCategoriesAsync() As Task(Of ProductAndCategoryData)
    End Interface

    Public Class ProductAndCategoryData
        Public Property Products As List(Of Product)
        Public Property Categories As List(Of ProductCategory)
    End Class

    Public Class FIFODeductionResult
        Public Property BatchId As Integer
        Public Property QuantityDeducted As Integer
        Public Property UnitCost As Decimal
        ''' <summary>Cost of Goods Sold for this deduction: QuantityDeducted × UnitCost.</summary>
        Public Property COGS As Decimal
    End Class

    Public Class StockLevelDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentQuantity As Integer
        Public Property MinimumThreshold As Integer
        Public Property IsBelowThreshold As Boolean
    End Class

End Namespace
