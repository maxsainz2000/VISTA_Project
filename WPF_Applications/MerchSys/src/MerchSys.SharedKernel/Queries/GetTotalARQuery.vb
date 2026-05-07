Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the total outstanding accounts receivable balance.
    ''' Handled by the POS module's GetTotalARQueryHandler, which aggregates CreditAccount.CurrentBalance.
    ''' Used by FinancialOverviewService to populate the AR figure on the dashboard.
    ''' </summary>
    Public Class GetTotalARQuery
        Implements IRequest(Of Decimal)
    End Class

End Namespace
