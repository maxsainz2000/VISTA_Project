Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' Partial-class extension that adds the BIR per-line VAT breakdown columns to
    ''' <see cref="SalesTransactionLine"/> without modifying the POS-01 source file.
    ''' Fields are populated by <c>VatCalculator.CalculateLine</c> inside
    ''' <c>VatAwareReceiptService.GenerateReceiptAsync</c>.
    ''' </summary>
    Partial Public Class SalesTransactionLine

        ''' <summary>
        ''' BIR VAT classification for this product line.
        ''' Defaults to <see cref="VatTreatment.Vatable"/> on backfill.
        ''' When <c>VatConfiguration.IsVatRegistered</c> is False the calculator overrides
        ''' this to Exempt regardless of the stored value.
        ''' </summary>
        Public Property Treatment As VatTreatment

        ''' <summary>
        ''' Net taxable base (VAT-exclusive) when <see cref="Treatment"/> is Vatable; 0 otherwise.
        ''' Computed as Round(LineTotal / (1 + rate), 2, Banker) when VAT-registered.
        ''' </summary>
        Public Property VatableAmount As Decimal

        ''' <summary>Gross line amount when <see cref="Treatment"/> is Exempt; 0 otherwise.</summary>
        Public Property VatExemptAmount As Decimal

        ''' <summary>Gross line amount when <see cref="Treatment"/> is ZeroRated; 0 otherwise.</summary>
        Public Property ZeroRatedAmount As Decimal

        ''' <summary>
        ''' Output VAT charged on this line: LineTotal - VatableAmount when Vatable and registered;
        ''' 0 for Exempt and ZeroRated lines, and always 0 when not VAT-registered.
        ''' </summary>
        Public Property OutputVat As Decimal

    End Class

End Namespace
