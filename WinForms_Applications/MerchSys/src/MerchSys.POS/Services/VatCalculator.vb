Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    ''' <summary>
    ''' Implements VAT decomposition for Philippines retail sales.
    ''' Prices on file are VAT-inclusive; decomposition extracts the base and output VAT.
    ''' Banker's rounding (MidpointRounding.ToEven) is applied on every Round call per BIR examples
    ''' in LLM_Wiki/wiki/concepts/vat-ready.md.
    ''' Decomposition happens at line level; transaction totals are derived by summation — never
    ''' by re-decomposing the transaction gross — to prevent centavo drift.
    ''' </summary>
    Public Class VatCalculator
        Implements IVatCalculator

        Public Function Decompose(grossAmount As Decimal, treatment As VatTreatment,
                                  config As VatConfiguration) As VatBreakdown Implements IVatCalculator.Decompose
            Dim result As New VatBreakdown()

            ' When not VAT-registered every line is exempt and no output VAT is charged.
            If Not config.IsVatRegistered Then
                result.VatExemptAmount = grossAmount
                Return result
            End If

            Select Case treatment
                Case VatTreatment.Vatable
                    Dim r = config.VatRate
                    result.VatableAmount = Math.Round(grossAmount / (1D + r), 2, MidpointRounding.ToEven)
                    result.OutputVat = grossAmount - result.VatableAmount

                Case VatTreatment.Exempt
                    result.VatExemptAmount = grossAmount

                Case VatTreatment.ZeroRated
                    result.ZeroRatedAmount = grossAmount

            End Select

            Return result
        End Function

        Public Function CalculateLine(line As SalesTransactionLine,
                                      config As VatConfiguration) As VatBreakdown Implements IVatCalculator.CalculateLine
            Return Decompose(line.LineTotal, line.Treatment, config)
        End Function

        Public Function AggregateTransaction(transaction As SalesTransaction,
                                             config As VatConfiguration) As TransactionVatTotals Implements IVatCalculator.AggregateTransaction
            Dim totals As New TransactionVatTotals()

            For Each line In transaction.Lines
                totals.VatableSales += line.VatableAmount
                totals.VatExemptSales += line.VatExemptAmount
                totals.ZeroRatedSales += line.ZeroRatedAmount
                totals.OutputVat += line.OutputVat
            Next

            Return totals
        End Function

    End Class

End Namespace
