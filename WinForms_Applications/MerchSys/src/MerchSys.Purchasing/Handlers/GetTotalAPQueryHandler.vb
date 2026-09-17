Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Purchasing.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetTotalAPQuery"/> sent by the Accounting module.
    ''' Sums <c>AccountsPayableEntry.Balance</c> for all unpaid entries to derive total AP.
    ''' </summary>
    Public Class GetTotalAPQueryHandler
        Implements IRequestHandler(Of GetTotalAPQuery, Decimal)

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _logger As ILogger(Of GetTotalAPQueryHandler)

        Public Sub New(db As PurchasingDbContext, logger As ILogger(Of GetTotalAPQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetTotalAPQuery, cancellationToken As CancellationToken) As Task(Of Decimal) Implements IRequestHandler(Of GetTotalAPQuery, Decimal).Handle
            Dim totalAP = Await _db.AccountsPayableEntries _
                .Where(Function(e) Not e.IsPaid) _
                .SumAsync(Function(e) e.Balance, cancellationToken)

            _logger.LogDebug("GetTotalAPQuery: TotalAP={TotalAP}.", totalAP)
            Return totalAP
        End Function

    End Class

End Namespace
