Imports MerchSys.SharedKernel.Sync

Namespace Interfaces

    ''' <summary>
    ''' Cross-cutting service for surfacing infrastructure status changes to the UI shell.
    ''' Implementations raise sync status events that the shell status bar can observe.
    ''' </summary>
    Public Interface INotificationService

        Sub NotifySyncStatusChanged(newStatus As SyncStatus)

    End Interface

End Namespace
