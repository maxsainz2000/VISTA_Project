Namespace Entities

    ''' <summary>
    ''' Point-in-time cache of key financial indicators, refreshed periodically by the
    ''' Accounting service.  Provides instant KPI reads without re-aggregating all records
    ''' on every dashboard load.
    ''' </summary>
    Public Class FinancialSnapshot
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Timestamp at which this snapshot was captured.</summary>
        Public Property SnapshotDate As DateTime

        ''' <summary>Total accounts receivable outstanding (credit/utang balances from POS).</summary>
        Public Property TotalAR As Decimal

        ''' <summary>Total accounts payable outstanding (unpaid PO balances from Purchasing).</summary>
        Public Property TotalAP As Decimal

        ''' <summary>Current inventory value computed using FIFO batch costs.</summary>
        Public Property InventoryValue As Decimal

        ''' <summary>Total net revenue recognised on the snapshot date.</summary>
        Public Property TodayRevenue As Decimal

        ''' <summary>Cumulative net revenue from the first of the month to the snapshot date.</summary>
        Public Property MonthToDateRevenue As Decimal

        ''' <summary>Cumulative net revenue from the first of the year to the snapshot date.</summary>
        Public Property YearToDateRevenue As Decimal

    End Class

End Namespace
