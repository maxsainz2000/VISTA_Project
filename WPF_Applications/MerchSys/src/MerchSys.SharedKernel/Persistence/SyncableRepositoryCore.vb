Imports System.Reflection
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports MerchSys.SharedKernel.Sync

Namespace Persistence

    ''' <summary>
    ''' Static implementation core shared by all four module
    ''' <see cref="ISyncableRepository(Of TContext)"/> classes.
    ''' Module implementations delegate here; this keeps all change-capture and
    ''' journal-mapping logic in one place without requiring inheritance.
    ''' </summary>
    Public Module SyncableRepositoryCore

        Private ReadOnly ExcludedByTableName As HashSet(Of String) =
            New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                "Sync_Journal",
                "__EFMigrationsHistory"
            }

        Private ReadOnly SerializerOptions As New JsonSerializerOptions With {
            .WriteIndented = False
        }

        ''' <summary>
        ''' Captures descriptors for all currently tracked changes without persisting anything.
        ''' Used by <see cref="ISyncableRepository(Of TContext).GetTrackedChangeDescriptors"/>.
        ''' </summary>
        Public Function CaptureDescriptors(context As DbContext) As IReadOnlyList(Of SyncJournalDescriptor)
            ' DetectChanges must be called explicitly here because auto-detect may not have
            ' fired yet when entity properties were set via direct assignment rather than
            ' through EF proxy notifications, or when AutoDetectChangesEnabled is False.
            context.ChangeTracker.DetectChanges()
            Dim captures = CollectPreSaveInfo(context)
            Return BuildDescriptors(captures)
        End Function

        ''' <summary>
        ''' Saves module data, then appends journal rows.
        ''' If the data save throws, nothing is journalled.
        ''' If the journal append throws after a successful data save, an
        ''' <see cref="InvalidOperationException"/> is raised with the original exception as
        ''' inner so the caller is aware that data was committed but journalling failed.
        ''' </summary>
        Public Async Function SaveWithJournalAsync(
            context As DbContext,
            journalContext As SyncJournalDbContext,
            moduleName As String,
            cancellationToken As CancellationToken
        ) As Task(Of Integer)

            ' DetectChanges must be explicit — same reasoning as CaptureDescriptors above.
            context.ChangeTracker.DetectChanges()

            Dim captures = CollectPreSaveInfo(context)

            ' Step 1: persist module data.  Any exception propagates; nothing is journalled.
            Dim rowCount = Await context.SaveChangesAsync(cancellationToken)

            ' Step 2: build descriptors now so Added entries have their DB-generated PKs.
            Dim descriptors = BuildDescriptors(captures)
            If descriptors.Count = 0 Then Return rowCount

            ' Step 3: append to Sync_Journal.
            ' Await is not permitted inside Catch (BC36943), so exception state is captured
            ' outside and re-raised after the Try block.
            Dim journalException As Exception = Nothing
            Try
                Dim entries = descriptors.Select(Function(d) ToJournalEntry(d, moduleName)).ToList()
                journalContext.SyncJournalEntries.AddRange(entries)
                Await journalContext.SaveChangesAsync(cancellationToken)
            Catch ex As Exception
                journalException = ex
            End Try

            If journalException IsNot Nothing Then
                Throw New InvalidOperationException(
                    $"Module data saved but Sync_Journal append failed for module '{moduleName}'. " &
                    "Data is committed; the affected rows will not be synced until the journal is repaired.",
                    journalException)
            End If

            Return rowCount
        End Function

        ' ── private helpers ────────────────────────────────────────────────────────────────

        Private Function CollectPreSaveInfo(context As DbContext) As List(Of PreSaveInfo)
            Dim result As New List(Of PreSaveInfo)

            For Each dbEntry In context.ChangeTracker.Entries().ToList()

                Select Case dbEntry.State
                    Case EntityState.Added, EntityState.Modified, EntityState.Deleted
                        ' fall through
                    Case Else
                        Continue For
                End Select

                ' entry.Metadata.ClrType gives the original (non-proxy) entity type.
                Dim clrType = dbEntry.Metadata.ClrType
                If clrType.GetCustomAttribute(Of NoSyncAttribute)(False) IsNot Nothing Then
                    Continue For
                End If

                Dim tableName = dbEntry.Metadata.GetTableName()
                If String.IsNullOrWhiteSpace(tableName) OrElse ExcludedByTableName.Contains(tableName) Then
                    Continue For
                End If

                Dim opKind As String
                Select Case dbEntry.State
                    Case EntityState.Added : opKind = "Insert"
                    Case EntityState.Modified : opKind = "Update"
                    Case Else : opKind = "Delete"
                End Select

                ' Snapshot current (or original, for deletes) property values before save.
                ' The EntityEntry reference is retained so Added entries can be re-read for
                ' their DB-generated PK values after SaveChangesAsync returns.
                Dim snapshot As New Dictionary(Of String, Object)
                If opKind = "Delete" Then
                    For Each prop In dbEntry.Properties
                        snapshot(prop.Metadata.Name) = prop.OriginalValue
                    Next
                Else
                    For Each prop In dbEntry.Properties
                        snapshot(prop.Metadata.Name) = prop.CurrentValue
                    Next
                End If

                result.Add(New PreSaveInfo(dbEntry, opKind, tableName, snapshot))
            Next

            Return result
        End Function

        Private Function BuildDescriptors(captures As List(Of PreSaveInfo)) As IReadOnlyList(Of SyncJournalDescriptor)
            Dim result As New List(Of SyncJournalDescriptor)

            For Each cap In captures
                Dim pkDict As New Dictionary(Of String, Object)

                If cap.OperationKind = "Insert" Then
                    ' Read back primary key from the entity after save — DB-generated identity
                    ' values (auto-increment) are only populated in the entity once
                    ' SaveChangesAsync has returned.
                    For Each pkProp In cap.Entry.Metadata.FindPrimaryKey().Properties
                        pkDict(pkProp.Name) = cap.Entry.Property(pkProp.Name).CurrentValue
                    Next
                Else
                    ' For Update/Delete use the pre-save snapshot values for PK columns.
                    For Each pkProp In cap.Entry.Metadata.FindPrimaryKey().Properties
                        Dim val As Object = Nothing
                        cap.PropertySnapshot.TryGetValue(pkProp.Name, val)
                        pkDict(pkProp.Name) = val
                    Next
                End If

                Dim pkJson = JsonSerializer.Serialize(pkDict, SerializerOptions)

                Dim snapshotJson As String = Nothing
                If cap.OperationKind <> "Delete" Then
                    If cap.OperationKind = "Insert" Then
                        ' Re-read from the live entry post-save so the DB-generated PK replaces
                        ' the EF Core temporary negative key that was present in the pre-save snapshot.
                        Dim postSave As New Dictionary(Of String, Object)
                        For Each prop In cap.Entry.Properties
                            postSave(prop.Metadata.Name) = prop.CurrentValue
                        Next
                        snapshotJson = JsonSerializer.Serialize(postSave, SerializerOptions)
                    Else
                        snapshotJson = JsonSerializer.Serialize(cap.PropertySnapshot, SerializerOptions)
                    End If
                End If

                result.Add(New SyncJournalDescriptor With {
                    .TableName = cap.TableName,
                    .PrimaryKeyJson = pkJson,
                    .OperationKind = cap.OperationKind,
                    .RowSnapshotJson = snapshotJson,
                    .OccurredAt = DateTime.UtcNow
                })
            Next

            Return result.AsReadOnly()
        End Function

        Private Function ToJournalEntry(descriptor As SyncJournalDescriptor, moduleName As String) As SyncJournal
            ' Try to extract a single integer PK so SyncJournal.RowId (Long) is populated.
            ' All current entities inherit BaseEntity.Id As Integer, so this succeeds for
            ' every domain entity.  Non-integer composite PKs fall back to RowId = 0.
            Dim rowId As Long = 0
            Dim parseException As Exception = Nothing
            Try
                Using doc = JsonDocument.Parse(descriptor.PrimaryKeyJson)
                    For Each prop In doc.RootElement.EnumerateObject()
                        If prop.Value.ValueKind = JsonValueKind.Number Then
                            rowId = prop.Value.GetInt64()
                            Exit For
                        End If
                    Next
                End Using
            Catch ex As Exception
                parseException = ex
            End Try

            ' parseException is captured but intentionally not re-raised — a missing RowId
            ' means the row will not push until SyncOrchestrator is updated, but it does not
            ' justify discarding the journal entry.

            Return New SyncJournal With {
                .TableName = descriptor.TableName,
                .RowId = rowId,
                .Operation = descriptor.OperationKind.ToUpperInvariant(),
                .Payload = descriptor.RowSnapshotJson,
                .ModuleName = moduleName,
                .AttemptCount = 0,
                .SyncedAt = Nothing
            }
        End Function

    End Module

    ''' <summary>Captures the state of a single EF-tracked entry before <c>SaveChangesAsync</c>.</summary>
    Friend NotInheritable Class PreSaveInfo

        Friend ReadOnly Entry As EntityEntry
        Friend ReadOnly OperationKind As String
        Friend ReadOnly TableName As String
        Friend ReadOnly PropertySnapshot As Dictionary(Of String, Object)

        Friend Sub New(entry As EntityEntry,
                       operationKind As String,
                       tableName As String,
                       snapshot As Dictionary(Of String, Object))
            Me.Entry = entry
            Me.OperationKind = operationKind
            Me.TableName = tableName
            Me.PropertySnapshot = snapshot
        End Sub

    End Class

End Namespace
