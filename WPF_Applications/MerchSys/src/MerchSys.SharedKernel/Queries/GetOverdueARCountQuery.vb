Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the number of customer credit accounts with outstanding balances.
    ''' Handled by the POS module's GetOverdueARCountQueryHandler.
    ''' </summary>
    Public Class GetOverdueARCountQuery
        Implements IRequest(Of Integer)
    End Class

End Namespace
