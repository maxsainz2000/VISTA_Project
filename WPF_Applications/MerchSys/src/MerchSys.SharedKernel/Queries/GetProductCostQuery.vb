Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the current FIFO unit cost for a product.
    ''' Handled exclusively by the Inventory module's GetProductCostQueryHandler.
    ''' Used to populate accurate COGS on <see cref="MerchSys.SharedKernel.Events.SaleCompletedEvent"/> handling.
    ''' </summary>
    Public Class GetProductCostQuery
        Implements IRequest(Of GetProductCostResult)

        ''' <summary>Product identifier to look up in the Inventory FIFO batches.</summary>
        Public Property ProductId As Integer

    End Class

End Namespace
