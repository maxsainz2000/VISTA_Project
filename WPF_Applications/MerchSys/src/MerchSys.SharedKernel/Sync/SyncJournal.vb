Imports MerchSys.SharedKernel.Entities

Namespace Sync

    ''' <summary>
    ''' Append-only record of every local write that must be replicated to the central
    ''' MariaDB instance. Each module appends a row when it persists data locally.
    ''' The sync worker sets <see cref="SyncedAt"/> on successful transmission.
    ''' <para>
    ''' Lives in <c>Sync_Journal</c> (shared SQLite file) so any module can write to it
    ''' without taking a dependency on another module's DbContext.
    ''' </para>
    ''' </summary>
    Public Class SyncJournal
        Inherits AuditableEntity

        Public Property TableName As String
        Public Property RowId As Long
        ''' <summary>"INSERT", "UPDATE", or "DELETE".</summary>
        Public Property Operation As String
        ''' <summary>JSON snapshot of the affected row at the time of the operation.</summary>
        Public Property Payload As String
        Public Property AttemptCount As Integer
        ''' <summary>Last error message from a failed sync attempt; Nothing if never attempted or last attempt succeeded.</summary>
        Public Property LastError As String
        ''' <summary>UTC timestamp of successful replication; Nothing while pending.</summary>
        Public Property SyncedAt As DateTime?
        ''' <summary>"POS", "Inventory", "Purchasing", or "Accounting".</summary>
        Public Property ModuleName As String

    End Class

End Namespace
