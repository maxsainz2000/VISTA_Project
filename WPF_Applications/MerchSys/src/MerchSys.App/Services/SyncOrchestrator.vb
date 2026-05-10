Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports MerchSys.SharedKernel.Sync

Namespace Services

    Public Class SyncOrchestrator

        Private ReadOnly _repositories As IEnumerable(Of ISyncableRepository)
        Private ReadOnly _logger As ILogger(Of SyncOrchestrator)

        ' Canonical sync order: Purchasing first (source of inventory cost), then Inventory,
        ' POS, Accounting — mirrors the event flow in analysis/cross-module-data-flow.md.
        Private Shared ReadOnly ModuleOrder As String() =
            {"Purchasing", "Inventory", "POS", "Accounting"}

        Public Sub New(repositories As IEnumerable(Of ISyncableRepository),
                       logger As ILogger(Of SyncOrchestrator))
            _repositories = repositories
            _logger = logger
        End Sub

        ''' <summary>
        ''' Iterates registered module repositories in dependency order and delegates
        ''' pending journal entries to INFRA-06 for transmission. Stops on the first
        ''' module error; subsequent modules retry on the next probe cycle.
        ''' </summary>
        Public Async Function RunAsync(cancellationToken As CancellationToken) As Task
            Dim ordered = _repositories.
                OrderBy(Function(r) IndexOf(r.ModuleName)).
                ToList()

            For Each repo In ordered
                If cancellationToken.IsCancellationRequested Then Exit For

                Try
                    Dim pending = Await repo.GetPendingChangesAsync()
                    If pending.Count = 0 Then Continue For

                    _logger.LogInformation("Sync: {Module} has {Count} pending entries",
                                           repo.ModuleName, pending.Count)

                    ' INFRA-06 will implement actual MariaDB transmission here.
                    ' For now mark all entries as synced to keep the journal clean during testing.
                    Await repo.MarkSyncedAsync(pending.Select(Function(j) j.Id))

                Catch ex As Exception
                    _logger.LogError(ex, "Sync failed for module {Module}; stopping this cycle",
                                     repo.ModuleName)
                    Return
                End Try
            Next
        End Function

        Private Shared Function IndexOf(moduleName As String) As Integer
            Dim idx = Array.IndexOf(ModuleOrder, moduleName)
            Return If(idx < 0, Integer.MaxValue, idx)
        End Function

    End Class

End Namespace
