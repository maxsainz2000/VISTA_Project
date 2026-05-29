Imports System.Threading
Imports MediatR
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Helpers
Imports MerchSys.Purchasing.Services.Vat
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    Public Class GoodsReceivingService
        Implements IGoodsReceivingService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _priceChangeService As IPriceChangeService
        Private ReadOnly _vatCalculator As GoodsReceiptVatCalculator
        Private ReadOnly _session As ISessionService
        Private _grListForPO As List(Of GoodsReceipt)

        Public Sub New(db As PurchasingDbContext,
                       mediator As IMediator,
                       priceChangeService As IPriceChangeService,
                       vatCalculator As GoodsReceiptVatCalculator,
                       session As ISessionService)
            _db = db
            _mediator = mediator
            _priceChangeService = priceChangeService
            _vatCalculator = vatCalculator
            _session = session
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

            ' Update/auto-create vendor catalog entries for the received products (Option B timing)
            For Each grLine In receipt.Lines
                If grLine.QuantityReceived > 0 Then
                    Dim entry = Await _db.VendorProducts.
                        IgnoreQueryFilters().
                        FirstOrDefaultAsync(Function(vp) vp.VendorId = po.VendorId AndAlso
                                                           vp.ProductId = grLine.ProductId)
                    If entry IsNot Nothing Then
                        entry.LastUnitCost = grLine.UnitCost
                        entry.IsDeleted = False
                        entry.ModifiedBy = If(_session IsNot Nothing AndAlso Not String.IsNullOrEmpty(_session.CurrentUsername), _session.CurrentUsername, "System")
                        entry.ModifiedAt = DateTime.UtcNow
                    Else
                        ' Auto-create catalog entry if it doesn't exist
                        Dim newEntry As New VendorProduct With {
                            .VendorId = po.VendorId,
                            .ProductId = grLine.ProductId,
                            .ProductName = grLine.ProductName,
                            .LastUnitCost = grLine.UnitCost,
                            .Notes = "Auto-created from Goods Receiving",
                            .CreatedBy = If(_session IsNot Nothing AndAlso Not String.IsNullOrEmpty(_session.CurrentUsername), _session.CurrentUsername, "System"),
                            .CreatedAt = DateTime.UtcNow,
                            .IsDeleted = False
                        }
                        _db.VendorProducts.Add(newEntry)
                    End If
                End If
            Next

            Await _db.SaveChangesAsync()

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
            _grListForPO = New List(Of GoodsReceipt)()
            Dim rfpConnStr = _db.Database.GetConnectionString()
            Using rfpConn As New MySqlConnection(rfpConnStr)
                Await rfpConn.OpenAsync()
                Using rfpCmd = rfpConn.CreateCommand()
                    rfpCmd.CommandText = "SELECT Id, PurchaseOrderId, ReceiptNumber, ReceivedDate, ReceivedBy, Notes, " &
                                         "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Pur_GoodsReceipts WHERE PurchaseOrderId = @poId"
                    rfpCmd.Parameters.Add(New MySqlParameter("@poId", purchaseOrderId))
                    Using rfpReader = rfpCmd.ExecuteReader()
                        While rfpReader.Read()
                            _grListForPO.Add(New GoodsReceipt With {
                                .Id = rfpReader.GetInt32(0),
                                .PurchaseOrderId = rfpReader.GetInt32(1),
                                .ReceiptNumber = rfpReader.GetString(2),
                                .ReceivedDate = rfpReader.GetDateTime(3),
                                .ReceivedBy = If(rfpReader.IsDBNull(4), Nothing, rfpReader.GetString(4)),
                                .Notes = If(rfpReader.IsDBNull(5), Nothing, rfpReader.GetString(5)),
                                .CreatedBy = If(rfpReader.IsDBNull(6), Nothing, rfpReader.GetString(6)),
                                .CreatedAt = rfpReader.GetDateTime(7),
                                .ModifiedBy = If(rfpReader.IsDBNull(8), Nothing, rfpReader.GetString(8)),
                                .ModifiedAt = If(rfpReader.IsDBNull(9), Nothing, CType(rfpReader.GetDateTime(9), DateTime?))
                            })
                        End While
                    End Using
                End Using

                If _grListForPO.Any() Then
                    Dim grIds As String = String.Join(",", _grListForPO.Select(Function(receipt) receipt.Id))
                    Dim grMap = _grListForPO.ToDictionary(Function(receipt) receipt.Id)
                    Using lineCmd = rfpConn.CreateCommand()
                        lineCmd.CommandText = "SELECT Id, GoodsReceiptId, ProductId, ProductName, QuantityOrdered, " &
                                              "QuantityReceived, UnitCost, ExpiryDate, HasDiscrepancy, DiscrepancyNotes, " &
                                              "VatClassification, VatAmount, VatableSales, " &
                                              "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                              $"FROM Pur_GoodsReceiptLines WHERE GoodsReceiptId IN ({grIds})"
                        Using lineReader = lineCmd.ExecuteReader()
                            While lineReader.Read()
                                Dim lineGrId = lineReader.GetInt32(1)
                                Dim grl As New GoodsReceiptLine With {
                                    .Id = lineReader.GetInt32(0),
                                    .GoodsReceiptId = lineGrId,
                                    .ProductId = lineReader.GetInt32(2),
                                    .ProductName = lineReader.GetString(3),
                                    .QuantityOrdered = lineReader.GetInt32(4),
                                    .QuantityReceived = lineReader.GetInt32(5),
                                    .UnitCost = lineReader.GetDecimal(6),
                                    .ExpiryDate = If(lineReader.IsDBNull(7), CType(Nothing, DateTime?), CType(lineReader.GetDateTime(7), DateTime?)),
                                    .HasDiscrepancy = lineReader.GetBoolean(8),
                                    .DiscrepancyNotes = If(lineReader.IsDBNull(9), Nothing, lineReader.GetString(9)),
                                    .VatClassification = CType(lineReader.GetInt32(10), VatTreatment),
                                    .VatAmount = lineReader.GetDecimal(11),
                                    .VatableSales = lineReader.GetDecimal(12)
                                }
                                Dim parentGr As GoodsReceipt = Nothing
                                If grMap.TryGetValue(lineGrId, parentGr) Then
                                    parentGr.Lines.Add(grl)
                                End If
                            End While
                        End Using
                    End Using
                End If
            End Using
            Return _grListForPO
        End Function

    End Class

End Namespace
