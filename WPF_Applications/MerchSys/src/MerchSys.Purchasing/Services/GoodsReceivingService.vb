Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Helpers
Imports MerchSys.Purchasing.Services.Vat
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class GoodsReceivingService
        Implements IGoodsReceivingService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _priceChangeService As IPriceChangeService
        Private ReadOnly _vatCalculator As GoodsReceiptVatCalculator
        Private ReadOnly _repository As ISyncableRepository(Of PurchasingDbContext)

        Public Sub New(db As PurchasingDbContext,
                       mediator As IMediator,
                       priceChangeService As IPriceChangeService,
                       vatCalculator As GoodsReceiptVatCalculator,
                       repository As ISyncableRepository(Of PurchasingDbContext))
            _db = db
            _mediator = mediator
            _priceChangeService = priceChangeService
            _vatCalculator = vatCalculator
            _repository = repository
        End Sub

        Public Async Function ReceiveGoodsAsync(purchaseOrderId As Integer, lines As List(Of ReceiveGoodsLineDto)) As Task(Of GoodsReceipt) Implements IGoodsReceivingService.ReceiveGoodsAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                FirstOrDefaultAsync(Function(p) p.Id = purchaseOrderId)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {purchaseOrderId} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Submitted Then
                Throw New InvalidOperationException($"Only Submitted purchase orders can receive goods. Current status: {po.Status}.")
            End If

            For Each dto In lines
                If dto.QuantityReceived <> dto.QuantityOrdered AndAlso String.IsNullOrWhiteSpace(dto.DiscrepancyNotes) Then
                    Throw New InvalidOperationException($"DiscrepancyNotes is required for '{dto.ProductName}' because quantity received differs from quantity ordered.")
                End If
            Next

            Dim year As Integer = DateTime.UtcNow.Year
            Dim existingNumbers As List(Of String) = Await _db.GoodsReceipts.
                Select(Function(r) r.ReceiptNumber).
                ToListAsync()
            Dim receiptNumber As String = SequentialNumberGenerator.Generate("GR", year, existingNumbers)

            Dim receipt As New GoodsReceipt With {
                .PurchaseOrderId = purchaseOrderId,
                .ReceiptNumber = receiptNumber,
                .ReceivedDate = DateTime.UtcNow,
                .ReceivedBy = "System"
            }

            For Each dto In lines
                Dim hasDiscrepancy As Boolean = (dto.QuantityReceived <> dto.QuantityOrdered)
                Dim lineTotal As Decimal = CDec(dto.QuantityReceived) * dto.UnitCost
                Dim vatAmt As Decimal = 0D
                Dim vatableSls As Decimal = 0D
                If dto.VatClassification = VatTreatment.Vatable Then
                    ' 12% Philippine VAT rate (NIRC Sec. 106). UnitCost is VAT-inclusive.
                    vatableSls = Math.Round(lineTotal / 1.12D, 2)
                    vatAmt = Math.Round(lineTotal - vatableSls, 2)
                Else
                    vatableSls = lineTotal
                End If
                receipt.Lines.Add(New GoodsReceiptLine With {
                    .ProductId = dto.ProductId,
                    .ProductName = dto.ProductName,
                    .QuantityOrdered = dto.QuantityOrdered,
                    .QuantityReceived = dto.QuantityReceived,
                    .UnitCost = dto.UnitCost,
                    .ExpiryDate = dto.ExpiryDate,
                    .HasDiscrepancy = hasDiscrepancy,
                    .DiscrepancyNotes = dto.DiscrepancyNotes,
                    .VatClassification = dto.VatClassification,
                    .VatAmount = vatAmt,
                    .VatableSales = vatableSls
                })
            Next

            _db.GoodsReceipts.Add(receipt)
            po.Status = PurchaseOrderStatus.Received
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Dim ev As New GoodsReceivedEvent With {
                .PurchaseOrderId = purchaseOrderId,
                .ReceivedDate = receipt.ReceivedDate
            }

            For Each grLine In receipt.Lines
                ev.Items.Add(New GoodsReceivedEvent.GoodsReceivedItem With {
                    .ProductId = grLine.ProductId,
                    .ProductName = grLine.ProductName,
                    .QuantityReceived = grLine.QuantityReceived,
                    .UnitCost = grLine.UnitCost,
                    .ExpiryDate = grLine.ExpiryDate
                })
            Next

            Await _mediator.Publish(ev)

            ' Publish VAT-aware sibling (INFRA-07 / PUR-14 / PUR-15).  Both events fire so legacy
            ' consumers (FIFO costing, stock movement, low-stock alerts) keep receiving
            ' GoodsReceivedEvent while ACC-10 and ACC-11 consume GoodsReceivedWithVatEvent.
            ' PUR-15: per-line VatClassification, VatAmount, and VatableSales are now populated on
            ' each GoodsReceiptLine before SaveChanges, replacing the Option-2 aggregate simplification.
            Dim vatBreakdown = _vatCalculator.Calculate(receipt, receipt.Lines)

            Dim vatEvent As New GoodsReceivedWithVatEvent With {
                .PurchaseOrderId = purchaseOrderId,
                .ReceivedDate = receipt.ReceivedDate,
                .VatableInput = vatBreakdown.VatableInputs,
                .VatExemptInput = vatBreakdown.VatExemptInputs,
                .ZeroRatedInput = vatBreakdown.ZeroRatedInputs,
                .InputVat = vatBreakdown.InputVat
            }

            For Each grLine In receipt.Lines
                Dim unitCostExcl As Decimal = If(grLine.VatClassification = VatTreatment.Vatable,
                    Math.Round(grLine.UnitCost / 1.12D, 4),
                    grLine.UnitCost)
                vatEvent.Items.Add(New GoodsReceivedWithVatEvent.GoodsReceivedItemWithVat With {
                    .ProductId = grLine.ProductId,
                    .ProductName = grLine.ProductName,
                    .QuantityReceived = grLine.QuantityReceived,
                    .UnitCost = Math.Round(unitCostExcl, 2),
                    .ExpiryDate = grLine.ExpiryDate,
                    .Treatment = grLine.VatClassification,
                    .InputVat = grLine.VatAmount
                })
            Next

            Await _mediator.Publish(vatEvent)

            Await _priceChangeService.DetectChangesAsync(receipt.Id)

            Return Await GetReceiptByIdAsync(receipt.Id)
        End Function

        Public Async Function GetReceiptByIdAsync(id As Integer) As Task(Of GoodsReceipt) Implements IGoodsReceivingService.GetReceiptByIdAsync
            Return Await _db.GoodsReceipts.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = id)
        End Function

        Public Async Function GetReceiptsForPOAsync(purchaseOrderId As Integer) As Task(Of List(Of GoodsReceipt)) Implements IGoodsReceivingService.GetReceiptsForPOAsync
            Return Await _db.GoodsReceipts.
                Include(Function(r) r.Lines).
                Where(Function(r) r.PurchaseOrderId = purchaseOrderId).
                ToListAsync()
        End Function

    End Class

End Namespace
