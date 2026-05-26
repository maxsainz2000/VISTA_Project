Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports MerchSys.App.Services.Sync
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Services

    ' Replaces the INFRA-05 placeholder stub: actual data transmission is now delegated to
    ' ISyncTransmitter (MariaDbSyncTransmitter) rather than inline per-entry SaveChangesAsync calls.
    Public Class SyncOrchestrator

        Private ReadOnly _repositories As IEnumerable(Of ISyncableRepository)
        Private ReadOnly _mariaDb As MariaDbSyncContext
        Private ReadOnly _journalDb As SyncJournalDbContext
        Private ReadOnly _conflictResolver As IConflictResolver
        Private ReadOnly _transmitter As ISyncTransmitter
        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _settings As IOptionsMonitor(Of SyncSettings)
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SyncOrchestrator)

        ' Canonical sync order: Purchasing first (source of inventory cost), then Inventory,
        ' POS, Accounting — mirrors the event flow in analysis/cross-module-data-flow.md.
        Private Shared ReadOnly ModuleOrder As String() =
            {"Purchasing", "Inventory", "POS", "Accounting"}

        Public Sub New(repositories As IEnumerable(Of ISyncableRepository),
                       mariaDb As MariaDbSyncContext,
                       journalDb As SyncJournalDbContext,
                       conflictResolver As IConflictResolver,
                       transmitter As ISyncTransmitter,
                       notifications As INotificationService,
                       settings As IOptionsMonitor(Of SyncSettings),
                       writeContext As IWriteContextScope,
                       logger As ILogger(Of SyncOrchestrator))
            _repositories = repositories
            _mariaDb = mariaDb
            _journalDb = journalDb
            _conflictResolver = conflictResolver
            _transmitter = transmitter
            _notifications = notifications
            _settings = settings
            _writeContext = writeContext
            _logger = logger
        End Sub

        ''' <summary>
        ''' Iterates registered module repositories in dependency order (Purchasing → Inventory →
        ''' POS → Accounting), applies conflict resolution, batches resolved entries via
        ''' <see cref="ISyncTransmitter"/>, and marks successfully transmitted rows as synced.
        ''' Stops on the first module-level error so that subsequent modules retry on the next
        ''' probe cycle.
        ''' </summary>
        Public Async Function RunAsync(cancellationToken As CancellationToken) As Task
            Using _writeContext.Enter(WriteContextKind.System)
                Dim ordered = _repositories.
                    OrderBy(Function(r) IndexOf(r.ModuleName)).
                    ToList()

                For Each repo In ordered
                    If cancellationToken.IsCancellationRequested Then Exit For
                    Try
                        Await RunForModuleAsync(repo, cancellationToken)
                    Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                        _logger.LogError(ex, "Sync: module {Module} failed; stopping this cycle", repo.ModuleName)
                        Return
                    End Try
                Next
            End Using
        End Function

        Private Async Function RunForModuleAsync(repo As ISyncableRepository, cancellationToken As CancellationToken) As Task
            Dim maxAttempts = _settings.CurrentValue.MaxAttempts
            Dim batchSize = _settings.CurrentValue.BatchSize

            Dim allPending = Await repo.GetPendingChangesAsync()
            If allPending.Count = 0 Then Return

            Dim exhausted = allPending.Where(Function(e) e.AttemptCount >= maxAttempts).ToList()
            If exhausted.Count > 0 Then
                _logger.LogWarning("Sync: {Count} entries in {Module} have reached MaxAttempts={Max} and will not be retried",
                                   exhausted.Count, repo.ModuleName, maxAttempts)
                _notifications.NotifySyncStatusChanged(SyncStatus.[Error])
            End If

            Dim actionable = allPending.
                Where(Function(e) e.AttemptCount < maxAttempts).
                Take(batchSize).
                ToList()

            If actionable.Count = 0 Then Return

            ' Build a lookup so we can retrieve the original entity when processing TransmitResult errors.
            Dim entryById As New Dictionary(Of Long, SyncJournal)()
            For Each entry In actionable
                entryById(CLng(entry.Id)) = entry
            Next

            Dim toTransmit As New List(Of SyncJournal)()
            Dim toSkip As New List(Of Long)()

            ' ── Phase 1: conflict resolution (per-entry, requires remote snapshot) ────────
            For Each entry In actionable
                If cancellationToken.IsCancellationRequested Then Exit For

                Dim pendingFailureReason As String = Nothing

                Try
                    Dim remoteRow = Await _mariaDb.FetchRemoteRowAsync(entry.TableName, entry.RowId)
                    Dim decision = Await _conflictResolver.ResolveAsync(entry, remoteRow)

                    Select Case decision.Action
                        Case SyncAction.Push
                            toTransmit.Add(entry)
                            _logger.LogDebug("Sync: queued {Table}/{Id} for transmission — {Reason}",
                                             entry.TableName, entry.RowId, decision.Reason)

                        Case SyncAction.Skip
                            toSkip.Add(CLng(entry.Id))
                            _logger.LogDebug("Sync: skipped {Table}/{Id} — {Reason}",
                                             entry.TableName, entry.RowId, decision.Reason)

                        Case SyncAction.Reject
                            pendingFailureReason = decision.Reason
                            _logger.LogWarning("Sync: rejected {Table}/{Id} — {Reason}",
                                               entry.TableName, entry.RowId, decision.Reason)
                    End Select

                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    _logger.LogError(ex, "Sync: conflict resolution failed for {Table}/{Id}",
                                     entry.TableName, entry.RowId)
                    pendingFailureReason = ex.Message
                End Try

                If pendingFailureReason IsNot Nothing Then
                    Await IncrementAttemptAsync(entry, pendingFailureReason)
                End If
            Next

            ' ── Phase 2: batch transmission via ISyncTransmitter ─────────────────────────
            Dim transmitSuccessIds As New List(Of Long)()
            If toTransmit.Count > 0 Then
                Dim result = Await _transmitter.TransmitBatchAsync(toTransmit, cancellationToken)

                Dim failedIds As New HashSet(Of Long)(result.Errors.Select(Function(e) e.EntryId))

                For Each entry In toTransmit
                    If Not failedIds.Contains(CLng(entry.Id)) Then
                        transmitSuccessIds.Add(CLng(entry.Id))
                    End If
                Next

                For Each txErr In result.Errors
                    Dim failedEntry As SyncJournal = Nothing
                    If entryById.TryGetValue(txErr.EntryId, failedEntry) Then
                        Await IncrementAttemptAsync(failedEntry, txErr.Message)
                    End If
                Next

                If result.FailedCount > 0 Then
                    _logger.LogWarning("Sync: {Module} — {Failed} of {Total} entries failed transmission",
                                       repo.ModuleName, result.FailedCount, toTransmit.Count)
                End If
            End If

            ' ── Phase 3: mark synced (successful transmits + conflict-skipped entries) ───
            Dim syncedIds = transmitSuccessIds.Concat(toSkip).ToList()
            If syncedIds.Count > 0 Then
                Await repo.MarkSyncedAsync(syncedIds)
                _logger.LogInformation("Sync: {Module} — {Synced} entries marked synced ({Transmitted} transmitted, {Skipped} skipped)",
                                       repo.ModuleName, syncedIds.Count, transmitSuccessIds.Count, toSkip.Count)
            End If

            Dim hasErrors = toTransmit.Count > 0 AndAlso transmitSuccessIds.Count < toTransmit.Count
            _notifications.NotifySyncStatusChanged(If(hasErrors, SyncStatus.[Error], SyncStatus.Online))
        End Function

        Private Async Function IncrementAttemptAsync(entry As SyncJournal, errorMessage As String) As Task
            entry.AttemptCount += 1
            entry.LastError = If(errorMessage?.Length > 1000, errorMessage.Substring(0, 1000), errorMessage)
            _journalDb.SyncJournalEntries.Update(entry)
            Try
                Await _journalDb.SaveChangesAsync()
                _journalDb.ChangeTracker.Clear()
            Catch ex As Exception
                _logger.LogError(ex, "Sync: failed to update journal for {Table}/{Id}", entry.TableName, entry.Id)
            End Try
        End Function

        Private Shared Function IndexOf(moduleName As String) As Integer
            Dim idx = Array.IndexOf(ModuleOrder, moduleName)
            Return If(idx < 0, Integer.MaxValue, idx)
        End Function

    End Class

End Namespace
