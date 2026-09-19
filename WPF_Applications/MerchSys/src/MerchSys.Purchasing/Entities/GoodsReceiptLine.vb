Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' Represents a single product line within a <see cref="GoodsReceipt"/>.
    ''' Records actual quantities and costs at the moment of receipt, which may differ
    ''' from the original purchase order. Expiry date is captured here for FIFO batch costing.
    ''' </summary>
    Public Class GoodsReceiptLine
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the parent <see cref="GoodsReceipt"/>.</summary>
        Public Property GoodsReceiptId As Integer

        ''' <summary>Cross-module reference to the product (Inventory module ID only; no EF navigation).</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalized product name captured at receipt time to avoid cross-module queries.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units expected per the originating purchase order line.</summary>
        Public Property QuantityOrdered As Integer

        ''' <summary>Number of units actually counted and accepted at the receiving dock.</summary>
        Public Property QuantityReceived As Integer

        ''' <summary>Actual landed cost per unit at the time of receipt; may differ from the PO unit cost.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Expiry date of this batch; Nothing for non-perishable products.</summary>
        Public Property ExpiryDate As DateTime?

        ''' <summary>True when QuantityReceived does not match QuantityOrdered.</summary>
        Public Property HasDiscrepancy As Boolean

        ''' <summary>Explanation of the quantity or condition discrepancy; Nothing when HasDiscrepancy is False.</summary>
        Public Property DiscrepancyNotes As String

        ''' <summary>
        ''' BIR three-bucket VAT classification for this purchase line.
        ''' Vatable = 12% input VAT creditable under NIRC Sec. 110; Exempt = no VAT;
        ''' ZeroRated = 0% rated, input VAT creditable. Default Vatable covers most agricultural retail purchases.
        ''' Reference: Revenue Regulations No. 16-2005, BIR.
        ''' </summary>
        Public Property VatClassification As VatTreatment

        ''' <summary>
        ''' Total creditable input VAT on this line.
        ''' Computed: LineTotal - (LineTotal / 1.12) for Vatable; 0 for Exempt and ZeroRated.
        ''' Rate source: Philippine VAT Act, NIRC Sec. 106 — standard 12% rate.
        ''' </summary>
        Public Property VatAmount As Decimal

        ''' <summary>VAT-exclusive line amount. LineTotal / 1.12 for Vatable; LineTotal for Exempt and ZeroRated.</summary>
        Public Property VatableSales As Decimal

        ' --- Navigation ---

        ''' <summary>The parent goods receipt that owns this line.</summary>
        Public Property GoodsReceipt As GoodsReceipt

    End Class

End Namespace
