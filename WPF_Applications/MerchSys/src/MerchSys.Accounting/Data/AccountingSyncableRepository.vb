Imports System.Threading
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Data

    ''' <summary>
    ''' Producer-side sync repository for the Accounting module.
    ''' Delegates all change-capture and journal-append logic to
    ''' <see cref="SyncableRepositoryCore"/>; this class is intentionally thin.
    ''' <para>
    ''' <see cref="MerchSys.Accounting.Entities.TamperAuditEntry"/> rows are excluded
    ''' from journalling via the <c>&lt;NoSync&gt;</c> attribute on that class.
    ''' </para>
    ''' </summary>
    Public Class AccountingSyncableRepository
        Implements ISyncableRepository(Of AccountingDbContext)

        Private ReadOnly _context As AccountingDbContext
        Private ReadOnly _journalContext As SyncJournalDbContext

        Public Sub New(context As AccountingDbContext, journalContext As SyncJournalDbContext)
            _context = context
            _journalContext = journalContext
        End Sub

        Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
            Implements ISyncableRepository(Of AccountingDbContext).GetTrackedChangeDescriptors
            Return SyncableRepositoryCore.CaptureDescriptors(_context)
        End Function

        Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
            Implements ISyncableRepository(Of AccountingDbContext).SaveChangesWithJournalAsync
            Return SyncableRepositoryCore.SaveWithJournalAsync(_context, _journalContext, "Accounting", cancellationToken)
        End Function

    End Class

End Namespace
