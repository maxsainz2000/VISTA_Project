Imports System.Threading
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

        Public Async Function GetPendingChangesAsync() As Task(Of IReadOnlyList(Of SyncJournal)) _
            Implements ISyncableRepository.GetPendingChangesAsync
            Dim entries = Await _journalContext.SyncJournalEntries _
                .Where(Function(j) j.ModuleName = "Inventory" AndAlso j.SyncedAt Is Nothing) _
                .OrderBy(Function(j) j.CreatedAt) _
                .ToListAsync()
            Return entries.AsReadOnly()
        End Function

        Public Async Function MarkSyncedAsync(journalIds As IEnumerable(Of Long)) As Task _
            Implements ISyncableRepository.MarkSyncedAsync
            Dim now = DateTime.UtcNow
            Dim ids = journalIds.ToList()
            Dim entries = Await _journalContext.SyncJournalEntries _
                .Where(Function(j) ids.Contains(j.Id)) _
                .ToListAsync()
            For Each entry In entries
                entry.SyncedAt = now
            Next
            Await _journalContext.SaveChangesAsync()
        End Function

    End Class

End Namespace
