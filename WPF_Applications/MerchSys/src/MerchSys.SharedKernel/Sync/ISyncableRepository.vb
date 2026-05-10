Namespace Sync

    ''' <summary>
    ''' Implemented by each module's data layer to expose pending local writes for
    ''' synchronization to the central MariaDB instance.
    ''' Module repositories append rows to <c>Sync_Journal</c> on every local write;
    ''' the sync worker calls these members to discover and confirm transmission.
    ''' </summary>
    Public Interface ISyncableRepository

        ''' <summary>Module identifier matching the <c>ModuleName</c> column in <c>Sync_Journal</c>.</summary>
        ReadOnly Property ModuleName As String

        ''' <summary>Returns journal entries where <c>SyncedAt</c> is null, ordered by <c>CreatedAt</c>.</summary>
        Function GetPendingChangesAsync() As Task(Of IReadOnlyList(Of SyncJournal))

        ''' <summary>Sets <c>SyncedAt = UtcNow</c> on the supplied journal row IDs.</summary>
        Function MarkSyncedAsync(journalIds As IEnumerable(Of Long)) As Task

    End Interface

End Namespace
