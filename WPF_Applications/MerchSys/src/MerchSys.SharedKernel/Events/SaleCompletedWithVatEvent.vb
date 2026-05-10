Imports MediatR
Imports MerchSys.SharedKernel.Enums

Namespace Events

    ''' <summary>
    ''' Published by POS (POS-14) when a VAT-aware sales transaction is finalised.
    ''' Consumed by ACC-10 (revenue journal) and ACC-11 (VAT summary ledger).
    ''' Sibling to <see cref="SaleCompletedEvent"/>: both are published for the same transaction
    ''' during the migration window so legacy and VAT-aware consumers each receive their contract.
    ''' Use <see cref="TransactionId"/> as the idempotency key — handlers must be idempotent
    ''' against double-publish if both events touch the same ledger row.
    ''' BIR rationale: OR must disclose vatable, exempt, and zero-rated sales separately
    ''' plus the output VAT amount (Revenue Regulations No. 16-2005).
    ''' </summary>
    Public Class SaleCompletedWithVatEvent
        Implements INotification

        ''' <summary>ID of the completed POS transaction. Natural idempotency key for ACC-10 and ACC-11.</summary>
        Public Property TransactionId As Integer

        ''' <summary>UTC date/time the transaction was finalised.</summary>
        Public Property TransactionDate As DateTime

        ''' <summary>Payment method used for this transaction.</summary>
        Public Property PaymentMethod As PaymentMethod

        ''' <summary>Total transaction amount including all line items after discounts (vatable + exempt + zero-rated).</summary>
        Public Property TotalAmount As Decimal

        ''' <summary>Optional customer ID. Nothing for anonymous cash sales.</summary>
        Public Property CustomerId As Integer?

        ''' <summary>Products sold in this transaction with per-line VAT breakdowns.</summary>
        Public Property Items As List(Of SaleItemWithVat)

        ''' <summary>
        ''' Sum of line <see cref="SaleItemWithVat.VatableAmount"/> across all <see cref="VatTreatment.Vatable"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the OR vatable sales bucket.
        ''' </summary>
        Public Property VatableSales As Decimal

        ''' <summary>
        ''' Sum of line amounts for <see cref="VatTreatment.Exempt"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the OR VAT-exempt sales bucket.
        ''' </summary>
        Public Property VatExemptSales As Decimal

        ''' <summary>
        ''' Sum of line amounts for <see cref="VatTreatment.ZeroRated"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the OR zero-rated sales bucket.
        ''' </summary>
        Public Property ZeroRatedSales As Decimal

        ''' <summary>
        ''' Total output VAT collected — sum of <see cref="SaleItemWithVat.OutputVat"/> across all lines.
        ''' Precision(18,2). Recorded as a VAT payable liability by ACC-10.
        ''' </summary>
        Public Property OutputVat As Decimal

        ''' <summary>
        ''' Snapshot of the VAT-registered flag at the moment of sale.
        ''' Historical receipts must reflect the VAT mode at issuance — not the current config —
        ''' so this is captured at publish time rather than read from config at consume time.
        ''' </summary>
        Public Property IsVatRegistered As Boolean

        Public Sub New()
            Items = New List(Of SaleItemWithVat)()
        End Sub

        ''' <summary>A single product line within a VAT-aware completed sale.</summary>
        Public Class SaleItemWithVat

            ''' <summary>Product identifier (matches Inv_ tables).</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name, denormalized for event consumers.</summary>
            Public Property ProductName As String

            ''' <summary>Number of units sold.</summary>
            Public Property Quantity As Integer

            ''' <summary>Selling price per unit before discount (VAT-exclusive for vatable lines).</summary>
            Public Property UnitPrice As Decimal

            ''' <summary>Total discount applied to this line item.</summary>
            Public Property DiscountAmount As Decimal

            ''' <summary>BIR VAT classification for this line. Drives bucket allocation in ACC-11.</summary>
            Public Property Treatment As VatTreatment

            ''' <summary>
            ''' Net taxable amount for this line (after discount) when <see cref="Treatment"/> is <see cref="VatTreatment.Vatable"/>;
            ''' zero otherwise. Precision(18,2).
            ''' </summary>
            Public Property VatableAmount As Decimal

            ''' <summary>
            ''' Net amount for this line when <see cref="Treatment"/> is <see cref="VatTreatment.Exempt"/>;
            ''' zero otherwise. Precision(18,2).
            ''' </summary>
            Public Property VatExemptAmount As Decimal

            ''' <summary>
            ''' Net amount for this line when <see cref="Treatment"/> is <see cref="VatTreatment.ZeroRated"/>;
            ''' zero otherwise. Precision(18,2).
            ''' </summary>
            Public Property ZeroRatedAmount As Decimal

            ''' <summary>
            ''' Output VAT charged on this line (VatableAmount × VAT rate); zero for exempt and zero-rated lines.
            ''' Precision(18,2). Summed into <see cref="SaleCompletedWithVatEvent.OutputVat"/> at event level.
            ''' </summary>
            Public Property OutputVat As Decimal

        End Class

    End Class

End Namespace
