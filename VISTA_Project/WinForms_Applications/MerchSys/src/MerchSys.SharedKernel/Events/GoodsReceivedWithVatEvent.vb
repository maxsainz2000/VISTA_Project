Imports MediatR
Imports MerchSys.SharedKernel.Enums

Namespace Events

    ''' <summary>
    ''' Published by Purchasing when a VAT-aware goods receipt is confirmed.
    ''' Consumed by ACC-10 (AP liability journal) and ACC-11 (input VAT ledger).
    ''' Sibling to <see cref="GoodsReceivedEvent"/>: both are published for the same receipt
    ''' during the migration window so legacy and VAT-aware consumers each receive their contract.
    ''' Use <see cref="PurchaseOrderId"/> as the idempotency key — handlers must be idempotent
    ''' against double-publish if both events touch the same ledger row.
    ''' BIR rationale: input VAT on purchases is creditable only when separately disclosed
    ''' on the supplier's VAT invoice (NIRC Sec. 110).
    ''' </summary>
    Public Class GoodsReceivedWithVatEvent
        Implements INotification

        ''' <summary>ID of the purchase order that was received. Natural idempotency key for ACC-10 and ACC-11.</summary>
        Public Property PurchaseOrderId As Integer

        ''' <summary>UTC date/time the goods were physically received.</summary>
        Public Property ReceivedDate As DateTime

        ''' <summary>Line items included in this receipt with per-line VAT breakdowns.</summary>
        Public Property Items As List(Of GoodsReceivedItemWithVat)

        ''' <summary>
        ''' Sum of line amounts for <see cref="VatTreatment.Vatable"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the input vatable purchases bucket.
        ''' </summary>
        Public Property VatableInput As Decimal

        ''' <summary>
        ''' Sum of line amounts for <see cref="VatTreatment.Exempt"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the input exempt purchases bucket.
        ''' </summary>
        Public Property VatExemptInput As Decimal

        ''' <summary>
        ''' Sum of line amounts for <see cref="VatTreatment.ZeroRated"/> lines.
        ''' Precision(18,2). Used by ACC-11 for the input zero-rated purchases bucket.
        ''' </summary>
        Public Property ZeroRatedInput As Decimal

        ''' <summary>
        ''' Total creditable input VAT on this receipt — sum of <see cref="GoodsReceivedItemWithVat.InputVat"/> across all lines.
        ''' Precision(18,2). Recorded as input VAT asset by ACC-10.
        ''' </summary>
        Public Property InputVat As Decimal

        Public Sub New()
            Items = New List(Of GoodsReceivedItemWithVat)()
        End Sub

        ''' <summary>A single product line within a VAT-aware goods receipt.</summary>
        Public Class GoodsReceivedItemWithVat

            ''' <summary>Product identifier (matches Inv_ tables).</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name, denormalized for event consumers.</summary>
            Public Property ProductName As String

            ''' <summary>Number of units received.</summary>
            Public Property QuantityReceived As Integer

            ''' <summary>Purchase cost per unit, VAT-exclusive for vatable lines (used to create FIFO batch).</summary>
            Public Property UnitCost As Decimal

            ''' <summary>Optional expiry date. Nothing for non-perishable products.</summary>
            Public Property ExpiryDate As DateTime?

            ''' <summary>BIR VAT classification for this purchase line. Drives input VAT creditability in ACC-11.</summary>
            Public Property Treatment As VatTreatment

            ''' <summary>
            ''' Creditable input VAT on this line (net cost × VAT rate); zero for exempt and zero-rated lines.
            ''' Precision(18,2). Summed into <see cref="GoodsReceivedWithVatEvent.InputVat"/> at event level.
            ''' </summary>
            Public Property InputVat As Decimal

        End Class

    End Class

End Namespace
