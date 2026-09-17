Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by Accounting to retrieve the FIFO-costed total value of all current stock.
    ''' Handled exclusively by the Inventory module's GetInventoryValuationQueryHandler.
    ''' Returns a snapshot of stock value as of the given date, or Now if no date is provided.
    ''' </summary>
    Public Class GetInventoryValuationQuery
        Implements IRequest(Of GetInventoryValuationResult)

        ''' <summary>
        ''' Optional UTC cut-off date for the valuation snapshot.
        ''' Nothing defaults to the current date/time.
        ''' </summary>
        Public Property AsOfDate As DateTime?

    End Class

End Namespace
