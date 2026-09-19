Namespace Services

    Public Interface IIncomeStatementService
        Function GenerateAsync(startDate As DateTime, endDate As DateTime) As Task(Of IncomeStatementDto)
        Function GenerateMonthlyAsync(year As Integer, month As Integer) As Task(Of IncomeStatementDto)
        Function GenerateQuarterlyAsync(year As Integer, quarter As Integer) As Task(Of IncomeStatementDto)
        Function GenerateAnnualAsync(year As Integer) As Task(Of IncomeStatementDto)
        Function GetPerProductMarginsAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of ProductMarginDto))
    End Interface

    Public Class IncomeStatementDto
        Public Property PeriodDescription As String
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime

        ' Revenue Section
        Public Property GrossSales As Decimal
        Public Property SalesReturns As Decimal
        Public Property SalesDiscounts As Decimal
        ''' <summary>GrossSales - SalesReturns - SalesDiscounts</summary>
        Public Property NetSales As Decimal

        ' Cost Section
        Public Property CostOfGoodsSold As Decimal
        ''' <summary>NetSales - CostOfGoodsSold</summary>
        Public Property GrossProfit As Decimal
        ''' <summary>(GrossProfit / NetSales) × 100; 0 when NetSales = 0.</summary>
        Public Property GrossMarginPercent As Decimal

        ' Expense Section
        Public Property OperatingExpenses As Decimal
        Public Property ShrinkageLoss As Decimal

        ' Bottom Line
        ''' <summary>GrossProfit - OperatingExpenses</summary>
        Public Property NetIncome As Decimal
        ''' <summary>(NetIncome / NetSales) × 100; 0 when NetSales = 0.</summary>
        Public Property NetMarginPercent As Decimal
    End Class

    Public Class ProductMarginDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Revenue As Decimal
        Public Property COGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property GrossMarginPercent As Decimal
        Public Property UnitsSold As Integer
    End Class

End Namespace
