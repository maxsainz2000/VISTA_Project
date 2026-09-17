Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Records a single receipt of stock for a product. The FIFO deduction engine always consumes
    ''' the batch with the oldest <see cref="ReceiptDate"/> first, ensuring cost accuracy.
    ''' </summary>
    Public Class StockBatch
        Inherits AuditableEntity

        ''' <summary>Foreign key to <see cref="Product"/>.</summary>
        Public Property ProductId As Integer

        ''' <summary>Total units in this shipment at the time of receipt.</summary>
        Public Property QuantityReceived As Integer

        ''' <summary>Units not yet sold, consumed, or recorded as shrinkage. Decremented on each deduction.</summary>
        Public Property QuantityRemaining As Integer

        ''' <summary>Purchase price per unit paid for this batch, used for FIFO cost-of-goods calculations.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Date and time (UTC) this batch was physically received into the store.</summary>
        Public Property ReceiptDate As DateTime

        ''' <summary>
        ''' Optional expiry date for perishable products (pesticides, seeds, feeds).
        ''' Nothing when the parent product has <see cref="Product.HasExpiry"/> = False.
        ''' </summary>
        Public Property ExpiryDate As DateTime?

        ''' <summary>
        ''' Cross-module reference to the originating Purchase Order in MerchSys.Purchasing.
        ''' Stored as a plain integer — no EF navigation property to avoid cross-module coupling.
        ''' </summary>
        Public Property SourcePurchaseOrderId As Integer?

        ''' <summary>True when ExpiryDate is set and the batch has passed its expiry date.</summary>
        Public ReadOnly Property IsExpired As Boolean
            Get
                Return ExpiryDate.HasValue AndAlso ExpiryDate.Value < DateTime.UtcNow
            End Get
        End Property

        ''' <summary>True when all originally received units have been consumed.</summary>
        Public ReadOnly Property IsFullyConsumed As Boolean
            Get
                Return QuantityRemaining = 0
            End Get
        End Property

        ''' <summary>The product this batch belongs to.</summary>
        Public Property Product As Product

    End Class

End Namespace
