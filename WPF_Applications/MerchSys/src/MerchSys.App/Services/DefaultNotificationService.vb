Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Services

    Public Class DefaultNotificationService
        Implements INotificationService

        Private _currentSyncStatus As SyncStatus = SyncStatus.Offline

        Public ReadOnly Property CurrentSyncStatus As SyncStatus
            Get
                Return _currentSyncStatus
            End Get
        End Property

        Public Event SyncStatusChanged As EventHandler(Of SyncStatus)

        Public Sub NotifySyncStatusChanged(newStatus As SyncStatus) Implements INotificationService.NotifySyncStatusChanged
            _currentSyncStatus = newStatus
            RaiseEvent SyncStatusChanged(Me, newStatus)
        End Sub

    End Class

End Namespace
