Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Queries

Namespace Services

    Public Class FinancialOverviewService
        Implements IFinancialOverviewService

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _logger As ILogger(Of FinancialOverviewService)

        Public Sub New(db As AccountingDbContext,
                       mediator As IMediator,
                       logger As ILogger(Of FinancialOverviewService))
            _db = db
            _mediator = mediator
            _logger = logger
        End Sub

        Public Async Function GetOverviewAsync() As Task(Of FinancialOverviewDto) Implements IFinancialOverviewService.GetOverviewAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim tomorrow As DateTime = today.AddDays(1)
            Dim monthStart As New DateTime(today.Year, today.Month, 1)
            Dim yearStart As New DateTime(today.Year, 1, 1)

            Dim todayRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= today AndAlso r.RecordDate < tomorrow) _
                .SumAsync(Function(r) r.NetAmount)

            Dim mtdRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= monthStart) _
                .SumAsync(Function(r) r.NetAmount)

            Dim ytdRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= yearStart) _
                .SumAsync(Function(r) r.NetAmount)

            Dim todayTransactions As Integer = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= today AndAlso r.RecordDate < tomorrow) _
                .Select(Function(r) r.SourceTransactionId) _
                .Distinct() _
                .CountAsync()

            Dim monthTransactions As Integer = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= monthStart) _
                .Select(Function(r) r.SourceTransactionId) _
                .Distinct() _
                .CountAsync()

            Dim mtdCOGS As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= monthStart) _
                .SumAsync(Function(r) r.COGS)

            Dim currentGrossMargin As Decimal = 0D
            If mtdRevenue > 0 Then
                currentGrossMargin = Math.Round(((mtdRevenue - mtdCOGS) / mtdRevenue) * 100D, 4)
            End If

            Dim totalAR As Decimal = 0D
            Dim totalAP As Decimal = 0D
            Dim inventoryValue As Decimal = 0D
            Dim overdueARCount As Integer = 0
            Dim overdueAPCount As Integer = 0
            Dim lowStockAlertCount As Integer = 0

            Try
                totalAR = Await _mediator.Send(New GetTotalARQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch total AR via mediator.")
            End Try

            Try
                totalAP = Await _mediator.Send(New GetTotalAPQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch total AP via mediator.")
            End Try

            Try
                Dim valResult = Await _mediator.Send(New GetInventoryValuationQuery())
                inventoryValue = valResult.TotalValue
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch inventory valuation via mediator.")
            End Try

            Try
                overdueARCount = Await _mediator.Send(New GetOverdueARCountQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch overdue AR count via mediator.")
            End Try

            Try
                overdueAPCount = Await _mediator.Send(New GetOverdueAPCountQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch overdue AP count via mediator.")
            End Try

            Try
                lowStockAlertCount = Await _mediator.Send(New GetLowStockAlertCountQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch low stock alert count via mediator.")
            End Try

            Dim trendStart As DateTime = New DateTime(today.Year, today.Month, 1).AddMonths(-5)

            Dim revenueByMonth = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= trendStart) _
                .GroupBy(Function(r) New With {Key .Year = r.RecordDate.Year, Key .Month = r.RecordDate.Month}) _
                .Select(Function(g) New With {
                    Key .Year = g.Key.Year,
                    Key .Month = g.Key.Month,
                    Key .Revenue = g.Sum(Function(r) r.NetAmount),
                    Key .COGS = g.Sum(Function(r) r.COGS),
                    Key .GrossProfit = g.Sum(Function(r) r.GrossProfit)
                }) _
                .OrderBy(Function(g) g.Year).ThenBy(Function(g) g.Month) _
                .ToListAsync()

            Dim monthlyTrend As New List(Of MonthlyTrendDto)()
            For Each entry In revenueByMonth
                Dim margin As Decimal = 0D
                If entry.Revenue > 0 Then
                    margin = Math.Round((entry.GrossProfit / entry.Revenue) * 100D, 4)
                End If
                monthlyTrend.Add(New MonthlyTrendDto With {
                    .Month = New DateTime(entry.Year, entry.Month, 1).ToString("MMM yyyy"),
                    .Revenue = entry.Revenue,
                    .COGS = entry.COGS,
                    .GrossProfit = entry.GrossProfit,
                    .GrossMarginPercent = margin
                })
            Next

            Dim topProductData = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= monthStart) _
                .GroupBy(Function(r) New With {Key .ProductId = r.ProductId, Key .ProductName = r.ProductName}) _
                .Select(Function(g) New With {
                    Key .ProductId = g.Key.ProductId,
                    Key .ProductName = g.Key.ProductName,
                    Key .Revenue = g.Sum(Function(r) r.NetAmount),
                    Key .COGS = g.Sum(Function(r) r.COGS),
                    Key .GrossProfit = g.Sum(Function(r) r.GrossProfit),
                    Key .QuantitySold = g.Sum(Function(r) r.QuantitySold)
                }) _
                .OrderByDescending(Function(g) g.Revenue) _
                .Take(10) _
                .ToListAsync()

            Dim topProducts As New List(Of TopProductDto)()
            For Each item In topProductData
                Dim margin As Decimal = 0D
                If item.Revenue > 0 Then
                    margin = Math.Round((item.GrossProfit / item.Revenue) * 100D, 4)
                End If
                topProducts.Add(New TopProductDto With {
                    .ProductId = item.ProductId,
                    .ProductName = item.ProductName,
                    .Revenue = item.Revenue,
                    .COGS = item.COGS,
                    .GrossProfit = item.GrossProfit,
                    .GrossMarginPercent = margin,
                    .QuantitySold = item.QuantitySold
                })
            Next

            _logger.LogInformation(
                "Financial overview built: TodayRevenue={TodayRevenue}, MTD={MTD}, YTD={YTD}.",
                todayRevenue, mtdRevenue, ytdRevenue)

            Return New FinancialOverviewDto With {
                .TodayRevenue = todayRevenue,
                .MonthToDateRevenue = mtdRevenue,
                .YearToDateRevenue = ytdRevenue,
                .TodayTransactions = todayTransactions,
                .MonthTransactions = monthTransactions,
                .CurrentGrossMargin = currentGrossMargin,
                .TotalAR = totalAR,
                .TotalAP = totalAP,
                .InventoryValue = inventoryValue,
                .MonthlyTrend = monthlyTrend,
                .TopProducts = topProducts,
                .OverdueARCount = overdueARCount,
                .OverdueAPCount = overdueAPCount,
                .LowStockAlertCount = lowStockAlertCount
            }
        End Function

        Public Async Function RefreshSnapshotAsync() As Task(Of FinancialSnapshot) Implements IFinancialOverviewService.RefreshSnapshotAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Dim tomorrow As DateTime = today.AddDays(1)
            Dim monthStart As New DateTime(today.Year, today.Month, 1)
            Dim yearStart As New DateTime(today.Year, 1, 1)

            Dim todayRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= today AndAlso r.RecordDate < tomorrow) _
                .SumAsync(Function(r) r.NetAmount)

            Dim mtdRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= monthStart) _
                .SumAsync(Function(r) r.NetAmount)

            Dim ytdRevenue As Decimal = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= yearStart) _
                .SumAsync(Function(r) r.NetAmount)

            Dim inventoryValue As Decimal = 0D
            Try
                Dim valuationResult = Await _mediator.Send(New GetInventoryValuationQuery())
                inventoryValue = valuationResult.TotalValue
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch inventory valuation for snapshot.")
            End Try

            Dim totalAR As Decimal = 0D
            Dim totalAP As Decimal = 0D

            Try
                totalAR = Await _mediator.Send(New GetTotalARQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch total AR for snapshot.")
            End Try

            Try
                totalAP = Await _mediator.Send(New GetTotalAPQuery())
            Catch ex As Exception
                _logger.LogError(ex, "Failed to fetch total AP for snapshot.")
            End Try

            Dim existing As FinancialSnapshot = Await _db.FinancialSnapshots _
                .FirstOrDefaultAsync(Function(s) s.SnapshotDate = today)

            If existing Is Nothing Then
                existing = New FinancialSnapshot With {.SnapshotDate = today}
                _db.FinancialSnapshots.Add(existing)
            End If

            existing.TodayRevenue = todayRevenue
            existing.MonthToDateRevenue = mtdRevenue
            existing.YearToDateRevenue = ytdRevenue
            existing.InventoryValue = inventoryValue
            existing.TotalAR = totalAR
            existing.TotalAP = totalAP

            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Snapshot refreshed for {Date}: TodayRevenue={TodayRevenue}, InventoryValue={InventoryValue}.",
                today, todayRevenue, inventoryValue)

            Return existing
        End Function

    End Class

End Namespace
