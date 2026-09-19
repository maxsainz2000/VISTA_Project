Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Queries
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Handlers

    ''' <summary>
    ''' Authoritative handler for recording revenue and COGS expenses for POS sales.
    ''' Consolidates VAT and legacy accounting entries.
    ''' Sole writer of <see cref="RevenueRecord"/> rows.
    ''' Idempotency is keyed by <c>(SourceTransactionId, ProductId)</c> for revenue records,
    ''' and by the matching details for COGS <see cref="ExpenseRecord"/> rows.
    ''' </summary>
    Public Class SaleRevenueHandler
        Implements INotificationHandler(Of SaleCompletedWithVatEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SaleRevenueHandler)

        Public Sub New(db As AccountingDbContext, mediator As IMediator, writeContext As IWriteContextScope, logger As ILogger(Of SaleRevenueHandler))
            _db = db
            _mediator = mediator
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedWithVatEvent, cancellationToken As CancellationToken) As Task _
            Implements INotificationHandler(Of SaleCompletedWithVatEvent).Handle

            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("SaleRevenueHandler: Processing TransactionId={TransactionId} ({ItemCount} items).",
                    notification.TransactionId, notification.Items.Count)

                For Each item In notification.Items
                    Dim existing = Await _db.RevenueRecords.FirstOrDefaultAsync(
                        Function(r) r.SourceTransactionId = notification.TransactionId AndAlso
                                    r.ProductId = item.ProductId,
                        cancellationToken)

                    If existing IsNot Nothing Then
                        ' Update VAT columns on record already created by prior publish or migration
                        existing.VatableAmount = item.VatableAmount
                        existing.VatExemptAmount = item.VatExemptAmount
                        existing.ZeroRatedAmount = item.ZeroRatedAmount
                        existing.OutputVat = item.OutputVat
                        existing.InputVat = 0D
                        existing.VatTreatment = item.Treatment
                    Else
                        ' Create full record
                        Dim grossAmount = item.UnitPrice * item.Quantity
                        Dim netAmount = grossAmount - item.DiscountAmount

                        ' Query the exact per-batch FIFO COGS breakdown from Inventory
                        Dim breakdown = Await _mediator.Send(
                            New GetSaleCogsBreakdownQuery() With {
                                .TransactionId = notification.TransactionId,
                                .ProductId = item.ProductId
                            },
                            cancellationToken)

                        Dim cogs As Decimal
                        If breakdown.Lines.Count > 0 Then
                            cogs = breakdown.TotalCogs
                        Else
                            ' Inventory's SaveChanges has not landed yet — fall back to oldest FIFO unit cost
                            ' so the row is at least non-zero. Idempotency guard means the next re-publish
                            ' (if any) will not overwrite this row.
                            Dim costResult = Await _mediator.Send(New GetProductCostQuery() With {.ProductId = item.ProductId}, cancellationToken)
                            cogs = costResult.FifoUnitCost * item.Quantity
                            _logger.LogWarning(
                                "COGS breakdown not yet persisted for Tx {Tx} / Product {Product}; fell back to FIFO-oldest unit cost. " &
                                "Investigate handler ordering if this recurs.",
                                notification.TransactionId, item.ProductId)
                        End If

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
                            .VatAmount = item.OutputVat,
                            .COGS = cogs,
                            .GrossProfit = netAmount - cogs,
                            .VatableAmount = item.VatableAmount,
                            .VatExemptAmount = item.VatExemptAmount,
                            .ZeroRatedAmount = item.ZeroRatedAmount,
                            .OutputVat = item.OutputVat,
                            .InputVat = 0D,
                            .VatTreatment = item.Treatment
                        }
                        _db.RevenueRecords.Add(revenue)

                        ' Create matching COGS ExpenseRecord
                        Dim cogsExpense As New ExpenseRecord With {
                            .RecordDate = notification.TransactionDate,
                            .Category = "COGS",
                            .Description = $"COGS for {item.ProductName} (Tx #{notification.TransactionId})",
                            .Amount = cogs,
                            .SourceModule = "POS",
                            .SourceReferenceId = notification.TransactionId
                        }

                        ' Guard against duplicate COGS ExpenseRecords for same transaction and product
                        Dim existingExpense = Await _db.ExpenseRecords.FirstOrDefaultAsync(
                            Function(e) e.Category = "COGS" AndAlso
                                        e.SourceModule = "POS" AndAlso
                                        e.SourceReferenceId = notification.TransactionId AndAlso
                                        e.Description.Contains($"(Tx #{notification.TransactionId})") AndAlso
                                        e.Description.Contains(item.ProductName),
                            cancellationToken)

                        If existingExpense Is Nothing Then
                            _db.ExpenseRecords.Add(cogsExpense)
                        End If
                    End If
                Next

                Await _db.SaveChangesAsync(cancellationToken)
                _logger.LogInformation("SaleRevenueHandler: Revenue and COGS records saved for TransactionId={TransactionId}.", notification.TransactionId)
            End Using
        End Function

    End Class

End Namespace
