Namespace Persistence

    ''' <summary>
    ''' Lightweight DTO capturing a single EF-tracked change event before a module
    ''' <c>SaveChangesAsync</c> is committed.  Used internally by
    ''' <see cref="SyncableRepositoryCore"/> to build <c>Sync_Journal</c> rows and
    ''' externally by tests and the debug harness via
    ''' <see cref="ISyncableRepository(Of TContext).GetTrackedChangeDescriptors"/>.
    ''' </summary>
    Public Class SyncJournalDescriptor

        ''' <summary>Database table name as reported by EF Core metadata.</summary>
        Public Property TableName As String

        ''' <summary>JSON object whose keys are primary-key property names and whose values are the PK values.</summary>
        Public Property PrimaryKeyJson As String

        ''' <summary>"Insert", "Update", or "Delete".</summary>
        Public Property OperationKind As String

        ''' <summary>Full row snapshot serialised as JSON.  <c>Nothing</c> for Delete operations.</summary>
        Public Property RowSnapshotJson As String

        ''' <summary>UTC timestamp captured immediately after the data write succeeds.</summary>
        Public Property OccurredAt As DateTime

    End Class

End Namespace
