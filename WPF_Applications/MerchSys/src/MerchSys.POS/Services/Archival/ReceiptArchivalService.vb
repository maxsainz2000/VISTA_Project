Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities

Namespace Services.Archival

    ''' <summary>
    ''' Background service that moves expired <see cref="OfficialReceipt"/> and
    ''' <see cref="ReceiptIntegrity"/> rows into archive tables on a periodic schedule.
    ''' <para>
    ''' Implements both <see cref="IReceiptArchivalService"/> (for manual or test invocation)
    ''' and <see cref="IHostedService"/> (for the scheduled timer).
    ''' </para>
    ''' </summary>
    Public Class ReceiptArchivalService
        Implements IReceiptArchivalService, IHostedService

        Private ReadOnly _scopeFactory As IServiceScopeFactory
        Private ReadOnly _options As IOptions(Of ReceiptArchivalOptions)
        Private ReadOnly _logger As ILogger(Of ReceiptArchivalService)
        Private ReadOnly _configuration As IConfiguration

        Private _timer As PeriodicTimer
        Private _backgroundTask As Task
        Private _cts As CancellationTokenSource

        Public Sub New(scopeFactory As IServiceScopeFactory,
                       options As IOptions(Of ReceiptArchivalOptions),
                       logger As ILogger(Of ReceiptArchivalService),
                       configuration As IConfiguration)
            _scopeFactory = scopeFactory
            _options = options
            _logger = logger
            _configuration = configuration
        End Sub

        ' ── IHostedService ──────────────────────────────────────────────────────

        Public Function StartAsync(cancellationToken As CancellationToken) As Task Implements IHostedService.StartAsync
            If Not _options.Value.Enabled Then
                _logger.LogInformation("ReceiptArchivalService is disabled via Receipts:Archival:Enabled")
                Return Task.CompletedTask
            End If

            _cts = New CancellationTokenSource()
            _timer = New PeriodicTimer(TimeSpan.FromHours(_options.Value.IntervalHours))
            _backgroundTask = Task.Run(AddressOf RunArchivalLoopAsync, _cts.Token)
            _logger.LogInformation("ReceiptArchivalService started — interval {Hours}h, batch {Batch}",
                                   _options.Value.IntervalHours, _options.Value.BatchSize)
            Return Task.CompletedTask
        End Function

        Public Async Function StopAsync(cancellationToken As CancellationToken) As Task Implements IHostedService.StopAsync
            If _cts IsNot Nothing Then _cts.Cancel()
            _timer?.Dispose()

            If _backgroundTask IsNot Nothing Then
                Dim stopEx As Exception = Nothing
                Try
                    Await _backgroundTask
                Catch ex As OperationCanceledException
                    ' Expected on clean shutdown — do not rethrow.
                Catch ex As Exception
                    stopEx = ex
                End Try
                If stopEx IsNot Nothing Then
                    _logger.LogError(stopEx, "ReceiptArchivalService background task faulted during stop")
                End If
            End If

            _logger.LogInformation("ReceiptArchivalService stopped")
        End Function

        ' ── Timer loop ──────────────────────────────────────────────────────────

        Private Async Function RunArchivalLoopAsync() As Task
            Try
                While Await _timer.WaitForNextTickAsync(_cts.Token)
                    Dim tickEx As Exception = Nothing
                    Dim result As ReceiptArchivalBatchResult = Nothing

                    Try
                        result = Await ArchiveEligibleAsync(
                            DateTime.UtcNow, _options.Value.BatchSize, _cts.Token)
                    Catch ex As OperationCanceledException
                        Return
                    Catch ex As Exception
                        tickEx = ex
                    End Try

                    If tickEx IsNot Nothing Then
                        _logger.LogError(tickEx, "ReceiptArchivalService batch failed — will retry next tick")
                    ElseIf result IsNot Nothing Then
                        LogBatchResult(result)
                    End If
                End While
            Catch ex As OperationCanceledException
                ' Normal shutdown path from WaitForNextTickAsync cancellation.
            End Try
        End Function

        Private Sub LogBatchResult(result As ReceiptArchivalBatchResult)
            _logger.LogInformation(
                "ReceiptArchivalService batch complete — " &
                "Receipts={Receipts} IntegrityRows={IntegrityRows} " &
                "Window=[{Earliest:yyyy-MM-dd},{Latest:yyyy-MM-dd}] " &
                "Duration={Duration}ms HadMore={HadMore}",
                result.ReceiptsMoved,
                result.IntegrityRowsMoved,
                result.EarliestArchivedReceiptDate,
                result.LatestArchivedReceiptDate,
                result.DurationMs,
                result.HadMoreEligible)
        End Sub

        ' ── IReceiptArchivalService ──────────────────────────────────────────────

        ''' <inheritdoc/>
        Public Async Function ArchiveEligibleAsync(
            asOfUtc As DateTime,
            batchSize As Integer,
            cancellationToken As CancellationToken) As Task(Of ReceiptArchivalBatchResult) Implements IReceiptArchivalService.ArchiveEligibleAsync

            If Not _options.Value.Enabled Then
                Return New ReceiptArchivalBatchResult()
            End If

            Dim graceDays As Integer = 0
            Dim graceDaysRaw = _configuration("Bir:ArchivePolicy:GraceDays")
            If Not String.IsNullOrEmpty(graceDaysRaw) Then
                Integer.TryParse(graceDaysRaw, graceDays)
            End If

            Dim archiveCutoff = asOfUtc.AddDays(-graceDays)
            Dim currentFiscalYear = asOfUtc.Year
            Dim sw = Stopwatch.StartNew()

            Using scope = _scopeFactory.CreateScope()
                Dim db = scope.ServiceProvider.GetRequiredService(Of POSDbContext)()
                Return Await RunBatchAsync(db, asOfUtc, archiveCutoff, currentFiscalYear, batchSize, sw, cancellationToken)
            End Using
        End Function

        ' ── Core batch logic ────────────────────────────────────────────────────

        Private Async Function RunBatchAsync(
            db As POSDbContext,
            asOfUtc As DateTime,
            archiveCutoff As DateTime,
            currentFiscalYear As Integer,
            batchSize As Integer,
            sw As Stopwatch,
            cancellationToken As CancellationToken) As Task(Of ReceiptArchivalBatchResult)

            ' Keep the connection alive across the session flag + transaction so a single
            ' connection context spans the flag set, deletes, and flag clear.
            Await db.Database.OpenConnectionAsync(cancellationToken)

            Dim transaction = Await db.Database.BeginTransactionAsync(cancellationToken)
            Dim batchEx As Exception = Nothing
            Dim result As New ReceiptArchivalBatchResult()

            Try
                ' ── 1. Query eligible receipts ────────────────────────────────
                Dim candidateCount = batchSize + 1    ' +1 to detect HadMoreEligible
                Dim candidates = Await (
                    From r In db.OfficialReceipts
                    Join ri In db.ReceiptIntegrities On ri.ReceiptId Equals r.Id
                    Where ri.RetentionExpiresAt < archiveCutoff AndAlso
                          r.IssueDate.Year < currentFiscalYear
                    Order By r.IssueDate Ascending
                    Select New With {
                        Key .Receipt = r,
                        Key .Integrity = ri
                    }).Take(candidateCount).ToListAsync(cancellationToken)

                result.HadMoreEligible = candidates.Count > batchSize
                Dim batch = If(result.HadMoreEligible, candidates.Take(batchSize).ToList(), candidates)

                If batch.Count = 0 Then
                    sw.Stop()
                    result.DurationMs = sw.ElapsedMilliseconds
                    Await transaction.RollbackAsync(cancellationToken)
                    Return result
                End If

                ' ── 2. Insert archive copies ──────────────────────────────────
                For Each item In batch
                    db.OfficialReceiptArchives.Add(New OfficialReceiptArchive() With {
                        .OriginalReceiptId = item.Receipt.Id,
                        .TransactionId = item.Receipt.TransactionId,
                        .ReceiptNumber = item.Receipt.ReceiptNumber,
                        .BusinessName = item.Receipt.BusinessName,
                        .BusinessAddress = item.Receipt.BusinessAddress,
                        .BusinessTIN = item.Receipt.BusinessTIN,
                        .IssueDate = item.Receipt.IssueDate,
                        .Items = item.Receipt.Items,
                        .TotalAmount = item.Receipt.TotalAmount,
                        .VatAmount = item.Receipt.VatAmount,
                        .IsVatRegistered = item.Receipt.IsVatRegistered,
                        .ArchivedAt = asOfUtc,
                        .ArchivedHash = item.Integrity.IntegrityHash
                    })

                    db.ReceiptIntegrityArchives.Add(New ReceiptIntegrityArchive() With {
                        .OriginalIntegrityId = item.Integrity.Id,
                        .ReceiptId = item.Receipt.Id,
                        .IntegrityHash = item.Integrity.IntegrityHash,
                        .PreviousHash = item.Integrity.PreviousHash,
                        .RetentionExpiresAt = item.Integrity.RetentionExpiresAt,
                        .IsImmutable = item.Integrity.IsImmutable,
                        .HashAlgorithm = item.Integrity.HashAlgorithm,
                        .CanonicalPayload = item.Integrity.CanonicalPayload,
                        .ArchivedAt = asOfUtc,
                        .ArchivedByService = "ReceiptArchivalService"
                    })
                Next

                ' Insert archive rows before any deletes — a failed insert rolls back
                ' cleanly without touching the live tables (see plan: insert-then-delete).
                Await db.SaveChangesAsync(cancellationToken)

                ' ── 3. Set archival session flag so the DELETE trigger allows the ops ─
                ' The trigger pos_receipts_no_delete reads Pos_ArchivalSession before deciding
                ' whether to RAISE(ABORT).  The 5-minute TTL provides crash-safety: if the
                ' service terminates mid-batch the flag expires and the trigger re-engages.
                Await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO `Pos_ArchivalSession` (`key`, `value`, `expires_at`) " &
                    "VALUES ('archival_in_progress', 1, DATE_ADD(NOW(6), INTERVAL 5 MINUTE)) " &
                    "ON DUPLICATE KEY UPDATE `value` = 1, `expires_at` = DATE_ADD(NOW(6), INTERVAL 5 MINUTE)",
                    cancellationToken)

                ' ── 4. Delete source rows (bypasses ImmutableReceiptInterceptor via raw SQL) ─
                ' Delete ReceiptIntegrity first to respect the FK that references OfficialReceipts.
                For Each item In batch
                    Await db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}",
                        {item.Receipt.Id},
                        cancellationToken)
                Next

                For Each item In batch
                    Await db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM `Pos_OfficialReceipts` WHERE `Id` = {0}",
                        {item.Receipt.Id},
                        cancellationToken)
                Next

                ' ── 5. Clear session flag ─────────────────────────────────────
                Await db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM `Pos_ArchivalSession` WHERE `key` = 'archival_in_progress'",
                    cancellationToken)

                Await transaction.CommitAsync(cancellationToken)

                ' ── 6. Populate result ────────────────────────────────────────
                sw.Stop()
                result.ReceiptsMoved = batch.Count
                result.IntegrityRowsMoved = batch.Count
                result.EarliestArchivedReceiptDate = batch.First().Receipt.IssueDate
                result.LatestArchivedReceiptDate = batch.Last().Receipt.IssueDate
                result.DurationMs = sw.ElapsedMilliseconds

            Catch ex As Exception
                batchEx = ex
            End Try

            ' Awaiting rollback outside the Try block: VB.NET does not allow Await in Catch.
            If batchEx IsNot Nothing Then
                Dim rollbackEx As Exception = Nothing
                Try
                    Await transaction.RollbackAsync()
                Catch ex As Exception
                    rollbackEx = ex
                End Try
                If rollbackEx IsNot Nothing Then
                    _logger.LogError(rollbackEx, "ReceiptArchivalService rollback failed")
                End If
                Throw batchEx
            End If

            Await db.Database.CloseConnectionAsync()
            Return result
        End Function

    End Class

End Namespace
