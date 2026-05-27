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
    ''' Handles <see cref="SaleCompletedWithVatEvent"/> (published by POS-14).
    ''' Populates or updates BIR three-bucket VAT columns
    ''' (<c>VatableAmount</c>, <c>VatExemptAmount</c>, <c>ZeroRatedAmount</c>, <c>OutputVat</c>,
    ''' <c>VatTreatment</c>) on the <see cref="RevenueRecord"/> rows created by
    ''' the legacy <see cref="SaleCompletedAccountingHandler"/>.
    ''' Idempotency key: <c>(SourceTransactionId, ProductId)</c> — re-publishing both
    ''' <c>SaleCompletedEvent</c> and <c>SaleCompletedWithVatEvent</c> for the same
    ''' transaction during the POS-14 migration window must not create duplicate rows.
    ''' </summary>
    Public Class SaleCompletedWithVatHandler
        Implements INotificationHandler(Of SaleCompletedWithVatEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SaleCompletedWithVatHandler)

        Public Sub New(db As AccountingDbContext, repository As ISyncableRepository(Of AccountingDbContext), mediator As IMediator, writeContext As IWriteContextScope, logger As ILogger(Of SaleCompletedWithVatHandler))
            _db = db
            _repository = repository
            _mediator = mediator
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedWithVatEvent, cancellationToken As CancellationToken) As Task _
            Implements INotificationHandler(Of SaleCompletedWithVatEvent).Handle

            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("SaleCompletedWithVatHandler: TransactionId={TransactionId} ({ItemCount} items).",
                    notification.TransactionId, notification.Items.Count)

                For Each item In notification.Items
                    Dim existing = Await _db.RevenueRecords.
                        FirstOrDefaultAsync(
                            Function(r) r.SourceTransactionId = notification.TransactionId AndAlso
                                        r.ProductId = item.ProductId,
                            cancellationToken)

                    If existing IsNot Nothing Then
                        ' Update VAT columns on record already created by SaleCompletedAccountingHandler
                        existing.VatableAmount = item.VatableAmount
                        existing.VatExemptAmount = item.VatExemptAmount
                        existing.ZeroRatedAmount = item.ZeroRatedAmount
                        existing.OutputVat = item.OutputVat
                        existing.InputVat = 0D
                        existing.VatTreatment = item.Treatment
                    Else
                        ' Create full record (VAT event arrived before legacy event, or running VAT-only mode)
                        Dim grossAmount = item.UnitPrice * item.Quantity
                        Dim netAmount = grossAmount - item.DiscountAmount

                        Dim costResult = Await _mediator.Send(
                            New GetProductCostQuery() With {.ProductId = item.ProductId},
                            cancellationToken)
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
                    End If
                Next

                Await _repository.SaveChangesWithJournalAsync(cancellationToken)
                _logger.LogInformation("SaleCompletedWithVatHandler: VAT columns saved for TransactionId={TransactionId}.", notification.TransactionId)
            End Using
        End Function

    End Class

End Namespace
