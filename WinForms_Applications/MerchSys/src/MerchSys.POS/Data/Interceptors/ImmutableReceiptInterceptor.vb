Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Exceptions

Namespace Data.Interceptors

    ''' <summary>
    ''' EF Core <see cref="SaveChangesInterceptor"/> that enforces immutability of
    ''' <see cref="OfficialReceipt"/> and <see cref="ReceiptIntegrity"/> entities at the ORM layer.
    ''' Any UPDATE or DELETE attempt throws <see cref="ImmutableEntityException"/> before
    ''' the command reaches the database.
    ''' <para>
    ''' This interceptor is the application-layer defense. SQLite-level triggers
    ''' <c>pos_receipts_no_update</c> and <c>pos_receipts_no_delete</c> (migration
    ''' <c>AddBirRetentionConstraints</c>) provide the database-layer defense-in-depth.
    ''' NIRC §235 — BIR 10-year tamper-proof receipt retention.
    ''' </para>
    ''' </summary>
    Public Class ImmutableReceiptInterceptor
        Inherits SaveChangesInterceptor

        Public Overrides Function SavingChanges(
            eventData As DbContextEventData,
            result As InterceptionResult(Of Integer)) As InterceptionResult(Of Integer)

            CheckImmutableEntries(eventData.Context)
            Return MyBase.SavingChanges(eventData, result)
        End Function

        Public Overrides Function SavingChangesAsync(
            eventData As DbContextEventData,
            result As InterceptionResult(Of Integer),
            Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of InterceptionResult(Of Integer))

            CheckImmutableEntries(eventData.Context)
            Return MyBase.SavingChangesAsync(eventData, result, cancellationToken)
        End Function

        Private Shared Sub CheckImmutableEntries(context As DbContext)
            If context Is Nothing Then Return

            For Each entry In context.ChangeTracker.Entries(Of OfficialReceipt)()
                If entry.State = EntityState.Modified OrElse entry.State = EntityState.Deleted Then
                    Throw New ImmutableEntityException(GetType(OfficialReceipt), entry.Entity.Id)
                End If
            Next

            For Each entry In context.ChangeTracker.Entries(Of ReceiptIntegrity)()
                If entry.State = EntityState.Modified OrElse entry.State = EntityState.Deleted Then
                    Throw New ImmutableEntityException(GetType(ReceiptIntegrity), entry.Entity.Id)
                End If
            Next
        End Sub

    End Class

End Namespace
