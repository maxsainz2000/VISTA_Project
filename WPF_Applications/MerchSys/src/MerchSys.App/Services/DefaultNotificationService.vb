Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync
Imports Notification.Wpf

Namespace Services

    Public Class DefaultNotificationService
        Implements INotificationService

        Private ReadOnly _toastManager As NotificationManager

        Private _currentSyncStatus As SyncStatus = SyncStatus.Offline

        Public ReadOnly Property CurrentSyncStatus As SyncStatus
            Get
                Return _currentSyncStatus
            End Get
        End Property

        Public Event SyncStatusChanged As EventHandler(Of SyncStatus)

        Public Sub New()
            _toastManager = New NotificationManager()
        End Sub

        Public Sub NotifySyncStatusChanged(newStatus As SyncStatus) Implements INotificationService.NotifySyncStatusChanged
            _currentSyncStatus = newStatus
            RaiseEvent SyncStatusChanged(Me, newStatus)
        End Sub

        Public Sub ShowSuccess(message As String) Implements INotificationService.ShowSuccess
            _toastManager.Show(New NotificationContent() With {
                .Title = "Success",
                .Message = message,
                .Type = NotificationType.Success
            })
        End Sub

        Public Sub ShowError(message As String) Implements INotificationService.ShowError
            _toastManager.Show(New NotificationContent() With {
                .Title = "Error",
                .Message = message,
                .Type = NotificationType.Error
            })
        End Sub

    End Class

End Namespace
