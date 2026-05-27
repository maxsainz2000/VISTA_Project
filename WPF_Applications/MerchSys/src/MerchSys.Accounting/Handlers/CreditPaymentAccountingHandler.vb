Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="CreditPaymentEvent"/> for the Accounting module.
    ''' Records the AR reduction when a customer pays down their outstanding utang balance.
    ''' </summary>
    Public Class CreditPaymentAccountingHandler
        Implements INotificationHandler(Of CreditPaymentEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of CreditPaymentAccountingHandler)

        Public Sub New(db As AccountingDbContext, repository As ISyncableRepository(Of AccountingDbContext), writeContext As IWriteContextScope, logger As ILogger(Of CreditPaymentAccountingHandler))
            _db = db
            _repository = repository
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As CreditPaymentEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of CreditPaymentEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Recording AR reduction for CreditPaymentEvent CustomerId={CustomerId} Amount={Amount}.",
                    notification.CustomerId, notification.PaymentAmount)

                Dim expense As New ExpenseRecord With {
                    .RecordDate = notification.PaymentDate,
                    .Category = "AR Reduction",
                    .Description = $"Credit payment received from Customer #{notification.CustomerId} via {notification.PaymentMethod}",
                    .Amount = notification.PaymentAmount,
                    .SourceModule = "POS",
                    .SourceReferenceId = notification.CustomerId
                }

                _db.ExpenseRecords.Add(expense)
                Await _repository.SaveChangesWithJournalAsync(cancellationToken)

                _logger.LogInformation("AR reduction recorded for CustomerId={CustomerId}: Amount={Amount}.",
                    notification.CustomerId, notification.PaymentAmount)
            End Using
        End Function

    End Class

End Namespace
