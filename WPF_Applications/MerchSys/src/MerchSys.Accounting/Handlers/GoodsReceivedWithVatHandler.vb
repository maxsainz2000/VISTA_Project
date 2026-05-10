Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GoodsReceivedWithVatEvent"/> (published by Purchasing/POS-14).
    ''' Populates or updates BIR three-bucket VAT columns on <see cref="ExpenseRecord"/> rows.
    ''' <c>InputVat</c> is populated; <c>OutputVat = 0</c> for all expense lines.
    ''' Idempotency key: <c>(SourceModule="Purchasing", SourceReferenceId=PurchaseOrderId, ProductName)</c> —
    ''' re-publishing both <c>GoodsReceivedEvent</c> and <c>GoodsReceivedWithVatEvent</c>
    ''' for the same receipt during the migration window must not create duplicate rows.
    ''' </summary>
    Public Class GoodsReceivedWithVatHandler
        Implements INotificationHandler(Of GoodsReceivedWithVatEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of GoodsReceivedWithVatHandler)

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of GoodsReceivedWithVatHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(notification As GoodsReceivedWithVatEvent, cancellationToken As CancellationToken) As Task _
            Implements INotificationHandler(Of GoodsReceivedWithVatEvent).Handle

            _logger.LogInformation("GoodsReceivedWithVatHandler: PO={PurchaseOrderId} ({ItemCount} items).",
                notification.PurchaseOrderId, notification.Items.Count)

            For Each item In notification.Items
                Dim totalCost = item.UnitCost * item.QuantityReceived

                ' Three-bucket amounts for this line
                Dim vatableAmt = If(item.Treatment = VatTreatment.Vatable, totalCost, 0D)
                Dim exemptAmt = If(item.Treatment = VatTreatment.Exempt, totalCost, 0D)
                Dim zeroAmt = If(item.Treatment = VatTreatment.ZeroRated, totalCost, 0D)

                ' Try to find the expense record created by the legacy GoodsReceivedAccountingHandler
                Dim poId As Integer = notification.PurchaseOrderId
                Dim existing = Await _db.ExpenseRecords.
                    FirstOrDefaultAsync(
                        Function(e) e.SourceModule = "Purchasing" AndAlso
                                    e.SourceReferenceId.HasValue AndAlso
                                    e.SourceReferenceId.Value = poId AndAlso
                                    e.Description.Contains(item.ProductName),
                        cancellationToken)

                If existing IsNot Nothing Then
                    ' Update VAT columns on the existing record
                    existing.VatableAmount = vatableAmt
                    existing.VatExemptAmount = exemptAmt
                    existing.ZeroRatedAmount = zeroAmt
                    existing.OutputVat = 0D
                    existing.InputVat = item.InputVat
                    existing.VatTreatment = item.Treatment
                Else
                    ' Create full record (VAT event arrived before legacy event, or running VAT-only mode)
                    Dim expense As New ExpenseRecord With {
                        .RecordDate = notification.ReceivedDate,
                        .Category = "Purchase",
                        .Description = $"Goods received: {item.ProductName} x{item.QuantityReceived} @ {item.UnitCost:C} (PO #{notification.PurchaseOrderId})",
                        .Amount = totalCost,
                        .SourceModule = "Purchasing",
                        .SourceReferenceId = notification.PurchaseOrderId,
                        .VatableAmount = vatableAmt,
                        .VatExemptAmount = exemptAmt,
                        .ZeroRatedAmount = zeroAmt,
                        .OutputVat = 0D,
                        .InputVat = item.InputVat,
                        .VatTreatment = item.Treatment
                    }
                    _db.ExpenseRecords.Add(expense)
                End If
            Next

            Await _db.SaveChangesAsync(cancellationToken)
            _logger.LogInformation("GoodsReceivedWithVatHandler: VAT columns saved for PO={PurchaseOrderId}.", notification.PurchaseOrderId)
        End Function

    End Class

End Namespace
