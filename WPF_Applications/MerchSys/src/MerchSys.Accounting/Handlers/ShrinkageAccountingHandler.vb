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
    ''' Handles <see cref="ShrinkageRecordedEvent"/> for the Accounting module.
    ''' Creates a "Shrinkage" <see cref="ExpenseRecord"/> for the inventory write-off value.
    ''' </summary>
    Public Class ShrinkageAccountingHandler
        Implements INotificationHandler(Of ShrinkageRecordedEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of ShrinkageAccountingHandler)

        Public Sub New(db As AccountingDbContext, repository As ISyncableRepository(Of AccountingDbContext), writeContext As IWriteContextScope, logger As ILogger(Of ShrinkageAccountingHandler))
            _db = db
            _repository = repository
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As ShrinkageRecordedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of ShrinkageRecordedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Recording shrinkage expense for ProductId={ProductId} ({ProductName}): Qty={Qty}, TotalValue={Value}.",
                    notification.ProductId, notification.ProductName, notification.QuantityLost, notification.TotalValue)

                Dim expense As New ExpenseRecord With {
                    .RecordDate = notification.RecordedDate,
                    .Category = "Shrinkage",
                    .Description = $"Shrinkage ({notification.Reason}): {notification.ProductName} x{notification.QuantityLost} @ {notification.UnitCost:C}",
                    .Amount = notification.TotalValue,
                    .SourceModule = "Inventory",
                    .SourceReferenceId = notification.ProductId
                }

                _db.ExpenseRecords.Add(expense)
                Await _repository.SaveChangesWithJournalAsync(cancellationToken)

                _logger.LogInformation("Shrinkage expense recorded for {ProductName}: Amount={Amount}.",
                    notification.ProductName, notification.TotalValue)
            End Using
        End Function

    End Class

End Namespace
