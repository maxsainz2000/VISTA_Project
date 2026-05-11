Imports System.Threading
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Data

    ''' <summary>
    ''' Producer-side sync repository for the POS module.
    ''' Delegates all change-capture and journal-append logic to
    ''' <see cref="SyncableRepositoryCore"/>; this class is intentionally thin.
    ''' </summary>
    Public Class PosSyncableRepository
        Implements ISyncableRepository(Of POSDbContext)

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _journalContext As SyncJournalDbContext

        Public Sub New(context As POSDbContext, journalContext As SyncJournalDbContext)
            _context = context
            _journalContext = journalContext
        End Sub

        Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
            Implements ISyncableRepository(Of POSDbContext).GetTrackedChangeDescriptors
            Return SyncableRepositoryCore.CaptureDescriptors(_context)
        End Function

        Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
            Implements ISyncableRepository(Of POSDbContext).SaveChangesWithJournalAsync
            Return SyncableRepositoryCore.SaveWithJournalAsync(_context, _journalContext, "POS", cancellationToken)
        End Function

    End Class

End Namespace
