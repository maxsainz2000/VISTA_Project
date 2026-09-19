Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' One line of recognised revenue, created per-product when Accounting handles a
    ''' <c>SaleCompletedEvent</c>.  Stores the FIFO-costed COGS alongside gross sales
    ''' figures so per-product margin can be reported without a cross-module query.
    ''' </summary>
    Public Class RevenueRecord
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Calendar date on which the sale was recorded.</summary>
        Public Property RecordDate As DateTime

        ''' <summary>Foreign key to the originating POS transaction (integer ID, no navigation).</summary>
        Public Property SourceTransactionId As Integer

        ''' <summary>Payment method used by the customer for this transaction.</summary>
        Public Property PaymentMethod As PaymentMethod

        ''' <summary>Full sale price before any discounts.</summary>
        Public Property GrossAmount As Decimal

        ''' <summary>Total discount applied to this line.</summary>
        Public Property DiscountAmount As Decimal

        ''' <summary>Net sale amount received: GrossAmount minus DiscountAmount.</summary>
        Public Property NetAmount As Decimal

        ''' <summary>VAT component included in the net amount.</summary>
        Public Property VatAmount As Decimal

        ''' <summary>Foreign key to the sold product (integer ID, no navigation).</summary>
        Public Property ProductId As Integer

        ''' <summary>Snapshot of the product name at the time of sale.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units sold on this line.</summary>
        Public Property QuantitySold As Integer

        ''' <summary>Cost of goods sold for this line, derived from FIFO batch costs at time of sale.</summary>
        Public Property COGS As Decimal

        ''' <summary>Gross profit on this line: NetAmount minus COGS.</summary>
        Public Property GrossProfit As Decimal

    End Class

End Namespace
