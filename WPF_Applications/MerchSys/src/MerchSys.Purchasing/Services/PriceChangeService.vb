Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class PriceChangeService
        Implements IPriceChangeService

        Private ReadOnly _db As PurchasingDbContext
        Private _priceAlertList As List(Of PriceChangeAlert)

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
            _priceAlertList = New List(Of PriceChangeAlert)()
            Dim uaConnStr = _db.Database.GetConnectionString()
            Using uaConn As New MySqlConnection(uaConnStr)
                Await uaConn.OpenAsync()
                Using uaCmd = uaConn.CreateCommand()
                    uaCmd.CommandText = "SELECT Id, ProductId, ProductName, VendorId, VendorName, PreviousUnitCost, " &
                                        "NewUnitCost, ChangePercent, ChangeDirection, GoodsReceiptId, IsAcknowledged, AcknowledgedAt, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_PriceChangeAlerts WHERE IsAcknowledged = 0 ORDER BY CreatedAt DESC"
                    Using uaReader = uaCmd.ExecuteReader()
                        While uaReader.Read()
                            _priceAlertList.Add(ReadPriceChangeAlert(uaReader))
                        End While
                    End Using
                End Using
            End Using
            Return _priceAlertList
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
            _priceAlertList = New List(Of PriceChangeAlert)()
            Dim hpConnStr = _db.Database.GetConnectionString()
            Using hpConn As New MySqlConnection(hpConnStr)
                Await hpConn.OpenAsync()
                Using hpCmd = hpConn.CreateCommand()
                    hpCmd.CommandText = "SELECT Id, ProductId, ProductName, VendorId, VendorName, PreviousUnitCost, " &
                                        "NewUnitCost, ChangePercent, ChangeDirection, GoodsReceiptId, IsAcknowledged, AcknowledgedAt, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_PriceChangeAlerts WHERE ProductId = @productId ORDER BY CreatedAt DESC"
                    hpCmd.Parameters.Add(New MySqlParameter("@productId", productId))
                    Using hpReader = hpCmd.ExecuteReader()
                        While hpReader.Read()
                            _priceAlertList.Add(ReadPriceChangeAlert(hpReader))
                        End While
                    End Using
                End Using
            End Using
            Return _priceAlertList
        End Function

        Private Shared Function ReadPriceChangeAlert(r As MySqlDataReader) As PriceChangeAlert
            Return New PriceChangeAlert With {
                .Id = r.GetInt32(0),
                .ProductId = r.GetInt32(1),
                .ProductName = r.GetString(2),
                .VendorId = r.GetInt32(3),
                .VendorName = r.GetString(4),
                .PreviousUnitCost = r.GetDecimal(5),
                .NewUnitCost = r.GetDecimal(6),
                .ChangePercent = r.GetDecimal(7),
                .ChangeDirection = r.GetString(8),
                .GoodsReceiptId = r.GetInt32(9),
                .IsAcknowledged = r.GetBoolean(10),
                .AcknowledgedAt = If(r.IsDBNull(11), CType(Nothing, DateTime?), CType(r.GetDateTime(11), DateTime?)),
                .CreatedBy = If(r.IsDBNull(12), Nothing, r.GetString(12)),
                .CreatedAt = r.GetDateTime(13),
                .ModifiedBy = If(r.IsDBNull(14), Nothing, r.GetString(14)),
                .ModifiedAt = If(r.IsDBNull(15), Nothing, CType(r.GetDateTime(15), DateTime?))
            }
        End Function

    End Class

End Namespace
