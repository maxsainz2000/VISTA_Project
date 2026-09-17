Namespace Entities

    ''' <summary>
    ''' Partial-class extension that adds the BIR three-bucket VAT totals to
    ''' <see cref="SalesTransaction"/> without modifying the POS-01 source file.
    ''' Columns are populated by <c>VatAwareReceiptService</c> immediately before
    ''' the Official Receipt is generated.
    ''' </summary>
    Partial Public Class SalesTransaction

        ''' <summary>Sum of <see cref="SalesTransactionLine.VatableAmount"/> across all Vatable lines.</summary>
        Public Property VatableSales As Decimal

        ''' <summary>Sum of gross amounts for all VAT-Exempt lines.</summary>
        Public Property VatExemptSales As Decimal

        ''' <summary>Sum of gross amounts for all Zero-Rated lines.</summary>
        Public Property ZeroRatedSales As Decimal

        ''' <summary>
        ''' Snapshot of <c>VatConfiguration.VatRate</c> captured at the moment of sale.
        ''' Historical receipts retain the rate they were issued under even after a config change.
        ''' </summary>
        Public Property VatRateSnapshot As Decimal

        ''' <summary>
        ''' Snapshot of <c>VatConfiguration.IsVatRegistered</c> captured at the moment of sale.
        ''' Determines whether output VAT was collected at the time of issuance.
        ''' </summary>
        Public Property IsVatRegisteredSnapshot As Boolean

    End Class

End Namespace
