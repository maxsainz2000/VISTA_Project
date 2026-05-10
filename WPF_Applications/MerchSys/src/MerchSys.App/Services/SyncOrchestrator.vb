Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync
Imports MerchSys.SharedKernel.Sync.SyncMaps

Namespace Services

    Public Class SyncOrchestrator

        Private ReadOnly _repositories As IEnumerable(Of ISyncableRepository)
        Private ReadOnly _mariaDb As MariaDbSyncContext
        Private ReadOnly _journalDb As SyncJournalDbContext
        Private ReadOnly _conflictResolver As IConflictResolver
        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _settings As IOptionsMonitor(Of SyncSettings)
        Private ReadOnly _logger As ILogger(Of SyncOrchestrator)

        ' Canonical sync order: Purchasing first (source of inventory cost), then Inventory,
        ' POS, Accounting — mirrors the event flow in analysis/cross-module-data-flow.md.
        Private Shared ReadOnly ModuleOrder As String() =
            {"Purchasing", "Inventory", "POS", "Accounting"}

        Public Sub New(repositories As IEnumerable(Of ISyncableRepository),
                       mariaDb As MariaDbSyncContext,
                       journalDb As SyncJournalDbContext,
                       conflictResolver As IConflictResolver,
                       notifications As INotificationService,
                       settings As IOptionsMonitor(Of SyncSettings),
                       logger As ILogger(Of SyncOrchestrator))
            _repositories = repositories
            _mariaDb = mariaDb
            _journalDb = journalDb
            _conflictResolver = conflictResolver
            _notifications = notifications
            _settings = settings
            _logger = logger
        End Sub

        ''' <summary>
        ''' Iterates registered module repositories in dependency order (Purchasing → Inventory →
        ''' POS → Accounting), applies conflict resolution, and pushes pending journal entries to
        ''' the central MariaDB instance. Stops on the first module-level error so that subsequent
        ''' modules retry on the next probe cycle.
        ''' </summary>
        Public Async Function RunAsync(cancellationToken As CancellationToken) As Task
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

            Dim syncedIds As New List(Of Long)

            For Each entry In actionable
                If cancellationToken.IsCancellationRequested Then Exit For

                ' Collected outside Try/Catch because Await is not permitted inside Catch in VB.NET.
                Dim pendingFailureReason As String = Nothing

                Try
                    Dim remoteRow = Await _mariaDb.FetchRemoteRowAsync(entry.TableName, entry.RowId)
                    Dim decision = Await _conflictResolver.ResolveAsync(entry, remoteRow)

                    Select Case decision.Action
                        Case SyncAction.Push
                            Await PushEntryAsync(entry, cancellationToken)
                            syncedIds.Add(CLng(entry.Id))
                            _logger.LogDebug("Sync: pushed {Table}/{Id} — {Reason}",
                                             entry.TableName, entry.RowId, decision.Reason)

                        Case SyncAction.Skip
                            syncedIds.Add(CLng(entry.Id))
                            _logger.LogDebug("Sync: skipped {Table}/{Id} — {Reason}",
                                             entry.TableName, entry.RowId, decision.Reason)

                        Case SyncAction.Reject
                            _logger.LogWarning("Sync: rejected {Table}/{Id} — {Reason}",
                                               entry.TableName, entry.RowId, decision.Reason)
                            pendingFailureReason = decision.Reason
                    End Select

                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    _logger.LogError(ex, "Sync: failed to process {Table}/{Id}", entry.TableName, entry.RowId)
                    pendingFailureReason = ex.Message
                End Try

                If pendingFailureReason IsNot Nothing Then
                    Await IncrementAttemptAsync(entry, pendingFailureReason)
                End If
            Next

            If syncedIds.Count > 0 Then
                Await repo.MarkSyncedAsync(syncedIds)
                _logger.LogInformation("Sync: {Module} marked {Count} entries as synced", repo.ModuleName, syncedIds.Count)
            End If
        End Function

        Private Async Function PushEntryAsync(entry As SyncJournal, cancellationToken As CancellationToken) As Task
            Dim remoteEntity = ToRemoteEntity(entry)
            If remoteEntity Is Nothing Then
                _logger.LogWarning("Sync: no SyncMap found for table {Table}; skipping", entry.TableName)
                Return
            End If

            Select Case entry.Operation
                Case "INSERT"
                    _mariaDb.Add(remoteEntity)
                Case "UPDATE"
                    _mariaDb.Update(remoteEntity)
                Case Else
                    _logger.LogWarning("Sync: unhandled operation '{Op}' for {Table}; skipping",
                                       entry.Operation, entry.TableName)
                    Return
            End Select

            Await _mariaDb.SaveChangesAsync(cancellationToken)
            _mariaDb.ChangeTracker.Clear()
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

        Private Shared Function ToRemoteEntity(entry As SyncJournal) As Object
            If entry.TableName.StartsWith("Pur_", StringComparison.OrdinalIgnoreCase) Then
                Return PurchasingSyncMap.ToRemote(entry)
            ElseIf entry.TableName.StartsWith("Inv_", StringComparison.OrdinalIgnoreCase) Then
                Return InventorySyncMap.ToRemote(entry)
            ElseIf entry.TableName.StartsWith("Pos_", StringComparison.OrdinalIgnoreCase) Then
                Return PosSyncMap.ToRemote(entry)
            ElseIf entry.TableName.StartsWith("Acc_", StringComparison.OrdinalIgnoreCase) Then
                Return AccountingSyncMap.ToRemote(entry)
            End If
            Return Nothing
        End Function

        Private Shared Function IndexOf(moduleName As String) As Integer
            Dim idx = Array.IndexOf(ModuleOrder, moduleName)
            Return If(idx < 0, Integer.MaxValue, idx)
        End Function

    End Class

End Namespace
