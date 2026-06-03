Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Interfaces

Namespace Persistence

    ''' <summary>
    ''' Reusable helpers for executing optimistic concurrency retry logic.
    ''' </summary>
    Public Module ConcurrencyHelper

        ''' <summary>
        ''' Executes a database-mutating operation inside a try-catch block. If a DbUpdateConcurrencyException
        ''' is caught, it shows an error toast and executes the onRefresh callback to load fresh data.
        ''' </summary>
        Public Async Function ExecuteWithConcurrencyRetryAsync(Of T)(work As Func(Of Task(Of T)),
                                                                     onRefresh As Func(Of Task),
                                                                     notifications As INotificationService) As Task(Of T)
            Dim concurrencyError = False
            Dim result As T = Nothing
            Try
                result = Await work()
            Catch ex As DbUpdateConcurrencyException
                concurrencyError = True
            End Try

            If concurrencyError Then
                notifications.ShowError("Data changed elsewhere — refreshing")
                Await onRefresh()
                Throw New DbUpdateConcurrencyException("Data changed elsewhere — refreshing")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Executes a database-mutating operation. If a DbUpdateConcurrencyException is caught,
        ''' prompts the user via IConflictPresenter. If user confirms Refresh, runs onRefresh.
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
