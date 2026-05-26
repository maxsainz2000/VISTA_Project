Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Exceptions

Namespace Data

    ''' <summary>
    ''' EF Core SaveChangesInterceptor that enforces role-based write rejection (OWASP DA5).
    ''' Rejects all write attempts (Added, Modified, Deleted entries) for users in the
    ''' Owner role unless they are running within a System context or performing a self-service
    ''' password change.
    ''' </summary>
    Public Class RoleGuardInterceptor
        Inherits SaveChangesInterceptor

        Private ReadOnly _session As ISessionService
        Private ReadOnly _writeContext As IWriteContextScope

        Public Sub New(session As ISessionService, writeContext As IWriteContextScope)
            _session = session
            _writeContext = writeContext
        End Sub

        Public Overrides Function SavingChanges(
            eventData As DbContextEventData,
            result As InterceptionResult(Of Integer)) As InterceptionResult(Of Integer)

            CheckRole(eventData.Context)
            Return MyBase.SavingChanges(eventData, result)
        End Function

        Public Overrides Function SavingChangesAsync(
            eventData As DbContextEventData,
            result As InterceptionResult(Of Integer),
            Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of InterceptionResult(Of Integer))

            CheckRole(eventData.Context)
            Return MyBase.SavingChangesAsync(eventData, result, cancellationToken)
        End Function

        Private Sub CheckRole(context As DbContext)
            If context Is Nothing Then Return

            ' 1. If System write context is active, always allow the write.
            If _writeContext.Current = WriteContextKind.System Then
                Return
            End If

            ' 2. If the session is unauthenticated (e.g. during application startup or login view lifecycle), allow.
            If Not _session.IsAuthenticated Then
                Return
            End If

            ' 3. If the user is a Manager, they have full CRUD access. Allow.
            If _session.CurrentRole = UserRole.Manager Then
                Return
            End If

            ' 4. If the user is an Owner, restrict writes.
            If _session.CurrentRole = UserRole.Owner Then
                Dim modifiedEntries = context.ChangeTracker.Entries().
                    Where(Function(e) e.State = EntityState.Added OrElse
                                      e.State = EntityState.Modified OrElse
                                      e.State = EntityState.Deleted).
                    ToList()

                ' No actual changes to persist, allow.
                If modifiedEntries.Count = 0 Then
                    Return
                End If

                ' AuthSelfService special case: Owner updating their own credentials.
                If _writeContext.Current = WriteContextKind.AuthSelfService Then
                    ' Exactly one entry of type UserAccount must be tracked.
                    If modifiedEntries.Count = 1 Then
                        Dim singleEntry = modifiedEntries(0)
                        Dim entityType = singleEntry.Entity.GetType()
                        Dim isUserAccount = entityType.Name = "UserAccount" OrElse entityType.FullName.EndsWith(".UserAccount")

                        If isUserAccount Then
                            ' The UserAccount being modified must match the asserted SelfServiceUsername.
                            Dim usernameProp = entityType.GetProperty("Username")
                            If usernameProp IsNot Nothing Then
                                Dim usernameValue = CStr(usernameProp.GetValue(singleEntry.Entity))
                                If String.Equals(usernameValue, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
                                    ' Allowed.
                                    Return
                                End If
                            End If
                        End If
                    End If
                End If

                ' Extract distinct table names to report in the exception.
                Dim distinctTables As New List(Of String)()
                For Each entry In modifiedEntries
                    Dim tableName = entry.Metadata.GetTableName()
                    If String.IsNullOrWhiteSpace(tableName) Then
                        tableName = entry.Entity.GetType().Name
                    End If
                    If Not distinctTables.Contains(tableName) Then
                        distinctTables.Add(tableName)
                    End If
                Next

                Throw New UnauthorizedWriteException(UserRole.Owner, distinctTables)
            End If
        End Sub

    End Class

End Namespace
