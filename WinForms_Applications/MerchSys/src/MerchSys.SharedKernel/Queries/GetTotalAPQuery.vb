Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the total outstanding accounts payable balance.
    ''' Handled by the Purchasing module's GetTotalAPQueryHandler, which sums AccountsPayableEntry.Balance
    ''' for all unpaid entries.
    ''' Used by FinancialOverviewService to populate the AP figure on the dashboard.
    ''' </summary>
    Public Class GetTotalAPQuery
        Implements IRequest(Of Decimal)
    End Class

End Namespace
