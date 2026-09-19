Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Purchasing.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetOverdueAPCountQuery"/> sent by the Accounting module.
    ''' Returns the number of unpaid accounts payable entries where the due date is in the past.
    ''' </summary>
    Public Class GetOverdueAPCountQueryHandler
        Implements IRequestHandler(Of GetOverdueAPCountQuery, Integer)

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _logger As ILogger(Of GetOverdueAPCountQueryHandler)

        Public Sub New(db As PurchasingDbContext, logger As ILogger(Of GetOverdueAPCountQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetOverdueAPCountQuery, cancellationToken As CancellationToken) As Task(Of Integer) Implements IRequestHandler(Of GetOverdueAPCountQuery, Integer).Handle
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim count = Await _db.AccountsPayableEntries _
                .CountAsync(Function(e) Not e.IsPaid AndAlso e.DueDate.Date < today, cancellationToken)

            _logger.LogDebug("GetOverdueAPCountQuery: found {Count} overdue supplier bills.", count)
            Return count
        End Function

    End Class

End Namespace
