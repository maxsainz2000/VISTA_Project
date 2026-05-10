Namespace Sync

    ''' <summary>
    ''' Ownership: SharedKernel/Sync layer. Evaluates the per-table conflict policy and produces
    ''' a <see cref="ResolutionDecision"/> without touching MariaDB directly — the orchestrator
    ''' supplies the pre-fetched <see cref="RemoteRowSnapshot"/>.
    ''' Policy: <c>Acc_*</c> and BIR financial tables are <see cref="ConflictResolution.AppendOnly"/>;
    ''' <c>Pur_*</c> and <c>Inv_*</c> are <see cref="ConflictResolution.LastWriteWins"/>.
    ''' </summary>
    Public Interface IConflictResolver
        ''' <summary>
        ''' Returns the action the sync orchestrator should take for <paramref name="entry"/>
        ''' given the current state of the remote row (<paramref name="remoteRow"/>).
        ''' </summary>
        Function ResolveAsync(entry As SyncJournal, remoteRow As RemoteRowSnapshot) As Task(Of ResolutionDecision)
    End Interface

    ''' <summary>
    ''' Default implementation of <see cref="IConflictResolver"/>.
    ''' Applies a static policy table: exact table-name matches take priority over prefix matches.
    ''' </summary>
    Public Class ConflictResolver
        Implements IConflictResolver

        ''' <summary>Exact table names that override the prefix rule.</summary>
        Private Shared ReadOnly _exactPolicy As New Dictionary(Of String, ConflictResolution)(StringComparer.OrdinalIgnoreCase) From {
            {"Pos_OfficialReceipts", ConflictResolution.AppendOnly},
            {"Pos_ReceiptIntegrity", ConflictResolution.AppendOnly},
            {"Pos_CreditPayments", ConflictResolution.AppendOnly}
        }

        ''' <summary>Prefix-based fallback: matched in declaration order.</summary>
        Private Shared ReadOnly _prefixPolicy As IReadOnlyList(Of KeyValuePair(Of String, ConflictResolution)) =
            New List(Of KeyValuePair(Of String, ConflictResolution)) From {
                New KeyValuePair(Of String, ConflictResolution)("Pur_", ConflictResolution.LastWriteWins),
                New KeyValuePair(Of String, ConflictResolution)("Inv_", ConflictResolution.LastWriteWins),
                New KeyValuePair(Of String, ConflictResolution)("Pos_", ConflictResolution.LastWriteWins),
                New KeyValuePair(Of String, ConflictResolution)("Acc_", ConflictResolution.AppendOnly)
            }

        Private Shared Function GetPolicy(tableName As String) As ConflictResolution
            Dim exact As ConflictResolution
            If _exactPolicy.TryGetValue(tableName, exact) Then Return exact
            For Each kv In _prefixPolicy
                If tableName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) Then Return kv.Value
            Next
            Return ConflictResolution.LastWriteWins
        End Function

        Public Function ResolveAsync(entry As SyncJournal, remoteRow As RemoteRowSnapshot) As Task(Of ResolutionDecision) Implements IConflictResolver.ResolveAsync
            Dim policy = GetPolicy(entry.TableName)
            Dim decision As ResolutionDecision

            Select Case policy
                Case ConflictResolution.AppendOnly
                    If entry.Operation = "INSERT" AndAlso Not remoteRow.Exists Then
                        decision = New ResolutionDecision With {.Action = SyncAction.Push, .Reason = "AppendOnly: new row, no remote row exists"}
                    Else
                        decision = New ResolutionDecision With {.Action = SyncAction.Skip, .Reason = "AppendOnly: row already exists or operation is not INSERT"}
                    End If

                Case ConflictResolution.LastWriteWins
                    If Not remoteRow.Exists Then
                        decision = New ResolutionDecision With {.Action = SyncAction.Push, .Reason = "LastWriteWins: no remote row"}
                    ElseIf remoteRow.ModifiedAt.HasValue AndAlso entry.CreatedAt <= remoteRow.ModifiedAt.Value Then
                        decision = New ResolutionDecision With {.Action = SyncAction.Skip, .Reason = "LastWriteWins: remote row is same age or newer"}
                    Else
                        decision = New ResolutionDecision With {.Action = SyncAction.Push, .Reason = "LastWriteWins: local row is newer"}
                    End If

                Case ConflictResolution.Reject
                    If remoteRow.Exists Then
                        decision = New ResolutionDecision With {.Action = SyncAction.Reject, .Reason = "Reject policy: a remote row already exists for this key"}
                    Else
                        decision = New ResolutionDecision With {.Action = SyncAction.Push, .Reason = "Reject policy: no remote row, safe to push"}
                    End If

                Case Else
                    decision = New ResolutionDecision With {.Action = SyncAction.Push, .Reason = "Unknown policy: defaulting to push"}
            End Select

            Return Task.FromResult(decision)
        End Function

    End Class

End Namespace
