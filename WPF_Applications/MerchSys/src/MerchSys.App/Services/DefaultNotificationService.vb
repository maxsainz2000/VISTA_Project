Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync
Imports Notification.Wpf

Namespace Services

    Public Class DefaultNotificationService
        Implements INotificationService

        Private ReadOnly _toastManager As NotificationManager

        Private _currentSyncStatus As SyncStatus = SyncStatus.Offline
        Private _lastSuccessfulPushAt As Nullable(Of DateTimeOffset)

        Public ReadOnly Property CurrentSyncStatus As SyncStatus Implements INotificationService.CurrentSyncStatus
            Get
                Return _currentSyncStatus
            End Get
        End Property

        ''' <summary>
        ''' Set when the status transitions Syncing → Online, meaning a push cycle completed.
        ''' </summary>
        Public ReadOnly Property LastSuccessfulPushAt As Nullable(Of DateTimeOffset) Implements INotificationService.LastSuccessfulPushAt
            Get
                Return _lastSuccessfulPushAt
            End Get
        End Property

        Public Event SyncStatusChanged As EventHandler(Of SyncStatus) Implements INotificationService.SyncStatusChanged

        Public Sub New()
            _toastManager = New NotificationManager()
        End Sub

        Public Sub NotifySyncStatusChanged(newStatus As SyncStatus) Implements INotificationService.NotifySyncStatusChanged
            If newStatus = SyncStatus.Online AndAlso _currentSyncStatus = SyncStatus.Syncing Then
                _lastSuccessfulPushAt = DateTimeOffset.Now
            End If
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
