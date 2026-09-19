Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetTotalARQuery"/> sent by the Accounting module.
    ''' Sums <c>CreditAccount.CurrentBalance</c> across all non-deleted accounts to derive total AR.
    ''' </summary>
    Public Class GetTotalARQueryHandler
        Implements IRequestHandler(Of GetTotalARQuery, Decimal)

        Private ReadOnly _db As POSDbContext
        Private ReadOnly _logger As ILogger(Of GetTotalARQueryHandler)

        Public Sub New(db As POSDbContext, logger As ILogger(Of GetTotalARQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetTotalARQuery, cancellationToken As CancellationToken) As Task(Of Decimal) Implements IRequestHandler(Of GetTotalARQuery, Decimal).Handle
            Dim totalAR = Await _db.CreditAccounts _
                .Where(Function(a) Not a.IsDeleted) _
                .SumAsync(Function(a) a.CurrentBalance, cancellationToken)

            _logger.LogDebug("GetTotalARQuery: TotalAR={TotalAR}.", totalAR)
            Return totalAR
        End Function

    End Class

End Namespace
