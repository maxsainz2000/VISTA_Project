Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Data

    ''' <summary>
    ''' Producer-side sync repository for the Inventory module.
    ''' Delegates all change-capture and journal-append logic to
    ''' <see cref="SyncableRepositoryCore"/>; this class is intentionally thin.
    ''' Implements both the generic write-path interface and the non-generic consumer interface
    ''' used by <see cref="MerchSys.App.Services.SyncOrchestrator"/>.
    ''' </summary>
    Public Class InventorySyncableRepository
        Implements ISyncableRepository(Of InventoryDbContext)
        Implements ISyncableRepository

        Private ReadOnly _context As InventoryDbContext
        Private ReadOnly _journalContext As SyncJournalDbContext

        Public Sub New(context As InventoryDbContext, journalContext As SyncJournalDbContext)
            _context = context
            _journalContext = journalContext
        End Sub

        Public ReadOnly Property ModuleName As String Implements ISyncableRepository.ModuleName
            Get
                Return "Inventory"
            End Get
        End Property

        Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
            Implements ISyncableRepository(Of InventoryDbContext).GetTrackedChangeDescriptors
            Return SyncableRepositoryCore.CaptureDescriptors(_context)
        End Function

        Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
            Implements ISyncableRepository(Of InventoryDbContext).SaveChangesWithJournalAsync
            Return SyncableRepositoryCore.SaveWithJournalAsync(_context, _journalContext, "Inventory", cancellationToken)
        End Function

        Public Function GetPendingChangesAsync() As Task(Of IReadOnlyList(Of SyncJournal)) _
            Implements ISyncableRepository.GetPendingChangesAsync
            Dim result As New List(Of SyncJournal)()
            Dim connStr = _journalContext.Database.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "SELECT Id, TableName, RowId, Operation, Payload, AttemptCount, " &
                        "LastError, SyncedAt, ModuleName, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                        "FROM Sync_Journal WHERE ModuleName = @m AND SyncedAt IS NULL ORDER BY CreatedAt"
                    cmd.Parameters.AddWithValue("@m", "Inventory")
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim j As New SyncJournal()
                            j.Id = reader.GetInt32(0)
                            j.TableName = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                            j.RowId = reader.GetInt64(2)
                            j.Operation = If(reader.IsDBNull(3), Nothing, reader.GetString(3))
                            j.Payload = If(reader.IsDBNull(4), Nothing, reader.GetString(4))
                            j.AttemptCount = reader.GetInt32(5)
                            j.LastError = If(reader.IsDBNull(6), Nothing, reader.GetString(6))
                            j.SyncedAt = If(reader.IsDBNull(7), CType(Nothing, DateTime?), reader.GetDateTime(7))
                            j.ModuleName = If(reader.IsDBNull(8), Nothing, reader.GetString(8))
                            j.CreatedBy = If(reader.IsDBNull(9), Nothing, reader.GetString(9))
                            j.CreatedAt = reader.GetDateTime(10)
                            j.ModifiedBy = If(reader.IsDBNull(11), Nothing, reader.GetString(11))
                            j.ModifiedAt = If(reader.IsDBNull(12), CType(Nothing, DateTime?), reader.GetDateTime(12))
                            result.Add(j)
                        End While
                    End Using
                End Using
            End Using
            Return Task.FromResult(CType(result.AsReadOnly(), IReadOnlyList(Of SyncJournal)))
        End Function

        Public Async Function MarkSyncedAsync(journalIds As IEnumerable(Of Long)) As Task _
            Implements ISyncableRepository.MarkSyncedAsync
            Dim now = DateTime.UtcNow
            For Each jid In journalIds
                Await _journalContext.Database.ExecuteSqlRawAsync(
                    "UPDATE Sync_Journal SET SyncedAt = {0} WHERE Id = {1}",
                    now, CInt(jid))
            Next
        End Function

    End Class

End Namespace
