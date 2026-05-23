Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports MySqlConnector
Imports MerchSys.SharedKernel.Sync
Imports MerchSys.SharedKernel.Sync.SyncMaps

Namespace Services.Sync

    ''' <summary>
    ''' MySqlConnector-backed <see cref="ISyncTransmitter"/> that writes pre-resolved sync journal
    ''' entries to the central MariaDB 11.4.x instance using raw parameterized SQL.
    ''' <para>
    ''' Replaces the former Pomelo/EF Core implementation to eliminate the EF Core 10/Pomelo 9
    ''' binary incompatibility. Uses <see cref="MariaDbSyncContext"/> for connection management
    ''' and SQL execution.
    ''' </para>
    ''' <para>
    ''' Processing flow: entries are grouped by <c>TableName</c>; each group is executed inside
    ''' its own transaction so a table-level failure is isolated. Within a group, each entry is
    ''' mapped to a remote POCO via the module SyncMaps, then written via parameterized
    ''' INSERT / UPDATE / DELETE and committed.
    ''' </para>
    ''' <para>
    ''' Conflict semantics at SQL level (secondary safety net; primary resolution is in the
    ''' orchestrator's <c>IConflictResolver</c>):
    ''' <list type="bullet">
    '''   <item>Non-financial tables — upsert: INSERT uses existence check then Insert or Update.</item>
    '''   <item>Financial tables (Pos_OfficialReceipts, Pos_ReceiptIntegrity,
    '''         Pos_CreditPayments, Acc_*) — reject-on-conflict: an INSERT is skipped if a
    '''         remote row already exists, preventing accidental overwrite of ledger data.</item>
    ''' </list>
    ''' </para>
    ''' <para>
    ''' Connection failures are caught, logged, and surfaced in <see cref="TransmitResult.Errors"/>
    ''' so <c>SyncWorker</c> can retry on the next probe cycle without crashing.
    ''' <c>Await</c> is never used inside <c>Catch</c> or <c>Finally</c> blocks (BC36943).
    ''' </para>
    ''' </summary>
    Public Class MariaDbSyncTransmitter
        Implements ISyncTransmitter

        Private ReadOnly _mariaDb As MariaDbSyncContext
        Private ReadOnly _logger As ILogger(Of MariaDbSyncTransmitter)

        ''' <summary>
        ''' Exact table names that must never be overwritten if a remote row already exists.
        ''' Acc_* prefix is handled separately via <see cref="IsFinancialTable"/>.
        ''' </summary>
        Private Shared ReadOnly FinancialTables As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
            "Pos_OfficialReceipts",
            "Pos_ReceiptIntegrity",
            "Pos_CreditPayments"
        }

        Public Sub New(mariaDb As MariaDbSyncContext, logger As ILogger(Of MariaDbSyncTransmitter))
            _mariaDb = mariaDb
            _logger = logger
        End Sub

        Public Async Function TransmitBatchAsync(entries As IReadOnlyList(Of SyncJournal),
                                                  cancellationToken As CancellationToken) As Task(Of TransmitResult) Implements ISyncTransmitter.TransmitBatchAsync
            If entries Is Nothing OrElse entries.Count = 0 Then Return TransmitResult.Empty
            If Not _mariaDb.IsConfigured Then Return TransmitResult.Empty

            Dim allErrors As New List(Of TransmitError)()
            Dim totalSuccess As Integer = 0

            Dim groups = entries.GroupBy(Function(e) e.TableName)

            For Each group In groups
                If cancellationToken.IsCancellationRequested Then Exit For

                Dim groupEntries = group.ToList()
                Dim groupSuccess As Integer = 0
                Dim groupErrors As New List(Of TransmitError)()
                Dim caughtException As Exception = Nothing

                Dim conn As MySqlConnection = Nothing
                Dim tx As MySqlTransaction = Nothing

                Try
                    conn = Await _mariaDb.CreateConnectionAsync(cancellationToken)
                    tx = Await conn.BeginTransactionAsync(cancellationToken)

                    Dim result = Await TransmitTableGroupAsync(group.Key, groupEntries, conn, tx, cancellationToken)
                    groupSuccess = result.SuccessCount
                    groupErrors.AddRange(result.Errors)
                    Await tx.CommitAsync(cancellationToken)
                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    caughtException = ex
                End Try

                ' Rollback and dispose outside Catch because Await is not allowed in Catch/Finally.
                If caughtException IsNot Nothing Then
                    If tx IsNot Nothing Then
                        Dim rollbackEx As Exception = Nothing
                        Try
                            Await tx.RollbackAsync(CancellationToken.None)
                        Catch rbEx As Exception
                            rollbackEx = rbEx
                        End Try
                        If rollbackEx IsNot Nothing Then
                            _logger.LogError(rollbackEx, "Sync: rollback failed for table group {Table}", group.Key)
                        End If
                    End If
                End If

                If tx IsNot Nothing Then tx.Dispose()
                If conn IsNot Nothing Then conn.Dispose()

                If caughtException IsNot Nothing Then
                    _logger.LogError(caughtException, "Sync: table group {Table} failed entirely — all {Count} entries will retry",
                                     group.Key, groupEntries.Count)
                    For Each entry In groupEntries
                        allErrors.Add(New TransmitError With {
                            .EntryId = CLng(entry.Id),
                            .TableName = entry.TableName,
                            .RowId = entry.RowId,
                            .Message = caughtException.Message
                        })
                    Next
                Else
                    totalSuccess += groupSuccess
                    allErrors.AddRange(groupErrors)
                End If
            Next

            Return New TransmitResult With {
                .SuccessCount = totalSuccess,
                .FailedCount = allErrors.Count,
                .Errors = allErrors
            }
        End Function

        Private Async Function TransmitTableGroupAsync(tableName As String,
                                                        entries As List(Of SyncJournal),
                                                        conn As MySqlConnection,
                                                        tx As MySqlTransaction,
                                                        ct As CancellationToken) As Task(Of TransmitResult)
            Dim isFinancial = IsFinancialTable(tableName)
            Dim errors As New List(Of TransmitError)()
            Dim successCount As Integer = 0

            For Each entry In entries
                If ct.IsCancellationRequested Then Exit For

                Dim caughtMsg As String = Nothing
                Try
                    Await TransmitEntryAsync(entry, isFinancial, conn, tx, ct)
                    successCount += 1
                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    caughtMsg = ex.Message
                End Try

                If caughtMsg IsNot Nothing Then
                    _logger.LogError("Sync: entry {Id} ({Table}/{Row}) failed: {Msg}",
                                     entry.Id, tableName, entry.RowId, caughtMsg)
                    errors.Add(New TransmitError With {
                        .EntryId = CLng(entry.Id),
                        .TableName = tableName,
                        .RowId = entry.RowId,
                        .Message = caughtMsg
                    })
                End If
            Next

            Return New TransmitResult With {
                .SuccessCount = successCount,
                .FailedCount = errors.Count,
                .Errors = errors
            }
        End Function

        Private Async Function TransmitEntryAsync(entry As SyncJournal,
                                                   isFinancial As Boolean,
                                                   conn As MySqlConnection,
                                                   tx As MySqlTransaction,
                                                   ct As CancellationToken) As Task
            Select Case entry.Operation.ToUpperInvariant()
                Case "INSERT", "UPDATE"
                    Dim remoteEntity = ToRemoteEntity(entry)
                    If remoteEntity Is Nothing Then
                        _logger.LogWarning("Sync: no SyncMap registered for table {Table}; entry {Id} skipped",
                                           entry.TableName, entry.Id)
                        Return
                    End If

                    If entry.Operation.ToUpperInvariant() = "INSERT" Then
                        Dim exists = (Await _mariaDb.FetchRemoteRowAsync(entry.TableName, entry.RowId)).Exists
                        If isFinancial AndAlso exists Then
                            _logger.LogWarning("Sync: financial row {Table}/{Id} already exists on central; skipping INSERT",
                                               entry.TableName, entry.RowId)
                            Return
                        End If
                        If exists Then
                            Await _mariaDb.ExecuteUpdateAsync(entry.TableName, remoteEntity, conn, tx, ct)
                        Else
                            Await _mariaDb.ExecuteInsertAsync(entry.TableName, remoteEntity, conn, tx, ct)
                        End If
                    Else
                        Await _mariaDb.ExecuteUpdateAsync(entry.TableName, remoteEntity, conn, tx, ct)
                    End If

                Case "DELETE"
                    Await _mariaDb.ExecuteDeleteAsync(entry.TableName, entry.RowId, conn, tx, ct)

                Case Else
                    _logger.LogWarning("Sync: unrecognised operation '{Op}' for {Table}/{Id}; skipping",
                                       entry.Operation, entry.TableName, entry.RowId)
            End Select
        End Function

        Private Shared Function IsFinancialTable(tableName As String) As Boolean
            Return FinancialTables.Contains(tableName) OrElse
                   tableName.StartsWith("Acc_", StringComparison.OrdinalIgnoreCase)
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

    End Class

End Namespace
