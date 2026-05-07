Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the number of active low-stock alerts.
    ''' Handled by the Inventory module's GetLowStockAlertCountQueryHandler.
    ''' Used by FinancialOverviewService to populate the low-stock count on the dashboard.
    ''' </summary>
    Public Class GetLowStockAlertCountQuery
        Implements IRequest(Of Integer)
    End Class

End Namespace
