Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Queries
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="SaleCompletedEvent"/> for the Accounting module.
    ''' Creates one <see cref="RevenueRecord"/> and one COGS <see cref="ExpenseRecord"/> per line item.
    ''' COGS is resolved at sale time by querying the Inventory module for the current FIFO unit cost.
    ''' </summary>
    Public Class SaleCompletedAccountingHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SaleCompletedAccountingHandler)

        Public Sub New(db As AccountingDbContext, repository As ISyncableRepository(Of AccountingDbContext), mediator As IMediator, writeContext As IWriteContextScope, logger As ILogger(Of SaleCompletedAccountingHandler))
            _db = db
            _repository = repository
            _mediator = mediator
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of SaleCompletedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Recording revenue for SaleCompletedEvent TransactionId={TransactionId} ({ItemCount} items).",
                    notification.TransactionId, notification.Items.Count)

                For Each item In notification.Items
                    Dim grossAmount = item.UnitPrice * item.Quantity
                    Dim netAmount = grossAmount - item.DiscountAmount

                    Dim costResult = Await _mediator.Send(New GetProductCostQuery() With {.ProductId = item.ProductId}, cancellationToken)
                    Dim cogs = costResult.FifoUnitCost * item.Quantity

                    Dim revenue As New RevenueRecord With {
                        .RecordDate = notification.TransactionDate,
                        .SourceTransactionId = notification.TransactionId,
                        .PaymentMethod = notification.PaymentMethod,
                        .ProductId = item.ProductId,
                        .ProductName = item.ProductName,
                        .QuantitySold = item.Quantity,
                        .GrossAmount = grossAmount,
                        .DiscountAmount = item.DiscountAmount,
                        .NetAmount = netAmount,
                        .VatAmount = 0,
                        .COGS = cogs,
                        .GrossProfit = netAmount - cogs
                    }

                    _db.RevenueRecords.Add(revenue)

                    Dim cogsExpense As New ExpenseRecord With {
                        .RecordDate = notification.TransactionDate,
                        .Category = "COGS",
                        .Description = $"COGS for {item.ProductName} (Tx #{notification.TransactionId})",
                        .Amount = cogs,
                        .SourceModule = "POS",
                        .SourceReferenceId = notification.TransactionId
                    }

                    _db.ExpenseRecords.Add(cogsExpense)
                Next

                Await _repository.SaveChangesWithJournalAsync(cancellationToken)

                _logger.LogInformation("Revenue and COGS records saved for TransactionId={TransactionId}.", notification.TransactionId)
            End Using
        End Function

    End Class

End Namespace
