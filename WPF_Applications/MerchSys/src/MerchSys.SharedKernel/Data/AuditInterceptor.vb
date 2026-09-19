Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports MerchSys.SharedKernel.Interfaces

Namespace Data

    ''' <summary>
    ''' Alternative EF Core SaveChanges interceptor that populates audit and soft-delete columns.
    ''' <para>
    ''' This is an alternative to the <see cref="BaseDbContext.SaveChangesAsync"/> override.
    ''' To use this approach instead, register it via
    ''' <c>options.AddInterceptors(New AuditInterceptor())</c> in each DbContext's DI
    ''' registration and remove the audit logic from <see cref="BaseDbContext"/>.
    ''' </para>
    ''' </summary>
    Public NotInheritable Class AuditInterceptor
        Inherits SaveChangesInterceptor

        Private Const DefaultUser As String = BaseDbContext.DefaultUser

        Public Overrides Function SavingChangesAsync(
            eventData As DbContextEventData,
            result As InterceptionResult(Of Integer),
            Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of InterceptionResult(Of Integer))

            If eventData.Context IsNot Nothing Then
                ApplyAudit(eventData.Context)
            End If

            Return MyBase.SavingChangesAsync(eventData, result, cancellationToken)
        End Function

        Private Shared Sub ApplyAudit(context As DbContext)
            Dim now = DateTime.UtcNow

            For Each dbEntry In context.ChangeTracker.Entries().ToList()
                Select Case dbEntry.State

                    Case EntityState.Added
                        If TypeOf dbEntry.Entity Is IAuditable Then
                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                            auditable.CreatedAt = now
                            auditable.CreatedBy = DefaultUser
                            auditable.ModifiedAt = now
                            auditable.ModifiedBy = DefaultUser
                        End If

                    Case EntityState.Modified
                        If TypeOf dbEntry.Entity Is IAuditable Then
                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                            auditable.ModifiedAt = now
                            auditable.ModifiedBy = DefaultUser
                        End If

                    Case EntityState.Deleted
                        If TypeOf dbEntry.Entity Is ISoftDeletable Then
                            dbEntry.State = EntityState.Modified
                            Dim softDel = DirectCast(dbEntry.Entity, ISoftDeletable)
                            softDel.IsDeleted = True
                            softDel.DeletedAt = now
                            softDel.DeletedBy = DefaultUser
                            If TypeOf dbEntry.Entity Is IAuditable Then
                                Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                                auditable.ModifiedAt = now
                                auditable.ModifiedBy = DefaultUser
                            End If
                        End If

                End Select
            Next
        End Sub

    End Class

End Namespace
