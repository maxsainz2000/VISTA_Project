Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Class PriceChangeService
        Implements IPriceChangeService

        Private ReadOnly _db As PurchasingDbContext

        Public Sub New(db As PurchasingDbContext)
            _db = db
        End Sub

        Public Async Function DetectChangesAsync(goodsReceiptId As Integer) As Task(Of List(Of PriceChangeAlert)) Implements IPriceChangeService.DetectChangesAsync
            Dim receipt As GoodsReceipt = Await _db.GoodsReceipts.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = goodsReceiptId)

            If receipt Is Nothing Then
                Throw New InvalidOperationException($"Goods receipt {goodsReceiptId} not found.")
            End If

            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                FirstOrDefaultAsync(Function(p) p.Id = receipt.PurchaseOrderId)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {receipt.PurchaseOrderId} not found.")
            End If

            Dim vendor As Vendor = Await _db.Vendors.
                FirstOrDefaultAsync(Function(v) v.Id = po.VendorId)

            Dim vendorName As String = If(vendor IsNot Nothing, vendor.Name, String.Empty)

            Dim alerts As New List(Of PriceChangeAlert)()

            For Each grLine In receipt.Lines
                Dim poLine As PurchaseOrderLine = po.Lines.
                    FirstOrDefault(Function(l) l.ProductId = grLine.ProductId)

                If poLine Is Nothing Then Continue For
                If grLine.UnitCost = poLine.UnitCost Then Continue For

                Dim changePercent As Decimal = ((grLine.UnitCost - poLine.UnitCost) / poLine.UnitCost) * 100D
                Dim direction As String = If(grLine.UnitCost > poLine.UnitCost, "Increase", "Decrease")

                Dim alert As New PriceChangeAlert With {
                    .ProductId = grLine.ProductId,
                    .ProductName = grLine.ProductName,
                    .VendorId = po.VendorId,
                    .VendorName = vendorName,
                    .PreviousUnitCost = poLine.UnitCost,
                    .NewUnitCost = grLine.UnitCost,
                    .ChangePercent = Math.Round(changePercent, 4),
                    .ChangeDirection = direction,
                    .GoodsReceiptId = goodsReceiptId,
                    .IsAcknowledged = False
                }

                alerts.Add(alert)
            Next

            If alerts.Count > 0 Then
                _db.PriceChangeAlerts.AddRange(alerts)
                Await _db.SaveChangesAsync()
            End If

            Return alerts
        End Function

        Public Async Function GetUnacknowledgedAsync() As Task(Of List(Of PriceChangeAlert)) Implements IPriceChangeService.GetUnacknowledgedAsync
            Return Await _db.PriceChangeAlerts.
                Where(Function(a) Not a.IsAcknowledged).
                OrderByDescending(Function(a) a.CreatedAt).
                ToListAsync()
        End Function

        Public Async Function AcknowledgeAsync(alertId As Integer) As Task Implements IPriceChangeService.AcknowledgeAsync
            Dim alert As PriceChangeAlert = Await _db.PriceChangeAlerts.
                FirstOrDefaultAsync(Function(a) a.Id = alertId)

            If alert Is Nothing Then
                Throw New InvalidOperationException($"Price change alert {alertId} not found.")
            End If

            alert.IsAcknowledged = True
            alert.AcknowledgedAt = DateTime.UtcNow
            Await _db.SaveChangesAsync()
        End Function

        Public Async Function GetHistoryForProductAsync(productId As Integer) As Task(Of List(Of PriceChangeAlert)) Implements IPriceChangeService.GetHistoryForProductAsync
            Return Await _db.PriceChangeAlerts.
                Where(Function(a) a.ProductId = productId).
                OrderByDescending(Function(a) a.CreatedAt).
                ToListAsync()
        End Function

    End Class

End Namespace
