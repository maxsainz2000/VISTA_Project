Imports System.Collections.ObjectModel
Imports System.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Interfaces

Namespace ViewModels

    ''' <summary>
    ''' ViewModel for the VAT Settings view (POS-17).
    ''' Loads the current <c>Pos_VatConfiguration</c> row, exposes editable fields,
    ''' and delegates saves to <see cref="IVatConfigurationWriter"/>.
    ''' Manager-only: Owner role cannot navigate to <c>VatSettingsView</c>.
    ''' </summary>
    Public Class VatSettingsViewModel
        Inherits ObservableObject

        Private ReadOnly _writer As IVatConfigurationWriter
        Private ReadOnly _notifications As INotificationService

        ' ── Observable properties ────────────────────────────────────────────────

        Private _isVatRegistered As Boolean
        Public Property IsVatRegistered As Boolean
            Get
                Return _isVatRegistered
            End Get
            Set(value As Boolean)
                If SetProperty(_isVatRegistered, value) Then
                    OnPropertyChanged(NameOf(IsNotVatRegistered))
                End If
            End Set
        End Property

        ''' <summary>Convenience inverse of <see cref="IsVatRegistered"/> for conditional visibility bindings.</summary>
        Public ReadOnly Property IsNotVatRegistered As Boolean
            Get
                Return Not _isVatRegistered
            End Get
        End Property

        Private _tin As String = String.Empty
        Public Property Tin As String
            Get
                Return _tin
            End Get
            Set(value As String)
                SetProperty(_tin, value)
            End Set
        End Property

        ' Display uses whole-number percentages (12, 3); the underlying model stores decimal fractions
        ' (0.12, 0.03). Multiply by 100 when loading from the writer; divide by 100 before sending.
        Private _vatRatePercent As Decimal = 12D
        Public Property VatRatePercent As Decimal
            Get
                Return _vatRatePercent
            End Get
            Set(value As Decimal)
                SetProperty(_vatRatePercent, value)
            End Set
        End Property

        Private _percentageTaxRatePercent As Decimal = 3D
        Public Property PercentageTaxRatePercent As Decimal
            Get
                Return _percentageTaxRatePercent
            End Get
            Set(value As Decimal)
                SetProperty(_percentageTaxRatePercent, value)
            End Set
        End Property

        Private _registeredBusinessName As String = String.Empty
        Public Property RegisteredBusinessName As String
            Get
                Return _registeredBusinessName
            End Get
            Set(value As String)
                SetProperty(_registeredBusinessName, value)
            End Set
        End Property

        Private _registeredAddress As String = String.Empty
        Public Property RegisteredAddress As String
            Get
                Return _registeredAddress
            End Get
            Set(value As String)
                SetProperty(_registeredAddress, value)
            End Set
        End Property

        Private _validationErrors As New ObservableCollection(Of String)()
        Public Property ValidationErrors As ObservableCollection(Of String)
            Get
                Return _validationErrors
            End Get
            Private Set(value As ObservableCollection(Of String))
                _validationErrors = value
            End Set
        End Property

        Private _isSaving As Boolean
        Public Property IsSaving As Boolean
            Get
                Return _isSaving
            End Get
            Private Set(value As Boolean)
                SetProperty(_isSaving, value)
            End Set
        End Property

        Private _statusMessage As String = String.Empty
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        ' ── Commands ─────────────────────────────────────────────────────────────

        Private _saveCommand As AsyncRelayCommand
        Public Property SaveCommand As AsyncRelayCommand
            Get
                Return _saveCommand
            End Get
            Private Set(value As AsyncRelayCommand)
                _saveCommand = value
            End Set
        End Property

        Private _reloadCommand As AsyncRelayCommand
        Public Property ReloadCommand As AsyncRelayCommand
            Get
                Return _reloadCommand
            End Get
            Private Set(value As AsyncRelayCommand)
                _reloadCommand = value
            End Set
        End Property

        ' ── Constructor ──────────────────────────────────────────────────────────

        Public Sub New(writer As IVatConfigurationWriter, notifications As INotificationService)
            _writer = writer
            _notifications = notifications

            _saveCommand = New AsyncRelayCommand(AddressOf SaveAsync)
            _reloadCommand = New AsyncRelayCommand(AddressOf ReloadAsync)
        End Sub

        ' ── Commands impl ─────────────────────────────────────────────────────────

        Public Async Function ReloadAsync() As Task
            Dim loadError As String = Nothing
            Dim config As Entities.VatConfiguration = Nothing

            Try
                config = Await _writer.GetCurrentAsync()
            Catch ex As Exception
                loadError = ex.Message
            End Try

            If loadError IsNot Nothing Then
                _validationErrors.Clear()
                _validationErrors.Add("Failed to load VAT settings: " & loadError)
                Return
            End If

            If config Is Nothing Then
                _validationErrors.Clear()
                _validationErrors.Add("VAT configuration record not found.")
                Return
            End If

            IsVatRegistered = config.IsVatRegistered
            Tin = If(config.BusinessTIN, String.Empty)
            VatRatePercent = config.VatRate * 100D
            PercentageTaxRatePercent = config.NonVatPercentageTaxRate * 100D
            RegisteredBusinessName = If(config.BusinessName, String.Empty)
            RegisteredAddress = If(config.BusinessAddress, String.Empty)
            _validationErrors.Clear()
            StatusMessage = "Settings loaded."
        End Function

        Public Async Function SaveAsync() As Task
            IsSaving = True
            _validationErrors.Clear()
            StatusMessage = String.Empty

            Dim result As VatConfigurationUpdateResult = Nothing
            Dim saveError As String = Nothing

            Try
                Dim request As New VatConfigurationUpdateRequest With {
                    .IsVatRegistered = IsVatRegistered,
                    .Tin = Tin,
                    .VatRate = VatRatePercent / 100D,
                    .PercentageTaxRate = PercentageTaxRatePercent / 100D,
                    .RegisteredBusinessName = RegisteredBusinessName,
                    .RegisteredAddress = RegisteredAddress
                }
                result = Await _writer.UpdateAsync(request, CancellationToken.None)
            Catch ex As Exception
                saveError = ex.Message
            Finally
                IsSaving = False
            End Try

            If saveError IsNot Nothing Then
                _validationErrors.Add("Unexpected error: " & saveError)
                Return
            End If

            If result.Persisted Then
                StatusMessage = "VAT settings saved."
                _notifications.ShowSuccess("VAT settings updated — receipts will use new values immediately.")
            Else
                For Each errMsg In result.ValidationErrors
                    _validationErrors.Add(errMsg)
                Next
            End If
        End Function

    End Class

End Namespace
