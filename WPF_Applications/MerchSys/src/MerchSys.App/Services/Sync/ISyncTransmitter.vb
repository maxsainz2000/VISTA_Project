Imports System.Threading
Imports MerchSys.SharedKernel.Sync

Namespace Services.Sync

    ''' <summary>
    ''' Abstraction for transmitting a pre-resolved batch of sync journal entries to the central
    ''' MariaDB instance. Callers (i.e. <c>SyncOrchestrator</c>) pass only entries whose conflict
    ''' policy has already been evaluated and resolved as <c>Push</c>.
    ''' <para>
    ''' Implementations group entries by <c>TableName</c> and commit each group in its own
    ''' transaction so a failure in one table group does not block others. Non-financial tables
    ''' use upsert semantics for idempotency; financial tables (Pos_OfficialReceipts,
    ''' Pos_ReceiptIntegrity, Pos_CreditPayments, Acc_*) use reject-on-conflict semantics —
    ''' an existing remote row causes the entry to be silently skipped rather than overwritten.
    ''' </para>
    ''' </summary>
    Public Interface ISyncTransmitter

        ''' <summary>
        ''' Transmits <paramref name="entries"/> to MariaDB. Returns a <see cref="TransmitResult"/>
        ''' with per-entry error details so the orchestrator can increment retry counters on failures
        ''' without blocking successful entries from being marked as synced.
        ''' </summary>
        Function TransmitBatchAsync(entries As IReadOnlyList(Of SyncJournal),
                                    cancellationToken As CancellationToken) As Task(Of TransmitResult)

    End Interface

    ''' <summary>Aggregate outcome of a <see cref="ISyncTransmitter.TransmitBatchAsync"/> call.</summary>
    Public Class TransmitResult

        Public Property SuccessCount As Integer
        Public Property FailedCount As Integer
        Public Property Errors As IReadOnlyList(Of TransmitError) = New List(Of TransmitError)()

        Public Shared ReadOnly Property Empty As TransmitResult
            Get
                Return New TransmitResult()
            End Get
        End Property

    End Class

    ''' <summary>Per-entry error detail returned inside <see cref="TransmitResult.Errors"/>.</summary>
    Public Class TransmitError

        Public Property EntryId As Long
        Public Property TableName As String
        Public Property RowId As Long
        Public Property Message As String

    End Class

End Namespace
