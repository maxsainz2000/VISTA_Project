Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class SalesSummaryService
        Implements ISalesSummaryService

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of SalesSummaryService)

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of SalesSummaryService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function GetDailySummaryAsync(targetDate As DateTime) As Task(Of AccountingSalesSummaryDto) Implements ISalesSummaryService.GetDailySummaryAsync
            Dim periodStart As DateTime = targetDate.Date
            Dim periodEnd As DateTime = periodStart.AddDays(1)
            Dim dto As AccountingSalesSummaryDto = Await BuildSummaryAsync(periodStart, periodEnd, includeDailyBreakdown:=False)
            dto.PeriodDescription = targetDate.ToString("MMMM d, yyyy")
            dto.StartDate = periodStart
            dto.EndDate = periodStart
            Return dto
        End Function

        Public Async Function GetWeeklySummaryAsync(weekStart As DateTime) As Task(Of AccountingSalesSummaryDto) Implements ISalesSummaryService.GetWeeklySummaryAsync
            Dim periodStart As DateTime = weekStart.Date
            Dim periodEnd As DateTime = periodStart.AddDays(7)
            Dim dto As AccountingSalesSummaryDto = Await BuildSummaryAsync(periodStart, periodEnd, includeDailyBreakdown:=True)
            dto.PeriodDescription = $"Week of {periodStart:MMM d} – {periodEnd.AddDays(-1):MMM d, yyyy}"
            dto.StartDate = periodStart
            dto.EndDate = periodEnd.AddDays(-1)
            Return dto
        End Function

        Public Async Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of AccountingSalesSummaryDto) Implements ISalesSummaryService.GetMonthlySummaryAsync
            Dim periodStart As New DateTime(year, month, 1)
            Dim periodEnd As DateTime = periodStart.AddMonths(1)
            Dim dto As AccountingSalesSummaryDto = Await BuildSummaryAsync(periodStart, periodEnd, includeDailyBreakdown:=True)
            dto.PeriodDescription = periodStart.ToString("MMMM yyyy")
            dto.StartDate = periodStart
            dto.EndDate = periodEnd.AddDays(-1)
            Return dto
        End Function

        Public Async Function GetPaymentMethodTrendAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of PaymentTrendDto)) Implements ISalesSummaryService.GetPaymentMethodTrendAsync
            Dim periodStart As DateTime = startDate.Date
            Dim periodEnd As DateTime = endDate.Date.AddDays(1)

            Dim rows = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= periodStart AndAlso r.RecordDate < periodEnd) _
                .GroupBy(Function(r) New With {Key .RecordDate = r.RecordDate, Key .Method = r.PaymentMethod}) _
                .Select(Function(g) New With {
                    Key .RecordDate = g.Key.RecordDate,
                    Key .Method = g.Key.Method,
                    Key .Total = g.Sum(Function(r) r.NetAmount)
                }) _
                .OrderBy(Function(x) x.RecordDate) _
                .ToListAsync()

            ' Pivot by date — one PaymentTrendDto per distinct date
            Dim byDate = rows.GroupBy(Function(x) x.RecordDate)
            Dim result As New List(Of PaymentTrendDto)()

            For Each group In byDate
                Dim trend As New PaymentTrendDto With {.[Date] = group.Key}
                For Each entry In group
                    Select Case entry.Method
                        Case PaymentMethod.Cash
                            trend.CashTotal = entry.Total
                        Case PaymentMethod.GCash
                            trend.GCashTotal = entry.Total
                        Case PaymentMethod.BankTransfer
                            trend.BankTransferTotal = entry.Total
                        Case PaymentMethod.Credit
                            trend.CreditTotal = entry.Total
                    End Select
                Next
                result.Add(trend)
            Next

            _logger.LogInformation(
                "Payment method trend built for {Start:yyyy-MM-dd}–{End:yyyy-MM-dd}: {Days} day(s).",
                periodStart, periodEnd, result.Count)

            Return result
        End Function

        Private Async Function BuildSummaryAsync(periodStart As DateTime, periodEnd As DateTime, includeDailyBreakdown As Boolean) As Task(Of AccountingSalesSummaryDto)
            ' Aggregate revenue totals and per-payment-method breakdown in a single pass
            Dim methodGroups = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= periodStart AndAlso r.RecordDate < periodEnd) _
                .GroupBy(Function(r) r.PaymentMethod) _
                .Select(Function(g) New With {
                    Key .Method = g.Key,
                    Key .TxCount = g.Select(Function(r) r.SourceTransactionId).Distinct().Count(),
                    Key .GrossAmount = g.Sum(Function(r) r.GrossAmount),
                    Key .DiscountAmount = g.Sum(Function(r) r.DiscountAmount),
                    Key .NetAmount = g.Sum(Function(r) r.NetAmount)
                }) _
                .ToListAsync()

            Dim totalGross As Decimal = methodGroups.Sum(Function(m) m.GrossAmount)
            Dim totalDiscounts As Decimal = methodGroups.Sum(Function(m) m.DiscountAmount)
            Dim totalNetFromRevenue As Decimal = methodGroups.Sum(Function(m) m.NetAmount)

            ' Returns are posted to ExpenseRecords by the return handler (same pattern as IncomeStatementService)
            Dim totalReturns As Decimal = Await _db.ExpenseRecords _
                .Where(Function(e) e.RecordDate >= periodStart AndAlso e.RecordDate < periodEnd _
                       AndAlso e.Category = "Return") _
                .SumAsync(Function(e) e.Amount)

            Dim totalNetSales As Decimal = totalNetFromRevenue - totalReturns

            ' Distinct transaction count across all payment methods
            Dim totalTxCount As Integer = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= periodStart AndAlso r.RecordDate < periodEnd) _
                .Select(Function(r) r.SourceTransactionId) _
                .Distinct() _
                .CountAsync()

            ' Build payment breakdown
            Dim paymentBreakdown As New List(Of PaymentBreakdownDto)()
            For Each mg In methodGroups.OrderByDescending(Function(m) m.NetAmount)
                Dim pct As Decimal = 0D
                If totalNetFromRevenue > 0D Then
                    pct = Math.Round((mg.NetAmount / totalNetFromRevenue) * 100D, 4)
                End If
                paymentBreakdown.Add(New PaymentBreakdownDto With {
                    .PaymentMethod = mg.Method.ToString(),
                    .TransactionCount = mg.TxCount,
                    .GrossAmount = mg.GrossAmount,
                    .NetAmount = mg.NetAmount,
                    .Percentage = pct
                })
            Next

            ' Build daily breakdown when requested (weekly/monthly views)
            Dim dailyBreakdown As New List(Of DailySalesDto)()
            If includeDailyBreakdown Then
                Dim dailyRevenue = Await _db.RevenueRecords _
                    .Where(Function(r) r.RecordDate >= periodStart AndAlso r.RecordDate < periodEnd) _
                    .GroupBy(Function(r) r.RecordDate) _
                    .Select(Function(g) New With {
                        Key .Date = g.Key,
                        Key .Gross = g.Sum(Function(r) r.GrossAmount),
                        Key .Discounts = g.Sum(Function(r) r.DiscountAmount),
                        Key .Net = g.Sum(Function(r) r.NetAmount),
                        Key .TxCount = g.Select(Function(r) r.SourceTransactionId).Distinct().Count()
                    }) _
                    .OrderBy(Function(g) g.Date) _
                    .ToListAsync()

                Dim dailyReturns = Await _db.ExpenseRecords _
                    .Where(Function(e) e.RecordDate >= periodStart AndAlso e.RecordDate < periodEnd _
                           AndAlso e.Category = "Return") _
                    .GroupBy(Function(e) e.RecordDate) _
                    .Select(Function(g) New With {Key .RecordDate = g.Key, Key .Total = g.Sum(Function(e) e.Amount)}) _
                    .ToDictionaryAsync(Function(x) x.RecordDate, Function(x) x.Total)

                For Each dailyRow In dailyRevenue
                    Dim dayReturns As Decimal = 0D
                    dailyReturns.TryGetValue(dailyRow.Date, dayReturns)
                    dailyBreakdown.Add(New DailySalesDto With {
                        .SalesDate = dailyRow.Date,
                        .GrossSales = dailyRow.Gross,
                        .Discounts = dailyRow.Discounts,
                        .Returns = dayReturns,
                        .NetSales = dailyRow.Net - dayReturns,
                        .TransactionCount = dailyRow.TxCount
                    })
                Next
            End If

            _logger.LogInformation(
                "Sales summary built for {Start:yyyy-MM-dd}–{End:yyyy-MM-dd}: GrossSales={Gross}, NetSales={Net}, Txns={Txns}.",
                periodStart, periodEnd, totalGross, totalNetSales, totalTxCount)

            Return New AccountingSalesSummaryDto With {
                .TotalGrossSales = totalGross,
                .TotalDiscounts = totalDiscounts,
                .TotalReturns = totalReturns,
                .TotalNetSales = totalNetSales,
                .TransactionCount = totalTxCount,
                .PaymentBreakdown = paymentBreakdown,
                .DailyBreakdown = dailyBreakdown
            }
        End Function

    End Class

End Namespace
