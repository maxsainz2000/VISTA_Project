Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Records an inventory loss event (damage, spoilage, expiry, admin error) along with its financial impact.
    ''' Publishes <c>ShrinkageRecordedEvent</c> so Accounting can post the corresponding expense entry.
    ''' </summary>
    Public Class ShrinkageRecord
        Inherits AuditableEntity

        ''' <summary>Foreign key to the affected <see cref="Product"/>.</summary>
        Public Property ProductId As Integer

        ''' <summary>Optional foreign key to a specific <see cref="StockBatch"/> when the loss can be traced to one batch.</summary>
        Public Property StockBatchId As Integer?

        ''' <summary>Number of units lost.</summary>
        Public Property QuantityLost As Integer

        ''' <summary>Unit cost at the time of the loss, sourced from the originating batch.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Total monetary value of the loss: QuantityLost × UnitCost.</summary>
        Public Property TotalValue As Decimal

        ''' <summary>Reason code for the loss. Allowed values: "Damage", "Spoilage", "Expiry", "Admin Error".</summary>
        Public Property Reason As String

        ''' <summary>Optional free-text details recorded by the staff member.</summary>
        Public Property Notes As String

        ''' <summary>Date and time (UTC) the shrinkage event was recorded.</summary>
        Public Property RecordedDate As DateTime

        ''' <summary>The product that experienced the shrinkage.</summary>
        Public Property Product As Product

        ''' <summary>The specific stock batch affected, if identified.</summary>
        Public Property StockBatch As StockBatch

    End Class

End Namespace
