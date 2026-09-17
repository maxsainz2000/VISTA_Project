Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by Purchasing (reorder engine) to check current stock levels before raising purchase orders.
    ''' Handled exclusively by the Inventory module's GetCurrentStockQueryHandler.
    ''' Pass a specific ProductId to check a single product, or Nothing to retrieve all products.
    ''' </summary>
    Public Class GetCurrentStockQuery
        Implements IRequest(Of GetCurrentStockResult)

        ''' <summary>
        ''' Optional product filter. Nothing returns stock levels for all products.
        ''' </summary>
        Public Property ProductId As Integer?

    End Class

End Namespace
