Namespace Entities

    ''' <summary>
    ''' Tracks a vendor invoice obligation arising from a received <see cref="PurchaseOrder"/>.
    ''' Records amounts owed, payments made, and outstanding balance until fully settled.
    ''' </summary>
    Public Class AccountsPayableEntry
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the <see cref="PurchaseOrder"/> that generated this payable.</summary>
        Public Property PurchaseOrderId As Integer

        ''' <summary>Foreign key to the <see cref="Vendor"/> owed this amount.</summary>
        Public Property VendorId As Integer

        ''' <summary>Vendor-issued invoice reference number for cross-referencing physical documents.</summary>
        Public Property InvoiceNumber As String

        ''' <summary>UTC date printed on the vendor's invoice.</summary>
        Public Property InvoiceDate As DateTime

        ''' <summary>UTC date by which payment must be made to the vendor.</summary>
        Public Property DueDate As DateTime

        ''' <summary>Total amount owed as stated on the vendor invoice.</summary>
        Public Property TotalAmount As Decimal

        ''' <summary>Running total of all payments made against this entry.</summary>
        Public Property AmountPaid As Decimal

        ''' <summary>Outstanding amount: TotalAmount − AmountPaid.</summary>
        Public Property Balance As Decimal

        ''' <summary>True when Balance reaches zero and the obligation is fully settled.</summary>
        Public Property IsPaid As Boolean

        ''' <summary>Free-text notes about payment arrangements or disputes; Nothing if not provided.</summary>
        Public Property Notes As String

        ' --- Navigation ---

        ''' <summary>The purchase order that created this payable obligation.</summary>
        Public Property PurchaseOrder As PurchaseOrder

        ''' <summary>The vendor to whom this amount is owed.</summary>
        Public Property Vendor As Vendor

    End Class

End Namespace
