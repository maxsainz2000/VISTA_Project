Namespace Queries

    ''' <summary>
    ''' Response to <see cref="GetProductCostQuery"/>.
    ''' Contains the current FIFO unit cost for a product in the Inventory module.
    ''' </summary>
    Public Class GetProductCostResult

        ''' <summary>Product identifier that was queried.</summary>
        Public Property ProductId As Integer

        ''' <summary>
        ''' Current FIFO unit cost — the UnitCost of the oldest available non-expired stock batch.
        ''' Returns zero when no stock batches exist for the product.
        ''' </summary>
        Public Property FifoUnitCost As Decimal

    End Class

End Namespace
