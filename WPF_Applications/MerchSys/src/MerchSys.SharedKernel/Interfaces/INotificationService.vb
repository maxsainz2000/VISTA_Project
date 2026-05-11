Imports MerchSys.SharedKernel.Sync

Namespace Interfaces

    ''' <summary>
    ''' Cross-cutting service for surfacing infrastructure status changes and user-facing
    ''' toast notifications to the UI shell.
    ''' </summary>
    Public Interface INotificationService

        Sub NotifySyncStatusChanged(newStatus As SyncStatus)

        ''' <summary>Raised whenever sync connectivity transitions between states.</summary>
        Event SyncStatusChanged As EventHandler(Of SyncStatus)

        ''' <summary>The most recently published sync state.</summary>
        ReadOnly Property CurrentSyncStatus As SyncStatus

        ''' <summary>
        ''' The timestamp of the last successful push to MariaDB, or Nothing on a fresh install.
        ''' Set when the status transitions Syncing → Online.
        ''' </summary>
        ReadOnly Property LastSuccessfulPushAt As Nullable(Of DateTimeOffset)

        ''' <summary>Displays a success toast notification with the given message.</summary>
        Sub ShowSuccess(message As String)

        ''' <summary>Displays an error toast notification with the given message.</summary>
        Sub ShowError(message As String)

    End Interface

End Namespace
