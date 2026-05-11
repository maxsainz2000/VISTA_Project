Imports System.Threading
Imports Microsoft.EntityFrameworkCore

Namespace Persistence

    ''' <summary>
    ''' Producer-side sync contract for a module's data layer.
    ''' <para>
    ''' Transactional guarantee: <see cref="SaveChangesWithJournalAsync"/> persists module
    ''' data first.  The <c>Sync_Journal</c> append only occurs if that write succeeds.
    ''' A thrown exception from the inner <c>SaveChangesAsync</c> leaves both the data
    ''' table and <c>Sync_Journal</c> unchanged.
    ''' </para>
    ''' <para>
    ''' This interface is distinct from <c>MerchSys.SharedKernel.Sync.ISyncableRepository</c>
    ''' (the consumer-side interface used by <c>SyncOrchestrator</c> to read pending entries).
    ''' </para>
    ''' </summary>
    Public Interface ISyncableRepository(Of TContext As DbContext)

        ''' <summary>
        ''' Persists all tracked changes via the module <c>DbContext</c> and appends
        ''' corresponding rows to <c>Sync_Journal</c> for later push to central MariaDB.
        ''' Returns the affected-row count from <c>SaveChangesAsync</c>.
        ''' </summary>
        Function SaveChangesWithJournalAsync(
            cancellationToken As CancellationToken
        ) As Task(Of Integer)

        ''' <summary>
        ''' Returns descriptors of all currently tracked changes without mutating state.
        ''' Intended for tests and the debug harness; does not persist anything.
        ''' </summary>
        Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor)

    End Interface

End Namespace
