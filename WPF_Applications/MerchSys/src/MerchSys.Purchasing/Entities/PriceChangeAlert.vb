Namespace Entities

    ''' <summary>
    ''' Records a detected discrepancy between the agreed PO cost and the actual cost at goods receipt.
    ''' Remains unacknowledged until a manager reviews it and updates retail pricing as appropriate.
    ''' </summary>
    Public Class PriceChangeAlert
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>The product whose cost changed.</summary>
        Public Property ProductId As Integer

        ''' <summary>Snapshot of the product name at the time the alert was created.</summary>
        Public Property ProductName As String

        ''' <summary>The vendor who supplied the goods.</summary>
        Public Property VendorId As Integer

        ''' <summary>Snapshot of the vendor name at the time the alert was created.</summary>
        Public Property VendorName As String

        ''' <summary>The cost agreed on the original purchase order line.</summary>
        Public Property PreviousUnitCost As Decimal

        ''' <summary>The actual cost recorded on the goods receipt line.</summary>
        Public Property NewUnitCost As Decimal

        ''' <summary>((NewUnitCost - PreviousUnitCost) / PreviousUnitCost) × 100, rounded to 4 decimal places.</summary>
        Public Property ChangePercent As Decimal

        ''' <summary>"Increase" when the new cost is higher; "Decrease" when lower.</summary>
        Public Property ChangeDirection As String

        ''' <summary>The goods receipt that triggered this alert.</summary>
        Public Property GoodsReceiptId As Integer

        ''' <summary>False until a manager explicitly acknowledges the alert.</summary>
        Public Property IsAcknowledged As Boolean

        ''' <summary>UTC timestamp when the alert was acknowledged; Nothing if still pending.</summary>
        Public Property AcknowledgedAt As DateTime?

    End Class

End Namespace
