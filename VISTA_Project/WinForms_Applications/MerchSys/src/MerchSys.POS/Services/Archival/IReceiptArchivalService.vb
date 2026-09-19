Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Archival

    ''' <summary>
    ''' Moves eligible <see cref="Entities.OfficialReceipt"/> and
    ''' <see cref="Entities.ReceiptIntegrity"/> rows into their respective archive tables once the
    ''' BIR retention window plus the configured grace period has elapsed.
    ''' <para>
    ''' <b>Fiscal-year guard:</b> receipts issued in the current calendar year are never archived,
    ''' even if <c>RetentionExpiresAt + GraceDays</c> is in the past. This prevents a
    ''' misconfigured short-retention value from silently archiving active-year data.
    ''' </para>
    ''' <para>
    ''' <b>Insert-then-delete ordering:</b> archive copies are inserted and committed before
    ''' source rows are deleted. An archive insert failure rolls back the entire batch and
    ''' leaves the live tables untouched.
    ''' </para>
    ''' </summary>
    Public Interface IReceiptArchivalService

        ''' <summary>
        ''' Archives all eligible receipts older than <paramref name="asOfUtc"/> minus the
        ''' configured grace window, up to <paramref name="batchSize"/> rows.
        ''' </summary>
        Function ArchiveEligibleAsync(
            asOfUtc As DateTime,
            batchSize As Integer,
            cancellationToken As CancellationToken
        ) As Task(Of ReceiptArchivalBatchResult)

    End Interface

    ''' <summary>
    ''' Summary of a single archival batch produced by
    ''' <see cref="IReceiptArchivalService.ArchiveEligibleAsync"/>.
    ''' </summary>
    Public Class ReceiptArchivalBatchResult

        ''' <summary>Number of <c>Pos_OfficialReceipts</c> rows moved to archive.</summary>
        Public Property ReceiptsMoved As Integer

        ''' <summary>Number of <c>Pos_ReceiptIntegrity</c> rows moved to archive.</summary>
        Public Property IntegrityRowsMoved As Integer

        ''' <summary>IssueDate of the oldest receipt archived in this batch.</summary>
        Public Property EarliestArchivedReceiptDate As DateTime?

        ''' <summary>IssueDate of the most recent receipt archived in this batch.</summary>
        Public Property LatestArchivedReceiptDate As DateTime?

        ''' <summary>Wall-clock milliseconds the batch took from start to commit.</summary>
        Public Property DurationMs As Long

        ''' <summary>
        ''' <c>True</c> when the eligibility query returned more rows than
        ''' <c>batchSize</c>, indicating another run is needed to drain the backlog.
        ''' </summary>
        Public Property HadMoreEligible As Boolean

    End Class

End Namespace
