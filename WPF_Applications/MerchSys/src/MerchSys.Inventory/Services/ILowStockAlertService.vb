Namespace Services

    Public Interface ILowStockAlertService
        ''' <summary>
        ''' Checks all active products against their alert thresholds and fires a desktop toast
        ''' if any products are at or below their minimum stock level.
        ''' </summary>
        Function CheckAndGenerateAlertsAsync() As Task(Of List(Of LowStockAlertDto))

        ''' <summary>Returns the current low-stock alert list without triggering a toast.</summary>
        Function GetCurrentAlertsAsync() As Task(Of List(Of LowStockAlertDto))

        ''' <summary>Updates the minimum threshold for a product (upserts its StockAlertConfig).</summary>
        Function UpdateThresholdAsync(productId As Integer, newThreshold As Integer) As Task
    End Interface

    ''' <summary>
    ''' Abstraction over the desktop notification layer so MerchSys.Inventory does not take
    ''' a direct dependency on Notification.Wpf (a WPF-only package).
    ''' Register a concrete implementation in MerchSys.App DI backed by Notification.Wpf.
    ''' </summary>
    Public Interface ILowStockNotifier
        Sub NotifyLowStock(alertCount As Integer)
    End Interface

    Public Class LowStockAlertDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentStock As Integer
        Public Property MinimumThreshold As Integer
        ''' <summary>MinimumThreshold - CurrentStock (positive means stock is below threshold).</summary>
        Public Property Deficit As Integer
        Public Property LastRestockDate As DateTime?
    End Class

End Namespace
