Imports System.Threading
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Data

    ''' <summary>
    ''' Producer-side sync repository for the Inventory module.
    ''' Delegates all change-capture and journal-append logic to
    ''' <see cref="SyncableRepositoryCore"/>; this class is intentionally thin.
    ''' </summary>
    Public Class InventorySyncableRepository
        Implements ISyncableRepository(Of InventoryDbContext)

        Private ReadOnly _context As InventoryDbContext
        Private ReadOnly _journalContext As SyncJournalDbContext

        Public Sub New(context As InventoryDbContext, journalContext As SyncJournalDbContext)
            _context = context
            _journalContext = journalContext
        End Sub

        Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
            Implements ISyncableRepository(Of InventoryDbContext).GetTrackedChangeDescriptors
            Return SyncableRepositoryCore.CaptureDescriptors(_context)
        End Function

        Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
            Implements ISyncableRepository(Of InventoryDbContext).SaveChangesWithJournalAsync
            Return SyncableRepositoryCore.SaveWithJournalAsync(_context, _journalContext, "Inventory", cancellationToken)
        End Function

    End Class

End Namespace
