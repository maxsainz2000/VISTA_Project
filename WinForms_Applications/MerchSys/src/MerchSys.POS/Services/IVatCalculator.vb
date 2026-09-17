Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    ''' <summary>
    ''' Decomposes VAT-inclusive gross amounts into the three BIR buckets (Vatable, Exempt, ZeroRated)
    ''' and computes the resulting output VAT using banker's rounding at line level.
    ''' See: LLM_Wiki/wiki/concepts/vat-ready.md — three-bucket model and decomposition rules.
    ''' </summary>
    Public Interface IVatCalculator

        ''' <summary>
        ''' Decomposes a single VAT-inclusive <paramref name="grossAmount"/> into a <see cref="VatBreakdown"/>
        ''' given the line's BIR <paramref name="treatment"/> and the current <paramref name="config"/>.
        ''' When <c>config.IsVatRegistered</c> is False every line is treated as Exempt regardless of treatment.
        ''' </summary>
        Function Decompose(grossAmount As Decimal, treatment As VatTreatment, config As VatConfiguration) As VatBreakdown

        ''' <summary>
        ''' Convenience overload that reads the gross amount from <see cref="SalesTransactionLine.LineTotal"/>
        ''' and the treatment from <see cref="SalesTransactionLine.Treatment"/>.
        ''' </summary>
        Function CalculateLine(line As SalesTransactionLine, config As VatConfiguration) As VatBreakdown

        ''' <summary>
        ''' Sums line-level breakdowns into transaction-level totals.
        ''' Lines must already have <see cref="SalesTransactionLine.VatableAmount"/> etc. populated
        ''' (i.e., <see cref="CalculateLine"/> must have been called for each line first).
        ''' </summary>
        Function AggregateTransaction(transaction As SalesTransaction, config As VatConfiguration) As TransactionVatTotals

    End Interface

    ''' <summary>
    ''' Per-line VAT decomposition result. All amounts in Philippine pesos, precision(18,2).
    ''' Exactly one of VatableAmount / VatExemptAmount / ZeroRatedAmount will be non-zero
    ''' for any given line.
    ''' </summary>
    Public Class VatBreakdown

        ''' <summary>VAT-exclusive taxable base (Gross / (1 + rate)), Banker's rounding.</summary>
        Public Property VatableAmount As Decimal

        ''' <summary>Full gross amount when the line is exempt from VAT.</summary>
        Public Property VatExemptAmount As Decimal

        ''' <summary>Full gross amount when the line is zero-rated.</summary>
        Public Property ZeroRatedAmount As Decimal

        ''' <summary>Output VAT = Gross - VatableAmount for Vatable lines; 0 otherwise.</summary>
        Public Property OutputVat As Decimal

    End Class

    ''' <summary>
    ''' Transaction-level sums of the three BIR VAT buckets and total output VAT.
    ''' Equals the sum of per-line <see cref="VatBreakdown"/> values to the centavo.
    ''' </summary>
    Public Class TransactionVatTotals

        ''' <summary>Sum of <see cref="VatBreakdown.VatableAmount"/> across all lines.</summary>
        Public Property VatableSales As Decimal

        ''' <summary>Sum of <see cref="VatBreakdown.VatExemptAmount"/> across all lines.</summary>
        Public Property VatExemptSales As Decimal

        ''' <summary>Sum of <see cref="VatBreakdown.ZeroRatedAmount"/> across all lines.</summary>
        Public Property ZeroRatedSales As Decimal

        ''' <summary>Sum of <see cref="VatBreakdown.OutputVat"/> across all lines.</summary>
        Public Property OutputVat As Decimal

    End Class

End Namespace
