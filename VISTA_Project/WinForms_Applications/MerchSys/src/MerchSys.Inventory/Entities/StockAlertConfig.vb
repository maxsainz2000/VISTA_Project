Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Per-product alert configuration that drives low-stock and near-expiry notifications
    ''' shown on the dashboard and in the Inventory module alert list.
    ''' </summary>
    Public Class StockAlertConfig
        Inherits AuditableEntity

        ''' <summary>Foreign key to the monitored <see cref="Product"/>.</summary>
        Public Property ProductId As Integer

        ''' <summary>Alert fires when current stock quantity is at or below this value.</summary>
        Public Property MinimumThreshold As Integer

        ''' <summary>Alert fires when a batch's expiry date is within this many days (default 30).</summary>
        Public Property ExpiryAlertDays As Integer = 30

        ''' <summary>False to silence all alerts for this product without deleting the configuration.</summary>
        Public Property IsAlertEnabled As Boolean = True

        ''' <summary>The product this alert configuration applies to.</summary>
        Public Property Product As Product

    End Class

End Namespace
