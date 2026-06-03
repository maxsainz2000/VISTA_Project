Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Interfaces

Namespace Persistence

    ''' <summary>
    ''' Canonical primitive for the VISTA optimistic-concurrency conflict UX.
    '''
    ''' VISTA runs up to 4 client laptops against one centralized MariaDB instance, so a
    ''' <see cref="DbUpdateConcurrencyException"/> on a guarded write is a *normal* runtime
    ''' condition. The mandated handling (CLAUDE.md / centralized-database-architecture) is:
    ''' surface "Data changed elsewhere — refresh and retry", offer Refresh/Cancel, and
    ''' **never silently overwrite** another client's change.
    '''
    ''' This module exposes the single correct way to do that. UX-14 standardizes all
    ''' write-path ViewModels onto it (today they inline an equivalent pattern).
    ''' </summary>
    Public Module ConcurrencyHelper

        ''' <summary>
        ''' Executes a database-mutating operation. On a <see cref="DbUpdateConcurrencyException"/>
        ''' it prompts the operator via <see cref="IConflictPresenter"/>; if the operator chooses
        ''' Refresh, <paramref name="onRefresh"/> reloads live values. The stale write is **never**
        ''' re-applied — the conflict is surfaced, not swallowed.
        '''
        ''' VB.NET note: the prompt is awaited *after* the Try/Catch (a conflict flag is captured
        ''' inside the Catch), because <c>Await</c> is illegal inside a Catch/Finally (BC36943).
        '''
        ''' Returns True when <paramref name="work"/> completed without a concurrency conflict;
        ''' False when a conflict was caught and surfaced. Exceptions other than
        ''' <see cref="DbUpdateConcurrencyException"/> propagate to the caller unchanged, so the
        ''' caller keeps its own validation / domain-error handling.
        ''' </summary>
        Public Async Function ExecuteWithConflictPromptAsync(work As Func(Of Task),
                                                             onRefresh As Func(Of Task),
                                                             conflictPresenter As IConflictPresenter) As Task(Of Boolean)
            Dim isConflict As Boolean = False
            Try
                Await work()
            Catch ex As DbUpdateConcurrencyException
                isConflict = True
            End Try

            If isConflict Then
                Dim shouldRefresh = Await conflictPresenter.PromptAsync()
                If shouldRefresh Then
                    Await onRefresh()
                End If
                Return False
            End If

            Return True
        End Function

    End Module

End Namespace
