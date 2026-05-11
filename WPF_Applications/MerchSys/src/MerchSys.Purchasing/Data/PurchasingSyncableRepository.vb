Imports System.Threading
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Data

    ''' <summary>
    ''' Producer-side sync repository for the Purchasing module.
    ''' Delegates all change-capture and journal-append logic to
    ''' <see cref="SyncableRepositoryCore"/>; this class is intentionally thin.
    ''' </summary>
    Public Class PurchasingSyncableRepository
        Implements ISyncableRepository(Of PurchasingDbContext)

        Private ReadOnly _context As PurchasingDbContext
        Private ReadOnly _journalContext As SyncJournalDbContext

        Public Sub New(context As PurchasingDbContext, journalContext As SyncJournalDbContext)
            _context = context
            _journalContext = journalContext
        End Sub

        Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
            Implements ISyncableRepository(Of PurchasingDbContext).GetTrackedChangeDescriptors
            Return SyncableRepositoryCore.CaptureDescriptors(_context)
        End Function

        Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
            Implements ISyncableRepository(Of PurchasingDbContext).SaveChangesWithJournalAsync
            Return SyncableRepositoryCore.SaveWithJournalAsync(_context, _journalContext, "Purchasing", cancellationToken)
        End Function

    End Class

End Namespace
