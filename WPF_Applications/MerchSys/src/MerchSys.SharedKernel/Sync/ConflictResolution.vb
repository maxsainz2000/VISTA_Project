Namespace Sync

    ''' <summary>
    ''' Per-table conflict resolution policy applied when a local journal entry targets a row
    ''' that may already exist in the central MariaDB instance.
    ''' </summary>
    Public Enum ConflictResolution
        ''' <summary>Compare <c>ModifiedAt</c>; push only when the local row is newer than the remote.</summary>
        LastWriteWins
        ''' <summary>INSERT only — any existing remote row causes the entry to be skipped. Used for BIR/ledger tables.</summary>
        AppendOnly
        ''' <summary>Push fails on any existing remote row; entry is marked as permanently rejected.</summary>
        Reject
    End Enum

    ''' <summary>Action the sync orchestrator should take for a single journal entry.</summary>
    Public Enum SyncAction
        ''' <summary>Write the entry to central MariaDB.</summary>
        Push
        ''' <summary>Silently discard this entry; mark it as synced.</summary>
        Skip
        ''' <summary>Record failure in the journal; do not retry after <c>MaxAttempts</c>.</summary>
        Reject
    End Enum

    ''' <summary>
    ''' Result of querying the central MariaDB for an existing row matching the journal entry.
    ''' </summary>
    Public Class RemoteRowSnapshot
        ''' <summary>True when a row with the same PK already exists in the remote table.</summary>
        Public Property Exists As Boolean
        ''' <summary><c>ModifiedAt</c> of the remote row; Nothing when the row does not exist.</summary>
        Public Property ModifiedAt As DateTime?
    End Class

    ''' <summary>Decision returned by <see cref="IConflictResolver"/> for a single journal entry.</summary>
    Public Class ResolutionDecision
        Public Property Action As SyncAction
        Public Property Reason As String
    End Class

End Namespace
