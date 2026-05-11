Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Write-side abstraction for VAT configuration.
    ''' Separates the read path (<see cref="VatConfigurationLoader"/>) from the write path so the
    ''' loader can remain a pure singleton cache without any persistence responsibility.
    ''' <para>
    ''' <see cref="UpdateAsync"/> is the only mutating entry point; it validates the request,
    ''' persists the singleton row, calls <see cref="VatConfigurationLoader.Invalidate"/> so all
    ''' subsequent receipts and reports use the new values without an app restart, and publishes
    ''' <see cref="VatConfigurationChangedEvent"/> for cross-module consumers.
    ''' </para>
    ''' </summary>
    Public Interface IVatConfigurationWriter

        ''' <summary>Returns a snapshot of the current VAT configuration row (Id = 1).</summary>
        Function GetCurrentAsync() As Task(Of Entities.VatConfiguration)

        ''' <summary>
        ''' Validates <paramref name="request"/>, persists the singleton row, invalidates the
        ''' in-memory cache in <see cref="VatConfigurationLoader"/>, and publishes
        ''' <see cref="VatConfigurationChangedEvent"/>.
        ''' Returns <see cref="VatConfigurationUpdateResult.Persisted"/> = False (never throws)
        ''' if validation fails or if the database write fails.
        ''' </summary>
        Function UpdateAsync(
            request As VatConfigurationUpdateRequest,
            cancellationToken As CancellationToken
        ) As Task(Of VatConfigurationUpdateResult)

    End Interface

    ''' <summary>Carries the new field values for a VAT configuration save.</summary>
    Public Class VatConfigurationUpdateRequest

        Public Property IsVatRegistered As Boolean

        ''' <summary>
        ''' BIR TIN. Required when <see cref="IsVatRegistered"/> is True.
        ''' Accepted formats: 999-999-999 (individual, no branch), 999-999-999-000
        ''' (registered business, 3-digit branch), 999-999-999-00000 (5-digit branch).
        ''' </summary>
        Public Property Tin As String

        ''' <summary>Stored as a decimal fraction (e.g., 0.12 for 12%). Must be in (0, 1).</summary>
        Public Property VatRate As Decimal

        ''' <summary>Stored as a decimal fraction (e.g., 0.03 for 3%). Must be in (0, 1).</summary>
        Public Property PercentageTaxRate As Decimal

        Public Property RegisteredBusinessName As String

        Public Property RegisteredAddress As String

    End Class

    ''' <summary>Outcome of a <see cref="IVatConfigurationWriter.UpdateAsync"/> call.</summary>
    Public Class VatConfigurationUpdateResult

        Public Property Persisted As Boolean

        Public Property ValidationErrors As IReadOnlyList(Of String)

        ''' <summary>True when <see cref="VatConfigurationLoader.Invalidate"/> was called.</summary>
        Public Property CacheInvalidated As Boolean

    End Class

    ' ─────────────────────────────────────────────────────────────────────────────
    ' Implementation
    ' ─────────────────────────────────────────────────────────────────────────────

    Public Class VatConfigurationWriter
        Implements IVatConfigurationWriter

        ' BIR Form 2303 TIN patterns:
        '   9-digit  (individual taxpayer, no branch code)   : 999-999-999
        '   12-digit (registered business, 3-digit branch)   : 999-999-999-000
        '   14-digit (legacy/VAT-registered, 5-digit branch) : 999-999-999-00000
        Private Const TinPattern As String = "^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$"

        Private ReadOnly _ctx As POSDbContext
        Private ReadOnly _loader As VatConfigurationLoader
        Private ReadOnly _eventBus As IEventBus
        Private ReadOnly _session As ISessionService

        Public Sub New(ctx As POSDbContext,
                       loader As VatConfigurationLoader,
                       eventBus As IEventBus,
                       session As ISessionService)
            _ctx = ctx
            _loader = loader
            _eventBus = eventBus
            _session = session
        End Sub

        Public Async Function GetCurrentAsync() As Task(Of Entities.VatConfiguration) _
            Implements IVatConfigurationWriter.GetCurrentAsync

            Return Await _ctx.VatConfigurations _
                .AsNoTracking() _
                .FirstOrDefaultAsync(Function(v) v.Id = 1)
        End Function

        Public Async Function UpdateAsync(
            request As VatConfigurationUpdateRequest,
            cancellationToken As CancellationToken
        ) As Task(Of VatConfigurationUpdateResult) Implements IVatConfigurationWriter.UpdateAsync

            Dim errors = Validate(request)
            If errors.Count > 0 Then
                Return New VatConfigurationUpdateResult With {
                    .Persisted = False,
                    .ValidationErrors = errors,
                    .CacheInvalidated = False
                }
            End If

            Dim persistError As String = Nothing
            Dim previousIsVatRegistered As Boolean = False

            Try
                Dim config = Await _ctx.VatConfigurations _
                    .FirstOrDefaultAsync(Function(v) v.Id = 1, cancellationToken)

                If config Is Nothing Then
                    Return New VatConfigurationUpdateResult With {
                        .Persisted = False,
                        .ValidationErrors = New List(Of String) From {"VAT configuration record not found."},
                        .CacheInvalidated = False
                    }
                End If

                previousIsVatRegistered = config.IsVatRegistered

                config.IsVatRegistered = request.IsVatRegistered
                config.BusinessTIN = If(request.Tin?.Trim(), String.Empty)
                config.VatRate = request.VatRate
                config.NonVatPercentageTaxRate = request.PercentageTaxRate
                config.BusinessName = request.RegisteredBusinessName.Trim()
                config.BusinessAddress = request.RegisteredAddress.Trim()
                config.EffectiveFrom = DateTime.UtcNow
                config.ModifiedAt = DateTime.UtcNow
                config.ModifiedBy = _session.CurrentUsername

                Await _ctx.SaveChangesAsync(cancellationToken)
            Catch ex As Exception
                persistError = ex.Message
            End Try

            If persistError IsNot Nothing Then
                Return New VatConfigurationUpdateResult With {
                    .Persisted = False,
                    .ValidationErrors = New List(Of String) From {persistError},
                    .CacheInvalidated = False
                }
            End If

            _loader.Invalidate()

            Await _eventBus.PublishAsync(New VatConfigurationChangedEvent With {
                .OccurredAt = DateTime.UtcNow,
                .IsVatRegistered = request.IsVatRegistered,
                .PreviousIsVatRegistered = previousIsVatRegistered
            })

            Return New VatConfigurationUpdateResult With {
                .Persisted = True,
                .ValidationErrors = New List(Of String)(),
                .CacheInvalidated = True
            }
        End Function

        Private Shared Function Validate(request As VatConfigurationUpdateRequest) As List(Of String)
            Dim errors As New List(Of String)()

            If request.IsVatRegistered Then
                If String.IsNullOrWhiteSpace(request.Tin) Then
                    errors.Add("TIN is required when the business is VAT-registered.")
                ElseIf Not Regex.IsMatch(request.Tin.Trim(), TinPattern) Then
                    errors.Add("TIN must match BIR format: 999-999-999, 999-999-999-000, or 999-999-999-00000.")
                End If
            End If

            If request.VatRate <= 0D OrElse request.VatRate >= 1D Then
                errors.Add("VAT rate must be a decimal fraction between 0 and 1 exclusive (e.g., 0.12 for 12%).")
            End If

            If request.PercentageTaxRate <= 0D OrElse request.PercentageTaxRate >= 1D Then
                errors.Add("Percentage tax rate must be a decimal fraction between 0 and 1 exclusive (e.g., 0.03 for 3%).")
            End If

            If String.IsNullOrWhiteSpace(request.RegisteredBusinessName) Then
                errors.Add("Registered business name is required.")
            End If

            If String.IsNullOrWhiteSpace(request.RegisteredAddress) Then
                errors.Add("Registered address is required.")
            End If

            Return errors
        End Function

    End Class

End Namespace
