Imports System.Collections.ObjectModel
Imports System.ComponentModel.DataAnnotations
Imports System.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace ViewModels

    ''' <summary>
    ''' ViewModel for the VAT Settings view (POS-17).
    ''' Loads the current <c>Pos_VatConfiguration</c> row, exposes editable fields,
    ''' and delegates saves to <see cref="IVatConfigurationWriter"/>.
    ''' Manager-only: Owner role cannot navigate to <c>VatSettingsView</c>.
    ''' </summary>
    Public Class VatSettingsViewModel
        Inherits ObservableValidator

        Private ReadOnly _writer As IVatConfigurationWriter
        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _conflictPresenter As IConflictPresenter

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
        <Required(ErrorMessage:="TIN is required.")>
        <RegularExpression("^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$",
            ErrorMessage:="TIN must match BIR format: 999-999-999, 999-999-999-000, or 999-999-999-00000.")>
        Public Property Tin As String
            Get
                Return _tin
            End Get
            Set(value As String)
                If SetProperty(_tin, value, True) Then
                    If SaveCommand IsNot Nothing Then SaveCommand.NotifyCanExecuteChanged()
                End If
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
        <Required(ErrorMessage:="Registered business name is required.")>
        Public Property RegisteredBusinessName As String
            Get
                Return _registeredBusinessName
            End Get
            Set(value As String)
                If SetProperty(_registeredBusinessName, value, True) Then
                    If SaveCommand IsNot Nothing Then SaveCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _registeredAddress As String = String.Empty
        <Required(ErrorMessage:="Registered address is required.")>
        Public Property RegisteredAddress As String
            Get
                Return _registeredAddress
            End Get
            Set(value As String)
                If SetProperty(_registeredAddress, value, True) Then
                    If SaveCommand IsNot Nothing Then SaveCommand.NotifyCanExecuteChanged()
                End If
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

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Private Set(value As Boolean)
                SetProperty(_isError, value)
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Private Set(value As String)
                SetProperty(_errorMessage, value)
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

        Public Sub New(writer As IVatConfigurationWriter,
                       notifications As INotificationService,
                       conflictPresenter As IConflictPresenter)
            _writer = writer
            _notifications = notifications
            _conflictPresenter = conflictPresenter

            _saveCommand = New AsyncRelayCommand(AddressOf SaveAsync, Function() Not HasErrors)
            _reloadCommand = New AsyncRelayCommand(AddressOf ReloadAsync)
        End Sub

        ' ── Commands impl ─────────────────────────────────────────────────────────

        Public Async Function ReloadAsync() As Task
            IsError = False
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
                ErrorMessage = loadError
                IsError = True
                Return
            End If

            If config Is Nothing Then
                _validationErrors.Clear()
                _validationErrors.Add("VAT configuration record not found.")
                Return
            End If

            _isVatRegistered = config.IsVatRegistered
            _tin = If(config.BusinessTIN, String.Empty)
            _vatRatePercent = config.VatRate * 100D
            _percentageTaxRatePercent = config.NonVatPercentageTaxRate * 100D
            _registeredBusinessName = If(config.BusinessName, String.Empty)
            _registeredAddress = If(config.BusinessAddress, String.Empty)

            ClearErrors(NameOf(Tin))
            ClearErrors(NameOf(RegisteredBusinessName))
            ClearErrors(NameOf(RegisteredAddress))

            OnPropertyChanged(NameOf(IsVatRegistered))
            OnPropertyChanged(NameOf(Tin))
            OnPropertyChanged(NameOf(VatRatePercent))
            OnPropertyChanged(NameOf(PercentageTaxRatePercent))
            OnPropertyChanged(NameOf(RegisteredBusinessName))
            OnPropertyChanged(NameOf(RegisteredAddress))

            _validationErrors.Clear()
            StatusMessage = "Settings loaded."
            _saveCommand.NotifyCanExecuteChanged()
        End Function

        Public Async Function SaveAsync() As Task
            ValidateAllProperties()
            If HasErrors Then Return

            ' Snapshot form values so they can be restored on a concurrency conflict.
            Dim snapVatRegistered = IsVatRegistered
            Dim snapTin = Tin
            Dim snapVatRate = VatRatePercent
            Dim snapPctTax = PercentageTaxRatePercent
            Dim snapBizName = RegisteredBusinessName
            Dim snapBizAddress = RegisteredAddress

            IsSaving = True
            _validationErrors.Clear()
            StatusMessage = "Saving…"   ' reflect the pending write immediately (optimistic)

            Dim result As VatConfigurationUpdateResult = Nothing
            Dim saveError As String = Nothing
            Dim didRefresh As Boolean = False

            Dim request As New VatConfigurationUpdateRequest With {
                .IsVatRegistered = IsVatRegistered,
                .Tin = Tin,
                .VatRate = VatRatePercent / 100D,
                .PercentageTaxRate = PercentageTaxRatePercent / 100D,
                .RegisteredBusinessName = RegisteredBusinessName,
                .RegisteredAddress = RegisteredAddress
            }

            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        result = Await _writer.UpdateAsync(request, CancellationToken.None)
                    End Function,
                    Async Function()
                        ' Conflict: roll back the optimistic form state to the pre-save snapshot,
                        ' then reload the current DB values so the form shows what another client wrote.
                        didRefresh = True
                        _isVatRegistered = snapVatRegistered
                        _tin = snapTin
                        _vatRatePercent = snapVatRate
                        _percentageTaxRatePercent = snapPctTax
                        _registeredBusinessName = snapBizName
                        _registeredAddress = snapBizAddress
                        OnPropertyChanged(NameOf(IsVatRegistered))
                        OnPropertyChanged(NameOf(IsNotVatRegistered))
                        OnPropertyChanged(NameOf(Tin))
                        OnPropertyChanged(NameOf(VatRatePercent))
                        OnPropertyChanged(NameOf(PercentageTaxRatePercent))
                        OnPropertyChanged(NameOf(RegisteredBusinessName))
                        OnPropertyChanged(NameOf(RegisteredAddress))
                        Await ReloadAsync()
                    End Function,
                    _conflictPresenter)

                If saved AndAlso result IsNot Nothing Then
                    If result.Persisted Then
                        StatusMessage = "VAT settings saved."
                        _notifications.ShowSuccess("VAT settings updated — receipts will use new values immediately.")
                    Else
                        StatusMessage = String.Empty
                        For Each errMsg In result.ValidationErrors
                            _validationErrors.Add(errMsg)
                        Next
                    End If
                ElseIf Not saved AndAlso Not didRefresh Then
                    ' Conflict surfaced but the operator chose Cancel: the rollback lambda never ran,
                    ' so clear the optimistic "Saving…" status (the form keeps the edits to retry).
                    StatusMessage = "Not saved — data changed elsewhere."
                End If
            Catch ex As Exception
                saveError = ex.Message
            Finally
                IsSaving = False
            End Try

            If saveError IsNot Nothing Then
                _validationErrors.Add("Unexpected error: " & saveError)
            End If
        End Function

    End Class

End Namespace
