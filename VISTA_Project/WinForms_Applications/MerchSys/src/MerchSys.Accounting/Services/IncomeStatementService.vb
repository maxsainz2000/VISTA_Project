Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data

Namespace Services

    Public Class IncomeStatementService
        Implements IIncomeStatementService

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of IncomeStatementService)

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of IncomeStatementService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function GenerateAsync(startDate As DateTime, endDate As DateTime) As Task(Of IncomeStatementDto) Implements IIncomeStatementService.GenerateAsync
            Dim periodEnd As DateTime = endDate.Date.AddDays(1)
            Dim dto As IncomeStatementDto = Await BuildStatementAsync(startDate.Date, periodEnd)
            dto.PeriodDescription = $"{startDate:MMM d, yyyy} – {endDate:MMM d, yyyy}"
            dto.StartDate = startDate.Date
            dto.EndDate = endDate.Date
            Return dto
        End Function

        Public Async Function GenerateMonthlyAsync(year As Integer, month As Integer) As Task(Of IncomeStatementDto) Implements IIncomeStatementService.GenerateMonthlyAsync
            Dim startDate As New DateTime(year, month, 1)
            Dim endDate As DateTime = startDate.AddMonths(1)
            Dim dto As IncomeStatementDto = Await BuildStatementAsync(startDate, endDate)
            dto.PeriodDescription = startDate.ToString("MMMM yyyy")
            dto.StartDate = startDate
            dto.EndDate = endDate.AddDays(-1)
            Return dto
        End Function

        Public Async Function GenerateQuarterlyAsync(year As Integer, quarter As Integer) As Task(Of IncomeStatementDto) Implements IIncomeStatementService.GenerateQuarterlyAsync
            Dim startMonth As Integer = (quarter - 1) * 3 + 1
            Dim startDate As New DateTime(year, startMonth, 1)
            Dim endDate As DateTime = startDate.AddMonths(3)
            Dim dto As IncomeStatementDto = Await BuildStatementAsync(startDate, endDate)
            dto.PeriodDescription = $"Q{quarter} {year}"
            dto.StartDate = startDate
            dto.EndDate = endDate.AddDays(-1)
            Return dto
        End Function

        Public Async Function GenerateAnnualAsync(year As Integer) As Task(Of IncomeStatementDto) Implements IIncomeStatementService.GenerateAnnualAsync
            Dim startDate As New DateTime(year, 1, 1)
            Dim endDate As New DateTime(year + 1, 1, 1)
            Dim dto As IncomeStatementDto = Await BuildStatementAsync(startDate, endDate)
            dto.PeriodDescription = $"FY {year}"
            dto.StartDate = startDate
            dto.EndDate = New DateTime(year, 12, 31)
            Return dto
        End Function

        Public Async Function GetPerProductMarginsAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of ProductMarginDto)) Implements IIncomeStatementService.GetPerProductMarginsAsync
            Dim periodEnd As DateTime = endDate.Date.AddDays(1)

            Dim data = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= startDate.Date AndAlso r.RecordDate < periodEnd) _
                .GroupBy(Function(r) New With {Key .ProductId = r.ProductId, Key .ProductName = r.ProductName}) _
                .Select(Function(g) New With {
                    Key .ProductId = g.Key.ProductId,
                    Key .ProductName = g.Key.ProductName,
                    Key .Revenue = g.Sum(Function(r) r.NetAmount),
                    Key .COGS = g.Sum(Function(r) r.COGS),
                    Key .GrossProfit = g.Sum(Function(r) r.GrossProfit),
                    Key .UnitsSold = g.Sum(Function(r) r.QuantitySold)
                }) _
                .OrderByDescending(Function(g) g.Revenue) _
                .ToListAsync()

            Dim result As New List(Of ProductMarginDto)()
            For Each item In data
                Dim margin As Decimal = 0D
                If item.Revenue > 0 Then
                    margin = Math.Round((item.GrossProfit / item.Revenue) * 100D, 4)
                End If
                result.Add(New ProductMarginDto With {
                    .ProductId = item.ProductId,
                    .ProductName = item.ProductName,
                    .Revenue = item.Revenue,
                    .COGS = item.COGS,
                    .GrossProfit = item.GrossProfit,
                    .GrossMarginPercent = margin,
                    .UnitsSold = item.UnitsSold
                })
            Next

            Return result
        End Function

        Private Async Function BuildStatementAsync(periodStart As DateTime, periodEnd As DateTime) As Task(Of IncomeStatementDto)
            ' Revenue aggregation — gross, discount, and COGS come from per-line RevenueRecords
            Dim revenueData = Await _db.RevenueRecords _
                .Where(Function(r) r.RecordDate >= periodStart AndAlso r.RecordDate < periodEnd) _
                .GroupBy(Function(r) 1) _
                .Select(Function(g) New With {
                    Key .GrossSales = g.Sum(Function(r) r.GrossAmount),
                    Key .SalesDiscounts = g.Sum(Function(r) r.DiscountAmount),
                    Key .COGS = g.Sum(Function(r) r.COGS)
                }) _
                .FirstOrDefaultAsync()

            Dim grossSales As Decimal = If(revenueData IsNot Nothing, revenueData.GrossSales, 0D)
            Dim salesDiscounts As Decimal = If(revenueData IsNot Nothing, revenueData.SalesDiscounts, 0D)
            Dim cogs As Decimal = If(revenueData IsNot Nothing, revenueData.COGS, 0D)

            ' Sales returns: ExpenseRecords with Category = "Return" posted by the POS return handler
            Dim salesReturns As Decimal = Await _db.ExpenseRecords _
                .Where(Function(e) e.RecordDate >= periodStart AndAlso e.RecordDate < periodEnd _
                       AndAlso e.Category = "Return") _
                .SumAsync(Function(e) e.Amount)

            ' Shrinkage: inventory write-offs recorded by the Inventory shrinkage handler
            Dim shrinkageLoss As Decimal = Await _db.ExpenseRecords _
                .Where(Function(e) e.RecordDate >= periodStart AndAlso e.RecordDate < periodEnd _
                       AndAlso e.Category = "Shrinkage") _
                .SumAsync(Function(e) e.Amount)

            ' Operating expenses include shrinkage plus any "Operating" category postings
            Dim otherOpEx As Decimal = Await _db.ExpenseRecords _
                .Where(Function(e) e.RecordDate >= periodStart AndAlso e.RecordDate < periodEnd _
                       AndAlso e.Category = "Operating") _
                .SumAsync(Function(e) e.Amount)

            Dim operatingExpenses As Decimal = shrinkageLoss + otherOpEx

            Dim netSales As Decimal = grossSales - salesReturns - salesDiscounts
            Dim grossProfit As Decimal = netSales - cogs
            Dim netIncome As Decimal = grossProfit - operatingExpenses

            Dim grossMarginPct As Decimal = 0D
            Dim netMarginPct As Decimal = 0D
            If netSales <> 0D Then
                grossMarginPct = Math.Round((grossProfit / netSales) * 100D, 4)
                netMarginPct = Math.Round((netIncome / netSales) * 100D, 4)
            End If

            _logger.LogInformation(
                "Income statement built for {Start:yyyy-MM-dd}–{End:yyyy-MM-dd}: NetSales={NetSales}, GP={GrossProfit}, NI={NetIncome}.",
                periodStart, periodEnd, netSales, grossProfit, netIncome)

            Return New IncomeStatementDto With {
                .GrossSales = grossSales,
                .SalesReturns = salesReturns,
                .SalesDiscounts = salesDiscounts,
                .NetSales = netSales,
                .CostOfGoodsSold = cogs,
                .GrossProfit = grossProfit,
                .GrossMarginPercent = grossMarginPct,
                .ShrinkageLoss = shrinkageLoss,
                .OperatingExpenses = operatingExpenses,
                .NetIncome = netIncome,
                .NetMarginPercent = netMarginPct
            }
        End Function

    End Class

End Namespace
