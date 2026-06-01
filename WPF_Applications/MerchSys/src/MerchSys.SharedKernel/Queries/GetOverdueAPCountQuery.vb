Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the number of unpaid supplier bills past their due dates.
    ''' Handled by the Purchasing module's GetOverdueAPCountQueryHandler.
    ''' </summary>
    Public Class GetOverdueAPCountQuery
        Implements IRequest(Of Integer)
    End Class

End Namespace
