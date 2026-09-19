Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services.Vat

    ''' <summary>
    ''' Pure-function calculator that derives input-VAT buckets from a confirmed goods receipt.
    ''' Reads per-line <see cref="GoodsReceiptLine.VatClassification"/>, <see cref="GoodsReceiptLine.VatAmount"/>,
    ''' and <see cref="GoodsReceiptLine.VatableSales"/> (populated by <c>GoodsReceivingService</c> before save)
    ''' to produce the three-bucket aggregate required by <c>GoodsReceivedWithVatEvent</c> and ACC-11.
    ''' BIR rationale: input VAT on purchases is creditable only when separately disclosed on the
    ''' supplier's VAT invoice (NIRC Sec. 110).
    ''' </summary>
    Public Class GoodsReceiptVatCalculator

        ''' <summary>
        ''' Computes <see cref="GoodsReceiptVatBreakdown"/> for the supplied receipt lines using
        ''' per-line <see cref="GoodsReceiptLine.VatClassification"/> set at receiving time.
        ''' </summary>
        Public Function Calculate(
            receipt As GoodsReceipt,
            lines As IEnumerable(Of GoodsReceiptLine)
        ) As GoodsReceiptVatBreakdown

            Dim vatableInputs As Decimal = 0D
            Dim vatExemptInputs As Decimal = 0D
            Dim zeroRatedInputs As Decimal = 0D
            Dim inputVat As Decimal = 0D
            Dim invoiceTotal As Decimal = 0D

            For Each line In lines
                Dim lineTotal As Decimal = CDec(line.QuantityReceived) * line.UnitCost
                invoiceTotal += lineTotal
                Select Case line.VatClassification
                    Case VatTreatment.Vatable
                        vatableInputs += line.VatableSales
                        inputVat += line.VatAmount
                    Case VatTreatment.Exempt
                        vatExemptInputs += lineTotal
                    Case VatTreatment.ZeroRated
                        zeroRatedInputs += lineTotal
                End Select
            Next

            Return New GoodsReceiptVatBreakdown With {
                .VatableInputs = Math.Round(vatableInputs, 2),
                .VatExemptInputs = Math.Round(vatExemptInputs, 2),
                .ZeroRatedInputs = Math.Round(zeroRatedInputs, 2),
                .InputVat = Math.Round(inputVat, 2),
                .VendorInvoiceTotal = Math.Round(invoiceTotal, 2)
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
