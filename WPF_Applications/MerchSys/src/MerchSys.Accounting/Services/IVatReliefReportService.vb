Namespace Services

    ''' <summary>
    ''' Lightweight monthly VAT Relief summary used for BIR RELIEF reconciliation audits.
    ''' Aggregates the three-bucket VAT totals (Vatable / VAT-Exempt / Zero-Rated) from
    ''' the revenue and expense ledgers for a given calendar month.
    ''' </summary>
    Public Class VatReliefSummary
        Public Property Year As Integer
        Public Property Month As Integer

        ' Sales side (revenue ledger)
        Public Property VatableSales As Decimal
        Public Property VatExemptSales As Decimal
        Public Property ZeroRatedSales As Decimal
        Public Property OutputVat As Decimal
        Public Property TotalGrossSales As Decimal      ' VatableSales + VatExemptSales + ZeroRatedSales + OutputVat

        ' Purchases side (expense ledger)
        Public Property VatablePurchases As Decimal
        Public Property VatExemptPurchases As Decimal
        Public Property ZeroRatedPurchases As Decimal
        Public Property InputVat As Decimal
        Public Property TotalGrossPurchases As Decimal  ' VatablePurchases + VatExemptPurchases + ZeroRatedPurchases + InputVat

        ' Derived
        Public Property NetVatPayable As Decimal        ' OutputVat - InputVat (negative = credit carryover)

        ' Row counts
        Public Property SalesRecordCount As Integer
        Public Property PurchaseRecordCount As Integer
    End Class

    ''' <summary>
    ''' Provides ad-hoc monthly VAT Relief summaries aggregated directly from the revenue and
    ''' expense ledger tables.  This service is stateless and read-only: it does not touch
    ''' <c>Acc_VatReturns</c> and has no generate/file/lock lifecycle.  The data it returns
    ''' is reconciled against BIR's RELIEF system during audits.
    '''
    ''' Calendar month window: [year-month-01 00:00 UTC, next-month-01 00:00 UTC).
    ''' </summary>
    Public Interface IVatReliefReportService

        ''' <summary>
        ''' Aggregates revenue + expense ledger rows whose RecordDate falls within the
        ''' calendar month [year-month-01 00:00 UTC, next-month-01 00:00 UTC).
        ''' Pure read; does not touch Acc_VatReturns.
        ''' Returns an all-zero <see cref="VatReliefSummary"/> (never Nothing) when no rows exist.
        ''' </summary>
        Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of VatReliefSummary)

        ''' <summary>
        ''' Returns up to <paramref name="months"/> consecutive monthly summaries ending at
        ''' (endYear, endMonth), ordered chronologically — used by the trend grid.
        ''' <paramref name="months"/> is clamped to [1, 24].
        ''' </summary>
        Function GetTrailingMonthsAsync(endYear As Integer, endMonth As Integer, months As Integer) As Task(Of IReadOnlyList(Of VatReliefSummary))

    End Interface

End Namespace
