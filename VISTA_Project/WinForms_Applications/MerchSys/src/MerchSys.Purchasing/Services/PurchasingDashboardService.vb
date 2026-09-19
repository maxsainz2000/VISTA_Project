Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class PurchasingDashboardService
        Implements IPurchasingDashboardService

        Private ReadOnly _db As PurchasingDbContext

        Public Sub New(db As PurchasingDbContext)
            _db = db
        End Sub

        Public Async Function GetMonthlyTrendAsync(trendStart As DateTime) As Task(Of List(Of TrendBarItem)) Implements IPurchasingDashboardService.GetMonthlyTrendAsync
            Dim dbResults = Await _db.PurchaseOrders _
                .Where(Function(po) Not po.IsDeleted AndAlso
                                   po.OrderDate >= trendStart AndAlso
                                   po.Status >= PurchaseOrderStatus.Submitted AndAlso
                                   po.Status <= PurchaseOrderStatus.Closed) _
                .GroupBy(Function(po) New With {Key .Year = po.OrderDate.Year, Key .Month = po.OrderDate.Month}) _
                .Select(Function(g) New With {
                    .Year = g.Key.Year,
                    .Month = g.Key.Month,
                    .Total = g.Sum(Function(po) po.TotalAmount)
                }) _
                .ToListAsync()

            Dim dict = dbResults.ToDictionary(Function(x) $"{x.Year}-{x.Month:D2}", Function(x) x.Total)
            
            Dim trendList As New List(Of TrendBarItem)()
            Dim today = DateTime.UtcNow.Date
            For i As Integer = 5 To 0 Step -1
                Dim targetMonth = today.AddMonths(-i)
                Dim key = $"{targetMonth.Year}-{targetMonth.Month:D2}"
                Dim totalAmount = If(dict.ContainsKey(key), dict(key), 0D)
                trendList.Add(New TrendBarItem With {
                    .Month = targetMonth.ToString("MMM yyyy"),
                    .Spend = totalAmount
                })
            Next

            ' Pre-compute bar heights relative to max spend (max = 120px)
            Const MaxBarPx As Double = 120.0
            Dim maxSpend As Decimal = trendList.Select(Function(t) t.Spend).DefaultIfEmpty(1D).Max()
            If maxSpend <= 0 Then maxSpend = 1D
            For Each item In trendList
                item.SpendBarHeight = Math.Max(2.0, CDbl(item.Spend / maxSpend) * MaxBarPx)
            Next

            Return trendList
        End Function

        Public Async Function GetTopVendorsAsync() As Task(Of List(Of TopVendorItem)) Implements IPurchasingDashboardService.GetTopVendorsAsync
            Return Await _db.PurchaseOrders _
                .Where(Function(po) Not po.IsDeleted AndAlso
                                   po.Status >= PurchaseOrderStatus.Submitted AndAlso
                                   po.Status <= PurchaseOrderStatus.Closed AndAlso
                                   Not po.Vendor.IsDeleted) _
                .GroupBy(Function(po) New With {Key po.VendorId, Key .VendorName = po.Vendor.Name}) _
                .Select(Function(g) New TopVendorItem With {
                    .VendorId = g.Key.VendorId,
                    .VendorName = g.Key.VendorName,
                    .POCount = g.Count(),
                    .TotalSpend = g.Sum(Function(po) po.TotalAmount)
                }) _
                .OrderByDescending(Function(x) x.TotalSpend) _
                .Take(5) _
                .ToListAsync()
        End Function

    End Class

End Namespace
