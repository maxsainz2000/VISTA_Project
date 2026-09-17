Namespace Entities

    ''' <summary>
    ''' Represents a summarised financial period (daily, weekly, monthly, quarterly, or annual).
    ''' Aggregates revenue, COGS, and expenses so the owner can see profitability at a glance.
    ''' </summary>
    Public Class FinancialPeriod
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Granularity of this period: "Daily", "Weekly", "Monthly", "Quarterly", or "Annual".</summary>
        Public Property PeriodType As String

        ''' <summary>Inclusive start of the period.</summary>
        Public Property StartDate As DateTime

        ''' <summary>Inclusive end of the period.</summary>
        Public Property EndDate As DateTime

        ''' <summary>Total net sales revenue recognised in this period.</summary>
        Public Property TotalRevenue As Decimal

        ''' <summary>Total cost of goods sold (FIFO-costed) for this period.</summary>
        Public Property TotalCOGS As Decimal

        ''' <summary>Gross profit: TotalRevenue minus TotalCOGS.</summary>
        Public Property GrossProfit As Decimal

        ''' <summary>Gross margin expressed as a percentage: (GrossProfit / TotalRevenue) × 100.</summary>
        Public Property GrossMarginPercent As Decimal

        ''' <summary>Total operating expenses (shrinkage, AP payments, etc.) for this period.</summary>
        Public Property TotalExpenses As Decimal

        ''' <summary>Net income: GrossProfit minus TotalExpenses.</summary>
        Public Property NetIncome As Decimal

        ''' <summary>When True the period has been finalised and no further postings are accepted.</summary>
        Public Property IsClosed As Boolean

    End Class

End Namespace
