Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Entities
Imports System.Collections.Concurrent

Namespace Data

    ''' <summary>
    ''' Abstract base DbContext inherited by all module DbContexts.
    ''' <para>
    ''' Automatically applies a global soft-delete query filter so that every entity
    ''' implementing <see cref="ISoftDeletable"/> is excluded from queries when
    ''' <c>IsDeleted = True</c>. Call <c>.IgnoreQueryFilters()</c> on a query to bypass
    ''' (e.g., admin restore screens).
    ''' </para>
    ''' <para>
    ''' Overrides <see cref="SaveChangesAsync"/> to auto-populate
    ''' <see cref="IAuditable"/> columns (<c>CreatedAt/By</c>, <c>ModifiedAt/By</c>) and to
    ''' convert hard deletes on <see cref="ISoftDeletable"/> entities into logical deletes
    ''' (<c>IsDeleted = True</c>, <c>DeletedAt</c>, <c>DeletedBy</c>).
    ''' </para>
    ''' </summary>
    Public MustInherit Class BaseDbContext
        Inherits DbContext

        ''' <summary>Hardcoded identity placeholder until the auth module is implemented.</summary>
        Friend Const DefaultUser As String = "Manager"
        Private ReadOnly _session As ISessionService

        Protected Sub New(options As DbContextOptions, session As ISessionService)
            MyBase.New(options)
            _session = session
        End Sub

        Private Function CurrentUser() As String
            Dim u = _session?.CurrentUsername
            Return If(String.IsNullOrWhiteSpace(u), DefaultUser, u)
        End Function

        ''' <summary>Sets a default max-length of 256 for all string columns and ignores non-token RowVersion properties.</summary>
        Protected Overrides Sub ConfigureConventions(configurationBuilder As ModelConfigurationBuilder)
            configurationBuilder.Properties(Of String)().HaveMaxLength(256)
            configurationBuilder.Conventions.Add(Function(sp) New IgnoreNonTokenRowVersionConvention())
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            ApplySoftDeleteFilters(modelBuilder)
        End Sub

        ''' <summary>
        ''' Registers <c>e.IsDeleted = False</c> as a global query filter on every entity
        ''' type in the model that implements <see cref="ISoftDeletable"/>.
        ''' </summary>
        Private Shared Sub ApplySoftDeleteFilters(modelBuilder As ModelBuilder)
            Dim buildMethod = GetType(BaseDbContext).GetMethod(
                NameOf(BuildSoftDeleteFilter),
                BindingFlags.Static Or BindingFlags.NonPublic)

            For Each entityType In modelBuilder.Model.GetEntityTypes()
                If GetType(ISoftDeletable).IsAssignableFrom(entityType.ClrType) Then
                    Dim genericMethod = buildMethod.MakeGenericMethod(entityType.ClrType)
                    Dim filter = DirectCast(genericMethod.Invoke(Nothing, Nothing), LambdaExpression)
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter)
                End If
            Next
        End Sub

        Private Shared Function BuildSoftDeleteFilter(Of T As {Class, ISoftDeletable})() As Expression(Of Func(Of T, Boolean))
            Return Function(e) Not e.IsDeleted
        End Function

        Public Shared ReadOnly UnconfiguredRowVersionEntities As New ConcurrentBag(Of String)()

        Private Sub ApplyAuditAndSoftDelete()
            Dim now = DateTime.UtcNow

            For Each dbEntry In ChangeTracker.Entries().ToList()
                Select Case dbEntry.State

                    Case EntityState.Added
                        If TypeOf dbEntry.Entity Is IAuditable Then
                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                            auditable.CreatedAt = now
                            auditable.CreatedBy = If(String.IsNullOrWhiteSpace(auditable.CreatedBy), CurrentUser(), auditable.CreatedBy)
                            auditable.ModifiedAt = now
                            auditable.ModifiedBy = CurrentUser()
                        End If

                    Case EntityState.Modified
                        If TypeOf dbEntry.Entity Is IAuditable Then
                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                            auditable.ModifiedAt = now
                            auditable.ModifiedBy = CurrentUser()
                        End If

                    Case EntityState.Deleted
                        If TypeOf dbEntry.Entity Is ISoftDeletable Then
                            ' Intercept the hard delete and convert to a logical delete.
                            dbEntry.State = EntityState.Modified
                            Dim softDel = DirectCast(dbEntry.Entity, ISoftDeletable)
                            softDel.IsDeleted = True
                            softDel.DeletedAt = now
                            softDel.DeletedBy = CurrentUser()
                            If TypeOf dbEntry.Entity Is IAuditable Then
                                Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                                auditable.ModifiedAt = now
                                auditable.ModifiedBy = CurrentUser()
                            End If
                        End If

                End Select
            Next
        End Sub

        ''' <summary>
        ''' Populates audit columns and converts hard deletes to soft deletes before
        ''' delegating to the base EF Core persistence logic.
        ''' </summary>
        Public Overrides Function SaveChanges() As Integer
            ApplyAuditAndSoftDelete()
            Return MyBase.SaveChanges()
        End Function

        ''' <summary>
        ''' Populates audit columns and converts hard deletes to soft deletes before
        ''' delegating to the base EF Core persistence logic.
        ''' </summary>
        Public Overrides Async Function SaveChangesAsync(
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of Integer)

            ApplyAuditAndSoftDelete()
            Return Await MyBase.SaveChangesAsync(cancellationToken)
        End Function

        ''' <summary>
        ''' Convention that runs during model finalization to ignore the RowVersion property
        ''' on any entity type where it is not explicitly configured as a concurrency token.
        ''' </summary>
        Private NotInheritable Class IgnoreNonTokenRowVersionConvention
            Implements IModelFinalizingConvention

            Public Sub ProcessModelFinalizing(modelBuilder As IConventionModelBuilder,
                                              context As IConventionContext(Of IConventionModelBuilder)) _
                                              Implements IModelFinalizingConvention.ProcessModelFinalizing
                Dim ignoredList As New List(Of String)()
                For Each et In modelBuilder.Metadata.GetEntityTypes()
                    If GetType(ConcurrencyAwareEntity).IsAssignableFrom(et.ClrType) Then
                        Dim rv = et.FindProperty("RowVersion")
                        If rv IsNot Nothing AndAlso Not rv.IsConcurrencyToken() Then
                            ignoredList.Add(et.ClrType.Name)
                            et.Builder.Ignore("RowVersion", fromDataAnnotation:=False)
                        End If
                    End If
                Next
                If ignoredList.Count > 0 Then
                    Dim warningMsg As String = "WARNING: The following entities inherit ConcurrencyAwareEntity but do not have RowVersion configured as a concurrency token: " & String.Join(", ", ignoredList)
                    System.Diagnostics.Debug.WriteLine(warningMsg)
                    System.Console.WriteLine(warningMsg)
                    For Each name In ignoredList
                        BaseDbContext.UnconfiguredRowVersionEntities.Add(name)
                    Next
                End If
            End Sub
        End Class

    End Class

End Namespace
