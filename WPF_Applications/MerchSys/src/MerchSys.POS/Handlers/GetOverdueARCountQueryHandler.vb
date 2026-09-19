Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetOverdueARCountQuery"/> sent by the Accounting module.
    ''' Returns the number of customer credit accounts that have an outstanding balance (> 0).
    ''' </summary>
    Public Class GetOverdueARCountQueryHandler
        Implements IRequestHandler(Of GetOverdueARCountQuery, Integer)

        Private ReadOnly _db As POSDbContext
        Private ReadOnly _logger As ILogger(Of GetOverdueARCountQueryHandler)

        Public Sub New(db As POSDbContext, logger As ILogger(Of GetOverdueARCountQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetOverdueARCountQuery, cancellationToken As CancellationToken) As Task(Of Integer) Implements IRequestHandler(Of GetOverdueARCountQuery, Integer).Handle
            Dim count = Await _db.CreditAccounts _
                .CountAsync(Function(a) Not a.IsDeleted AndAlso a.CurrentBalance > 0, cancellationToken)

            _logger.LogDebug("GetOverdueARCountQuery: found {Count} overdue credit accounts.", count)
            Return count
        End Function

    End Class

End Namespace
