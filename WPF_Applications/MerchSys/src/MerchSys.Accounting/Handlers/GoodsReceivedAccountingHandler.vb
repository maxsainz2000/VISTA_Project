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
    ''' Handles <see cref="GoodsReceivedEvent"/> for the Accounting module.
    ''' Creates a "Purchase" <see cref="ExpenseRecord"/> for each goods receipt line,
    ''' recording the AP liability incurred when stock is received from a vendor.
    ''' </summary>
    Public Class GoodsReceivedAccountingHandler
        Implements INotificationHandler(Of GoodsReceivedEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of GoodsReceivedAccountingHandler)

        Public Sub New(db As AccountingDbContext, repository As ISyncableRepository(Of AccountingDbContext), writeContext As IWriteContextScope, logger As ILogger(Of GoodsReceivedAccountingHandler))
            _db = db
            _repository = repository
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As GoodsReceivedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of GoodsReceivedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Recording AP expense for GoodsReceivedEvent PO={PurchaseOrderId} ({ItemCount} items).",
                    notification.PurchaseOrderId, notification.Items.Count)

                For Each item In notification.Items
                    Dim totalCost = item.UnitCost * item.QuantityReceived

                    Dim expense As New ExpenseRecord With {
                        .RecordDate = notification.ReceivedDate,
                        .Category = "Purchase",
                        .Description = $"Goods received: {item.ProductName} x{item.QuantityReceived} @ {item.UnitCost:C} (PO #{notification.PurchaseOrderId})",
                        .Amount = totalCost,
                        .SourceModule = "Purchasing",
                        .SourceReferenceId = notification.PurchaseOrderId
                    }

                    _db.ExpenseRecords.Add(expense)

                    _logger.LogInformation("AP expense recorded for {ProductName}: Amount={Amount}.", item.ProductName, totalCost)
                Next

                Await _repository.SaveChangesWithJournalAsync(cancellationToken)

                _logger.LogInformation("AP expense records saved for PO={PurchaseOrderId}.", notification.PurchaseOrderId)
            End Using
        End Function

    End Class

End Namespace
