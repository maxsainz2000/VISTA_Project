Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace ViewModels

    ''' <summary>
    ''' Lightweight DTO representing a credit-method sale in the account history panel.
    ''' </summary>
    Public Class CreditTransactionItem
        Public Property TransactionDate As DateTime
        Public Property TransactionNumber As String
        Public Property Amount As Decimal
    End Class

    ''' <summary>
    ''' ViewModel for the Credit Management screen.
    ''' Lists all customer credit accounts, supports payment recording, account creation,
    ''' and per-account payment / credit-transaction history.
    ''' </summary>
    Public Class CreditManagementViewModel
        Inherits ObservableObject

        Private ReadOnly _creditService As ICreditService
        Private ReadOnly _context As POSDbContext
        Private ReadOnly _session As ISessionService

        ' Unfiltered master list used for in-memory filtering
        Private _allAccounts As List(Of CreditAccount) = New List(Of CreditAccount)()

        ' ── Backing fields ────────────────────────────────────────────────────────

        Private _totalOutstanding As Decimal
        Private _blockedCount As Integer
        Private _overdueCount As Integer

        Private _accounts As ObservableCollection(Of CreditAccount) = New ObservableCollection(Of CreditAccount)()
        Private _selectedAccount As CreditAccount
        Private _searchText As String = String.Empty
        Private _activeFilter As String = "All"

        Private _paymentHistory As ObservableCollection(Of CreditPayment) = New ObservableCollection(Of CreditPayment)()
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
        Public ReadOnly Property SearchCommand As RelayCommand
        Public ReadOnly Property ApplyFilterCommand As RelayCommand(Of String)
        Public ReadOnly Property AddAccountCommand As RelayCommand
        Public ReadOnly Property CancelAddAccountCommand As RelayCommand
        Public ReadOnly Property ConfirmAddAccountCommand As AsyncRelayCommand
        Public ReadOnly Property SelectAccountCommand As AsyncRelayCommand(Of CreditAccount)
        Public ReadOnly Property OpenPaymentDialogCommand As RelayCommand(Of CreditAccount)
        Public ReadOnly Property RecordPaymentCommand As AsyncRelayCommand
        Public ReadOnly Property CancelPaymentCommand As RelayCommand

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
                SetProperty(_searchText, value)
            End Set
        End Property

        Public Property ActiveFilter As String
            Get
                Return _activeFilter
            End Get
            Private Set(value As String)
                SetProperty(_activeFilter, value)
                OnPropertyChanged(NameOf(IsFilterAll))
                OnPropertyChanged(NameOf(IsFilterBlocked))
                OnPropertyChanged(NameOf(IsFilterWithBalance))
                OnPropertyChanged(NameOf(IsFilterCleared))
            End Set
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

        Public Property IsAddAccountVisible As Boolean
            Get
                Return _isAddAccountVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isAddAccountVisible, value)
            End Set
        End Property

        Public Property NewAccountName As String
            Get
                Return _newAccountName
            End Get
            Set(value As String)
                SetProperty(_newAccountName, value)
                ConfirmAddAccountCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public Property NewAccountPhone As String
            Get
                Return _newAccountPhone
            End Get
            Set(value As String)
                SetProperty(_newAccountPhone, value)
                ConfirmAddAccountCommand.NotifyCanExecuteChanged()
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
            End Set
        End Property

        ''' <summary>Payment method options shown in the Record Payment dialog (Credit is excluded).</summary>
        Public ReadOnly Property PaymentMethodOptions As List(Of String)
            Get
                Return New List(Of String) From {"Cash", "GCash", "Bank Transfer"}
            End Get
        End Property

        ' ── Constructor ───────────────────────────────────────────────────────────

        Public Sub New(creditService As ICreditService, context As POSDbContext, session As ISessionService)
            _creditService = creditService
            _context = context
            _session = session

            LoadDataCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
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

            Dim txns = Await _context.SalesTransactions _
                .Where(Function(t) t.CustomerId.HasValue AndAlso t.CustomerId.Value = account.Id AndAlso
                                   t.PaymentMethod = PaymentMethod.Credit AndAlso
                                   Not t.IsDeleted) _
                .OrderByDescending(Function(t) t.TransactionDate) _
                .ToListAsync()

            CreditTransactions.Clear()
            For Each t In txns
                CreditTransactions.Add(New CreditTransactionItem() With {
                    .TransactionDate = t.TransactionDate,
                    .TransactionNumber = t.TransactionNumber,
                    .Amount = t.TotalAmount
                })
            Next
        End Function

        ' ── Command handlers ──────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsBusy = True
            Try
                Await LoadDataInternalAsync()
            Catch ex As Exception
                ShowError("Failed to load accounts: " & ex.Message)
            Finally
                IsBusy = False
            End Try
        End Function

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
        End Sub

        Private Sub SetFilter(filter As String)
            ActiveFilter = filter
            ApplyFilter()
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
            NewAccountName = String.Empty
            NewAccountPhone = String.Empty
            NewAccountAddress = String.Empty
        End Sub

        Private Function CanConfirmAddAccount() As Boolean
            Return Not String.IsNullOrWhiteSpace(NewAccountName) AndAlso
                   Not String.IsNullOrWhiteSpace(NewAccountPhone)
        End Function

        Private Async Function ConfirmAddAccountAsync() As Task
            IsBusy = True
            StatusMessage = String.Empty
            Try
                Dim addr = If(String.IsNullOrWhiteSpace(NewAccountAddress), Nothing, NewAccountAddress.Trim())
                Await _creditService.CreateAccountAsync(NewAccountName.Trim(), NewAccountPhone.Trim(), addr)
                ClearAddAccountForm()
                Await LoadDataInternalAsync()
                ShowSuccess("Account created successfully.")
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
            Try
                Dim amount = Decimal.Parse(PaymentAmount)
                Dim clearsBalance = (amount = PaymentTargetAccount.CurrentBalance)
                Dim targetId = PaymentTargetAccount.Id

                Dim method As PaymentMethod
                Select Case _selectedPaymentMethodIndex
                    Case 1 : method = PaymentMethod.GCash
                    Case 2 : method = PaymentMethod.BankTransfer
                    Case Else : method = PaymentMethod.Cash
                End Select

                Await _creditService.RecordPaymentAsync(targetId, amount, method, _session.CurrentUsername)

                IsPaymentDialogVisible = False
                PaymentAmount = String.Empty

                Await LoadDataInternalAsync()

                ' Refresh history if the paid account is still selected
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
                Else
                    ShowSuccess($"Payment of ₱{amount:N2} recorded.")
                End If
            Catch ex As Exception
                ShowError("Payment failed: " & ex.Message)
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
