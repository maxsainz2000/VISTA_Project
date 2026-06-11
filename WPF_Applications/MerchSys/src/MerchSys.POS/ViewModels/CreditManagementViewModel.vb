Imports System.Collections.ObjectModel
Imports System.ComponentModel.DataAnnotations
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Presentation

Namespace ViewModels



    ''' <summary>
    ''' ViewModel for the Credit Management screen.
    ''' Lists all customer credit accounts, supports payment recording, account creation,
    ''' and per-account payment / credit-transaction history.
    ''' </summary>
    Public Class CreditManagementViewModel
        Inherits ObservableValidator
        Implements IFreshnessAware

        Private _lastLoadedAt As DateTime?
        Public Property LastLoadedAt As DateTime? Implements IFreshnessAware.LastLoadedAt
            Get
                Return _lastLoadedAt
            End Get
            Set(value As DateTime?)
                SetProperty(_lastLoadedAt, value)
            End Set
        End Property

        Private ReadOnly _creditService As ICreditService
        Private ReadOnly _context As POSDbContext
        Private ReadOnly _session As ISessionService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _confirmationPresenter As IConfirmationPresenter
        Private ReadOnly _notifications As INotificationService

        ' Unfiltered master list used for in-memory filtering
        Private _allAccounts As List(Of CreditAccount) = New List(Of CreditAccount)()

        ' Session memory
        Private Shared _savedSearchText As String = String.Empty
        Private Shared _savedActiveFilter As String = "All"
        Private Shared _lastUser As String = Nothing

        ' ── Backing fields ────────────────────────────────────────────────────────

        Private _totalOutstanding As Decimal
        Private _blockedCount As Integer
        Private _overdueCount As Integer

        Private _accounts As ObservableCollection(Of CreditAccount) = New ObservableCollection(Of CreditAccount)()
        Private _selectedAccount As CreditAccount
        Private _searchText As String = String.Empty
        Private _activeFilter As String = "All"

        Private _paymentHistory As ObservableCollection(Of CreditPayment) = New ObservableCollection(Of CreditPayment)()
        Private _activeFilterChips As ObservableCollection(Of FilterChipItem) = New ObservableCollection(Of FilterChipItem)()
        Private _creditTransactions As ObservableCollection(Of CreditTransactionItem) = New ObservableCollection(Of CreditTransactionItem)()

        Private _isAddAccountVisible As Boolean
        Private _newAccountName As String = String.Empty
        Private _newAccountPhone As String = String.Empty
        Private _newAccountAddress As String = String.Empty

        Private _isPaymentDialogVisible As Boolean
        Private _paymentAmount As String = String.Empty
        Private _selectedPaymentMethodIndex As Integer
        Private _paymentTargetAccount As CreditAccount

        Private _statusMessage As String = String.Empty
        Private _isStatusSuccess As Boolean
        Private _isBusy As Boolean

        ' ── Commands ──────────────────────────────────────────────────────────────

        Public ReadOnly Property LoadDataCommand As AsyncRelayCommand
        Public ReadOnly Property ClearFiltersCommand As RelayCommand
        Public ReadOnly Property SearchCommand As RelayCommand
        Public ReadOnly Property ApplyFilterCommand As RelayCommand(Of String)
        Public ReadOnly Property AddAccountCommand As RelayCommand
        Public ReadOnly Property CancelAddAccountCommand As RelayCommand
        Public ReadOnly Property ConfirmAddAccountCommand As AsyncRelayCommand
        Public ReadOnly Property SelectAccountCommand As AsyncRelayCommand(Of CreditAccount)
        Public ReadOnly Property OpenPaymentDialogCommand As RelayCommand(Of CreditAccount)
        Public ReadOnly Property RecordPaymentCommand As AsyncRelayCommand
        Public ReadOnly Property CancelPaymentCommand As RelayCommand
        Public ReadOnly Property ToggleBlockCommand As AsyncRelayCommand(Of CreditAccount)

        ' ── Observable Properties ─────────────────────────────────────────────────

        Public Property TotalOutstanding As Decimal
            Get
                Return _totalOutstanding
            End Get
            Private Set(value As Decimal)
                SetProperty(_totalOutstanding, value)
            End Set
        End Property

        Public Property BlockedCount As Integer
            Get
                Return _blockedCount
            End Get
            Private Set(value As Integer)
                SetProperty(_blockedCount, value)
            End Set
        End Property

        Public Property OverdueCount As Integer
            Get
                Return _overdueCount
            End Get
            Private Set(value As Integer)
                SetProperty(_overdueCount, value)
            End Set
        End Property

        Public Property Accounts As ObservableCollection(Of CreditAccount)
            Get
                Return _accounts
            End Get
            Private Set(value As ObservableCollection(Of CreditAccount))
                SetProperty(_accounts, value)
            End Set
        End Property

        Public Property SelectedAccount As CreditAccount
            Get
                Return _selectedAccount
            End Get
            Set(value As CreditAccount)
                SetProperty(_selectedAccount, value)
                OnPropertyChanged(NameOf(HasSelectedAccount))
                If ToggleBlockCommand IsNot Nothing Then ToggleBlockCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property HasSelectedAccount As Boolean
            Get
                Return _selectedAccount IsNot Nothing
            End Get
        End Property

        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then
                    _savedSearchText = value
                    ApplyFilter()
                End If
            End Set
        End Property

        Public Property ActiveFilter As String
            Get
                Return _activeFilter
            End Get
            Set(value As String)
                If SetProperty(_activeFilter, value) Then
                    _savedActiveFilter = value
                    OnPropertyChanged(NameOf(IsFilterAll))
                    OnPropertyChanged(NameOf(IsFilterBlocked))
                    OnPropertyChanged(NameOf(IsFilterWithBalance))
                    OnPropertyChanged(NameOf(IsFilterCleared))
                    ApplyFilter()
                End If
            End Set
        End Property

        Public ReadOnly Property TotalAccounts As Integer
            Get
                Return _allAccounts.Count
            End Get
        End Property

        Public ReadOnly Property IsFilterAll As Boolean
            Get
                Return _activeFilter = "All"
            End Get
        End Property

        Public ReadOnly Property IsFilterBlocked As Boolean
            Get
                Return _activeFilter = "Blocked"
            End Get
        End Property

        Public ReadOnly Property IsFilterWithBalance As Boolean
            Get
                Return _activeFilter = "WithBalance"
            End Get
        End Property

        Public ReadOnly Property IsFilterCleared As Boolean
            Get
                Return _activeFilter = "Cleared"
            End Get
        End Property

        Public Property PaymentHistory As ObservableCollection(Of CreditPayment)
            Get
                Return _paymentHistory
            End Get
            Private Set(value As ObservableCollection(Of CreditPayment))
                SetProperty(_paymentHistory, value)
            End Set
        End Property

        Public Property CreditTransactions As ObservableCollection(Of CreditTransactionItem)
            Get
                Return _creditTransactions
            End Get
            Private Set(value As ObservableCollection(Of CreditTransactionItem))
                SetProperty(_creditTransactions, value)
            End Set
        End Property

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)
            Get
                Return _activeFilterChips
            End Get
            Set(value As ObservableCollection(Of FilterChipItem))
                SetProperty(_activeFilterChips, value)
            End Set
        End Property

        Public Property IsAddAccountVisible As Boolean
            Get
                Return _isAddAccountVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isAddAccountVisible, value)
            End Set
        End Property

        <Required(ErrorMessage:="Customer name is required.")>
        Public Property NewAccountName As String
            Get
                Return _newAccountName
            End Get
            Set(value As String)
                If SetProperty(_newAccountName, value, True) Then
                    ConfirmAddAccountCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        <Required(ErrorMessage:="Phone number is required.")>
        Public Property NewAccountPhone As String
            Get
                Return _newAccountPhone
            End Get
            Set(value As String)
                If SetProperty(_newAccountPhone, value, True) Then
                    ConfirmAddAccountCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property NewAccountAddress As String
            Get
                Return _newAccountAddress
            End Get
            Set(value As String)
                SetProperty(_newAccountAddress, value)
            End Set
        End Property

        Public Property IsPaymentDialogVisible As Boolean
            Get
                Return _isPaymentDialogVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isPaymentDialogVisible, value)
            End Set
        End Property

        Public Property PaymentAmount As String
            Get
                Return _paymentAmount
            End Get
            Set(value As String)
                SetProperty(_paymentAmount, value)
                RecordPaymentCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property SelectedPaymentMethodIndex As Integer
            Get
                Return _selectedPaymentMethodIndex
            End Get
            Set(value As Integer)
                SetProperty(_selectedPaymentMethodIndex, value)
            End Set
        End Property

        Public Property PaymentTargetAccount As CreditAccount
            Get
                Return _paymentTargetAccount
            End Get
            Private Set(value As CreditAccount)
                SetProperty(_paymentTargetAccount, value)
                RecordPaymentCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Private Set(value As String)
                SetProperty(_statusMessage, value)
                OnPropertyChanged(NameOf(HasStatusMessage))
            End Set
        End Property

        Public ReadOnly Property HasStatusMessage As Boolean
            Get
                Return Not String.IsNullOrEmpty(_statusMessage)
            End Get
        End Property

        Public Property IsStatusSuccess As Boolean
            Get
                Return _isStatusSuccess
            End Get
            Private Set(value As Boolean)
                SetProperty(_isStatusSuccess, value)
            End Set
        End Property

        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Private Set(value As Boolean)
                SetProperty(_isBusy, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Private Set(value As Boolean)
                SetProperty(_isError, value)
                OnPropertyChanged(NameOf(IsEmpty))
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

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return Accounts.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        ''' <summary>Payment method options shown in the Record Payment dialog (Credit is excluded).</summary>
        Public ReadOnly Property PaymentMethodOptions As List(Of String)
            Get
                Return New List(Of String) From {"Cash", "GCash", "Bank Transfer"}
            End Get
        End Property

        ' ── Constructor ───────────────────────────────────────────────────────────

        Public Sub New(creditService As ICreditService,
                       context As POSDbContext,
                       session As ISessionService,
                       conflictPresenter As IConflictPresenter,
                       confirmationPresenter As IConfirmationPresenter,
                       notifications As INotificationService)
            _creditService = creditService
            _context = context
            _session = session
            _conflictPresenter = conflictPresenter
            _confirmationPresenter = confirmationPresenter
            _notifications = notifications

            ' Restore session filters
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedSearchText = String.Empty
                _savedActiveFilter = "All"
                _lastUser = currentUser
            End If

            _searchText = _savedSearchText
            _activeFilter = _savedActiveFilter

            LoadDataCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)
            SearchCommand = New RelayCommand(AddressOf ApplyFilter)
            ApplyFilterCommand = New RelayCommand(Of String)(AddressOf SetFilter)
            AddAccountCommand = New RelayCommand(Sub() IsAddAccountVisible = True)
            CancelAddAccountCommand = New RelayCommand(AddressOf ClearAddAccountForm)
            ConfirmAddAccountCommand = New AsyncRelayCommand(AddressOf ConfirmAddAccountAsync, AddressOf CanConfirmAddAccount)
            SelectAccountCommand = New AsyncRelayCommand(Of CreditAccount)(AddressOf SelectAccountAsync)
            OpenPaymentDialogCommand = New RelayCommand(Of CreditAccount)(AddressOf OpenPaymentDialog)
            RecordPaymentCommand = New AsyncRelayCommand(AddressOf RecordPaymentAsync, AddressOf CanRecordPayment)
            CancelPaymentCommand = New RelayCommand(Sub()
                                                        IsPaymentDialogVisible = False
                                                        PaymentAmount = String.Empty
                                                        StatusMessage = String.Empty
                                                    End Sub)
            ToggleBlockCommand = New AsyncRelayCommand(Of CreditAccount)(AddressOf ToggleBlockAsync, AddressOf CanToggleBlock)
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()

            Dim initTask = LoadDataAsync()
        End Sub

        ' ── Internal helpers (no IsBusy management) ───────────────────────────────

        Private Async Function LoadDataInternalAsync() As Task
            _allAccounts = Await _creditService.GetAllAccountsAsync()
            TotalOutstanding = Await _creditService.GetTotalOutstandingAsync()
            BlockedCount = _allAccounts.Where(Function(a) a.IsBlocked).Count()
            Dim overdue = Await _creditService.GetOverdueAccountsAsync()
            OverdueCount = overdue.Count
            ApplyFilter()
        End Function

        Private Async Function LoadHistoryInternalAsync(account As CreditAccount) As Task
            Dim payments = Await _creditService.GetPaymentHistoryAsync(account.Id)
            PaymentHistory.Clear()
            For Each p In payments
                PaymentHistory.Add(p)
            Next

            Dim txs = Await _creditService.GetCreditTransactionsAsync(account.Id)
            CreditTransactions.Clear()
            For Each t In txs
                CreditTransactions.Add(t)
            Next
        End Function

        ' ── Command handlers ──────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Await LoadDataInternalAsync()
                IsError = False
                LastLoadedAt = DateTime.Now
            Catch ex As Exception
                ShowError("Failed to load accounts: " & ex.Message)
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return (ActiveFilter <> "All") OrElse (Not String.IsNullOrWhiteSpace(SearchText))
            End Get
        End Property

        Private Sub RefreshFilterChips()
            If ActiveFilterChips Is Nothing Then
                ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Else
                ActiveFilterChips.Clear()
            End If

            If ActiveFilter <> "All" Then
                ActiveFilterChips.Add(New FilterChipItem($"Status: {ActiveFilter}", "Status", New RelayCommand(Sub() ActiveFilter = "All")))
            End If

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                ActiveFilterChips.Add(New FilterChipItem($"Search: {SearchText.Trim()}", "Search", New RelayCommand(Sub() SearchText = String.Empty)))
            End If

            OnPropertyChanged(NameOf(IsFilterActive))
        End Sub

        Private Sub ClearFilters()
            _searchText = String.Empty
            _savedSearchText = String.Empty
            OnPropertyChanged(NameOf(SearchText))

            _activeFilter = "All"
            _savedActiveFilter = "All"
            OnPropertyChanged(NameOf(ActiveFilter))
            OnPropertyChanged(NameOf(IsFilterAll))
            OnPropertyChanged(NameOf(IsFilterBlocked))
            OnPropertyChanged(NameOf(IsFilterWithBalance))
            OnPropertyChanged(NameOf(IsFilterCleared))

            ApplyFilter()
        End Sub

        Private Sub ApplyFilter()
            Dim filtered = _allAccounts.AsEnumerable()

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.ToLower()
                filtered = filtered.Where(Function(a) a.CustomerName.ToLower().Contains(term) OrElse
                                                       a.Phone.ToLower().Contains(term))
            End If

            Select Case _activeFilter
                Case "Blocked"
                    filtered = filtered.Where(Function(a) a.IsBlocked)
                Case "WithBalance"
                    filtered = filtered.Where(Function(a) a.CurrentBalance > 0D)
                Case "Cleared"
                    filtered = filtered.Where(Function(a) a.CurrentBalance = 0D)
            End Select

            Accounts.Clear()
            For Each account In filtered.OrderBy(Function(a) a.CustomerName)
                Accounts.Add(account)
            Next
            
            RefreshFilterChips()
            OnPropertyChanged(NameOf(TotalAccounts))
            OnPropertyChanged(NameOf(IsEmpty))
        End Sub

        Private Sub SetFilter(filterName As String)
            ActiveFilter = filterName
        End Sub

        Private Async Function SelectAccountAsync(account As CreditAccount) As Task
            If account Is Nothing Then Return
            IsBusy = True
            Try
                Await LoadHistoryInternalAsync(account)
            Catch ex As Exception
                ShowError("Failed to load account history: " & ex.Message)
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ClearAddAccountForm()
            IsAddAccountVisible = False
            _newAccountName = String.Empty
            _newAccountPhone = String.Empty
            _newAccountAddress = String.Empty
            ClearErrors(NameOf(NewAccountName))
            ClearErrors(NameOf(NewAccountPhone))
            OnPropertyChanged(NameOf(NewAccountName))
            OnPropertyChanged(NameOf(NewAccountPhone))
            OnPropertyChanged(NameOf(NewAccountAddress))
            ConfirmAddAccountCommand.NotifyCanExecuteChanged()
        End Sub

        Private Function CanConfirmAddAccount() As Boolean
            Return Not String.IsNullOrWhiteSpace(NewAccountName) AndAlso
                   Not String.IsNullOrWhiteSpace(NewAccountPhone) AndAlso
                   Not HasErrors
        End Function

        Private Async Function ConfirmAddAccountAsync() As Task
            ValidateAllProperties()
            If HasErrors Then Return

            IsBusy = True
            StatusMessage = String.Empty
            Try
                Dim addr = If(String.IsNullOrWhiteSpace(NewAccountAddress), Nothing, NewAccountAddress.Trim())
                Await _creditService.CreateAccountAsync(NewAccountName.Trim(), NewAccountPhone.Trim(), addr)
                ClearAddAccountForm()
                Await LoadDataInternalAsync()
                ShowSuccess("Account created successfully.")
                _notifications.ShowSuccess("Credit account created.")
            Catch ex As Exception
                ShowError("Failed to create account: " & ex.Message)
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub OpenPaymentDialog(account As CreditAccount)
            If account Is Nothing Then Return
            PaymentTargetAccount = account
            PaymentAmount = String.Empty
            SelectedPaymentMethodIndex = 0
            StatusMessage = String.Empty
            IsPaymentDialogVisible = True
        End Sub

        Private Function CanRecordPayment() As Boolean
            If PaymentTargetAccount Is Nothing Then Return False
            Dim parsed As Decimal
            If Not Decimal.TryParse(PaymentAmount, parsed) Then Return False
            Return parsed > 0D AndAlso parsed <= PaymentTargetAccount.CurrentBalance
        End Function

        Private Async Function RecordPaymentAsync() As Task
            IsBusy = True
            StatusMessage = String.Empty
            Dim generalError As Boolean = False
            Dim errorMessage As String = String.Empty

            Dim amount = Decimal.Parse(PaymentAmount)
            Dim clearsBalance = (amount = PaymentTargetAccount.CurrentBalance)
            Dim targetId = PaymentTargetAccount.Id

            Dim method As PaymentMethod
            Select Case _selectedPaymentMethodIndex
                Case 1 : method = PaymentMethod.GCash
                Case 2 : method = PaymentMethod.BankTransfer
                Case Else : method = PaymentMethod.Cash
            End Select

            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function() Await _creditService.RecordPaymentAsync(targetId, amount, method, _session.CurrentUsername),
                    Async Function()
                        IsPaymentDialogVisible = False
                        PaymentAmount = String.Empty
                        Await LoadDataInternalAsync()
                        If SelectedAccount IsNot Nothing AndAlso SelectedAccount.Id = targetId Then
                            Dim refreshed = _allAccounts.FirstOrDefault(Function(a) a.Id = targetId)
                            If refreshed IsNot Nothing Then
                                _selectedAccount = refreshed
                                OnPropertyChanged(NameOf(SelectedAccount))
                                OnPropertyChanged(NameOf(HasSelectedAccount))
                                Await LoadHistoryInternalAsync(refreshed)
                            End If
                        End If
                    End Function,
                    _conflictPresenter)

                If saved Then
                    IsPaymentDialogVisible = False
                    PaymentAmount = String.Empty

                    Await LoadDataInternalAsync()

                    If SelectedAccount IsNot Nothing AndAlso SelectedAccount.Id = targetId Then
                        Dim refreshed = _allAccounts.FirstOrDefault(Function(a) a.Id = targetId)
                        If refreshed IsNot Nothing Then
                            _selectedAccount = refreshed
                            OnPropertyChanged(NameOf(SelectedAccount))
                            OnPropertyChanged(NameOf(HasSelectedAccount))
                            Await LoadHistoryInternalAsync(refreshed)
                        End If
                    End If

                    If clearsBalance Then
                        ShowSuccess("Account cleared — credit re-enabled.")
                        _notifications.ShowSuccess("Credit account cleared.")
                    Else
                        ShowSuccess($"Payment of ₱{amount:N2} recorded.")
                        _notifications.ShowSuccess($"Payment of ₱{amount:N2} recorded.")
                    End If
                End If
            Catch ex As Exception
                generalError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If generalError Then
                ShowError("Payment failed: " & errorMessage)
            End If
        End Function

        Private Function CanToggleBlock(account As CreditAccount) As Boolean
            If account Is Nothing Then Return False
            If account.IsBlocked Then
                Return account.CurrentBalance = 0D
            End If
            Return True
        End Function

        Private Async Function ToggleBlockAsync(account As CreditAccount) As Task
            If account Is Nothing Then Return
            If Not CanToggleBlock(account) Then Return

            Dim isBlocking As Boolean = Not account.IsBlocked
            Dim actionTitle As String = If(isBlocking, "Block Credit Account", "Unblock Credit Account")
            Dim actionMsg As String = If(isBlocking,
                $"Are you sure you want to block credit for {account.CustomerName}? They will not be able to charge new sales to credit.",
                $"Are you sure you want to unblock credit for {account.CustomerName}?")
            Dim confirmBtnText As String = If(isBlocking, "_Block", "_Unblock")

            Dim req As New ConfirmationRequest(actionTitle, actionMsg, confirmBtnText, True)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            Dim accountId = account.Id
            Dim customerName = account.CustomerName

            IsBusy = True
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim acc = Await _context.CreditAccounts.FindAsync(accountId)
                        If acc IsNot Nothing Then
                            acc.IsBlocked = isBlocking
                            Await _context.SaveChangesAsync()
                        End If
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Await LoadDataAsync()
                    
                    If SelectedAccount IsNot Nothing AndAlso SelectedAccount.Id = accountId Then
                        Dim refreshed = _allAccounts.FirstOrDefault(Function(a) a.Id = accountId)
                        If refreshed IsNot Nothing Then
                            SelectedAccount = refreshed
                        End If
                    End If

                    Dim hasUndone As Boolean = False
                    Dim actionTime = DateTime.UtcNow
                    Dim undoCallback = Async Sub()
                                           If hasUndone Then Return
                                           If (DateTime.UtcNow - actionTime).TotalSeconds > 8.0 Then
                                               _notifications.ShowWarning("Undo window has expired.")
                                               Return
                                           End If
                                           hasUndone = True

                                           Dim success = False
                                           Dim conflict = False
                                           Dim errMsg = String.Empty
                                           Try
                                               Dim restoreSaved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                                                   Async Function()
                                                       Dim acc = Await _context.CreditAccounts.FindAsync(accountId)
                                                       If acc IsNot Nothing Then
                                                           acc.IsBlocked = Not isBlocking
                                                           Await _context.SaveChangesAsync()
                                                           success = True
                                                       End If
                                                   End Function,
                                                   AddressOf LoadDataAsync,
                                                   _conflictPresenter)
                                           Catch dbEx As Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                                               conflict = True
                                           Catch ex As Exception
                                               errMsg = ex.Message
                                           End Try

                                           If conflict Then
                                               _notifications.ShowError("Could not undo — data was changed elsewhere.")
                                           ElseIf Not String.IsNullOrEmpty(errMsg) Then
                                               _notifications.ShowError($"Undo failed: {errMsg}")
                                           ElseIf success Then
                                               Await LoadDataAsync()
                                               If SelectedAccount IsNot Nothing AndAlso SelectedAccount.Id = accountId Then
                                                   Dim refreshed = _allAccounts.FirstOrDefault(Function(a) a.Id = accountId)
                                                   If refreshed IsNot Nothing Then
                                                       SelectedAccount = refreshed
                                                   End If
                                               End If
                                               _notifications.ShowSuccess(If(isBlocking, $"Credit account for '{customerName}' unblocked.", $"Credit account for '{customerName}' blocked."))
                                           Else
                                               _notifications.ShowError("Could not undo.")
                                           End If
                                       End Sub

                    Dim undoAction = New NotificationAction("Undo", undoCallback)
                    _notifications.ShowSuccess(If(isBlocking, $"Credit account for '{customerName}' blocked.", $"Credit account for '{customerName}' unblocked."), undoAction)
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ShowSuccess(message As String)
            IsStatusSuccess = True
            StatusMessage = message
        End Sub

        Private Sub ShowError(message As String)
            IsStatusSuccess = False
            StatusMessage = message
        End Sub

    End Class

End Namespace
