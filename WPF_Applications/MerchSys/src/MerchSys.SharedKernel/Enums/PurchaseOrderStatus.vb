Namespace Enums

    ''' <summary>
    ''' Lifecycle states of a purchase order from creation through closure.
    ''' </summary>
    Public Enum PurchaseOrderStatus

        ''' <summary>PO has been created but not yet sent to the vendor.</summary>
        Draft = 1

        ''' <summary>PO has been submitted/sent to the vendor and is awaiting delivery.</summary>
        Submitted = 2

        ''' <summary>Goods have been physically received at the store.</summary>
        Received = 3

        ''' <summary>Received goods have been verified against the PO quantities and invoice.</summary>
        Verified = 4

        ''' <summary>PO is fully settled — goods received, invoice matched, and AP recorded.</summary>
        Closed = 5

    End Enum

End Namespace
