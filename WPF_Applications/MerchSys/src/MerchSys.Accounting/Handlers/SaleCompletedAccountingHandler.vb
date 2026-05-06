Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Events

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="SaleCompletedEvent"/> for the Accounting module.
    ''' Creates one <see cref="RevenueRecord"/> and one COGS <see cref="ExpenseRecord"/> per line item.
    ''' COGS is not carried in the event payload; it is recorded as zero until a dedicated
    ''' per-product cost query is available from Inventory.
    ''' </summary>
    Public Class SaleCompletedAccountingHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of SaleCompletedAccountingHandler)

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of SaleCompletedAccountingHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of SaleCompletedEvent).Handle
            _logger.LogInformation("Recording revenue for SaleCompletedEvent TransactionId={TransactionId} ({ItemCount} items).",
                notification.TransactionId, notification.Items.Count)

            For Each item In notification.Items
                Dim grossAmount = item.UnitPrice * item.Quantity
                Dim netAmount = grossAmount - item.DiscountAmount

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
                    .COGS = 0,
                    .GrossProfit = netAmount
                }

                _db.RevenueRecords.Add(revenue)

                Dim cogsExpense As New ExpenseRecord With {
                    .RecordDate = notification.TransactionDate,
                    .Category = "COGS",
                    .Description = $"COGS for {item.ProductName} (Tx #{notification.TransactionId})",
                    .Amount = 0,
                    .SourceModule = "POS",
                    .SourceReferenceId = notification.TransactionId
                }

                _db.ExpenseRecords.Add(cogsExpense)
            Next

            Await _db.SaveChangesAsync(cancellationToken)

            _logger.LogInformation("Revenue and COGS records saved for TransactionId={TransactionId}.", notification.TransactionId)
        End Function

    End Class

End Namespace
