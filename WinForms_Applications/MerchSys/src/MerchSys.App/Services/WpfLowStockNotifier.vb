Imports MerchSys.Inventory.Services
Imports Notification.Wpf

Namespace Services

    ''' <summary>
    ''' WPF concrete implementation of ILowStockNotifier.
    ''' Wraps Notification.Wpf's NotificationManager to display desktop toast alerts.
    ''' Lives in MerchSys.App to avoid a WPF dependency inside the Inventory class library.
    ''' </summary>
    Public Class WpfLowStockNotifier
        Implements ILowStockNotifier

        Private ReadOnly _manager As NotificationManager

        Public Sub New()
            _manager = New NotificationManager()
        End Sub

        Public Sub NotifyLowStock(alertCount As Integer) Implements ILowStockNotifier.NotifyLowStock
            Dim message = If(alertCount = 1,
                "1 product is below its minimum stock threshold.",
                $"{alertCount} products are below their minimum stock thresholds.")

            _manager.Show(New NotificationContent() With {
                .Title = "Low Stock Alert",
                .Message = message,
                .Type = NotificationType.Warning
            })
        End Sub

    End Class

End Namespace
