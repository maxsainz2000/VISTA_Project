Imports MerchSys.Purchasing.Entities

Namespace Services.Vat

    ''' <summary>
    ''' Pure-function calculator that derives input-VAT buckets from a confirmed goods receipt.
    '''
    ''' **Known limitation — Option-2 simplification:**
    ''' All receipt lines are treated as <see cref="MerchSys.SharedKernel.Enums.VatTreatment.Vatable"/>
    ''' at the 12 % Philippine VAT rate.  <see cref="GoodsReceiptLine.UnitCost"/> is assumed to be
    ''' VAT-inclusive (i.e., the vendor invoice price already contains VAT).
    '''
    ''' This is a deliberate scope concession: <see cref="GoodsReceiptLine"/> does not yet carry
    ''' a per-line VAT classification column.  A follow-up plan should extend the entity and the
    ''' goods-receiving UI with per-line <c>VatClassification</c> and <c>VatAmount</c> columns
    ''' before VISTA serves businesses with mixed input types (vatable + exempt + zero-rated).
    '''
    ''' For businesses that purchase exclusively vatable goods (the typical Villon Farm Supply
    ''' scenario) this calculator is accurate.
    ''' </summary>
    Public Class GoodsReceiptVatCalculator

        Private Const VatDivisor As Decimal = 1.12D

        ''' <summary>
        ''' Computes <see cref="GoodsReceiptVatBreakdown"/> for the supplied receipt lines.
        ''' With the Option-2 assumption: VatableInputs = invoiceTotal / 1.12,
        ''' InputVat = invoiceTotal − VatableInputs, VatExemptInputs = 0, ZeroRatedInputs = 0.
        ''' </summary>
        Public Function Calculate(
            receipt As GoodsReceipt,
            lines As IEnumerable(Of GoodsReceiptLine)
        ) As GoodsReceiptVatBreakdown

            Dim invoiceTotal As Decimal = 0D
            For Each line In lines
                invoiceTotal += CDec(line.QuantityReceived) * line.UnitCost
            Next

            Dim vatableInputs As Decimal = Math.Round(invoiceTotal / VatDivisor, 2)
            Dim inputVat As Decimal = invoiceTotal - vatableInputs

            Return New GoodsReceiptVatBreakdown With {
                .VatableInputs = vatableInputs,
                .VatExemptInputs = 0D,
                .ZeroRatedInputs = 0D,
                .InputVat = inputVat,
                .VendorInvoiceTotal = invoiceTotal
            }
        End Function

    End Class

    ''' <summary>
    ''' Aggregate VAT buckets produced by <see cref="GoodsReceiptVatCalculator.Calculate"/>.
    ''' All amounts are in Philippine Peso, precision 18,2.
    ''' </summary>
    Public Class GoodsReceiptVatBreakdown

        ''' <summary>Sum of VAT-exclusive line amounts for vatable purchases (invoiceTotal / 1.12).</summary>
        Public Property VatableInputs As Decimal

        ''' <summary>Sum of line amounts classified as VAT-exempt. Always 0 under Option-2 simplification.</summary>
        Public Property VatExemptInputs As Decimal

        ''' <summary>Sum of line amounts classified as zero-rated. Always 0 under Option-2 simplification.</summary>
        Public Property ZeroRatedInputs As Decimal

        ''' <summary>Total creditable input VAT (invoiceTotal − VatableInputs). Maps to ACC-11 input-VAT ledger.</summary>
        Public Property InputVat As Decimal

        ''' <summary>Sum of qty × UnitCost across all lines (VAT-inclusive). Equals VatableInputs + InputVat.</summary>
        Public Property VendorInvoiceTotal As Decimal

    End Class

End Namespace
