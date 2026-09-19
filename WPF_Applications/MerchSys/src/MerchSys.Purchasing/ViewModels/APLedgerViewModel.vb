Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Presentation

Namespace ViewModels

    ''' <summary>Flat display row for the AP Ledger grid.</summary>
    Public Class APLedgerRow
        Public Property Id As Integer
        Public Property VendorId As Integer
        Public Property VendorName As String
        Public Property InvoiceNumber As String
        Public Property InvoiceDate As DateTime
        Public Property DueDate As DateTime
        Public Property TotalAmount As Decimal
        Public Property AmountPaid As Decimal
        Public Property Balance As Decimal
        Public Property IsPaid As Boolean

        Public ReadOnly Property IsOverdue As Boolean
            Get
                Return Not IsPaid AndAlso DueDate.Date < DateTime.UtcNow.Date
            End Get
        End Property
    End Class

    ''' <summary>Item for the vendor filter ComboBox. VendorId = 0 means "All Vendors".</summary>
    Public Class VendorSelectorItem
        Public Property VendorId As Integer
        Public Property DisplayName As String
    End Class

    ''' <summary>
    ''' ViewModel for the AP Ledger screen.
    ''' Displays all vendor invoices with outstanding balances, overdue row highlighting,
    ''' status filters, vendor filter, and an inline payment recording dialog.
    ''' </summary>
    Public Class APLedgerViewModel
        Inherits ObservableObject
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

        Private ReadOnly _session As ISessionService
        Private ReadOnly _apService As IAccountsPayableService
        Private _allRows As List(Of APLedgerRow) = New List(Of APLedgerRow)()
        Private _payingEntryId As Integer

        ' Session memory fields
        Private Shared _savedActiveFilter As String = "All"
        Private Shared _savedVendorId As Integer = 0
        Private Shared _lastUser As String = Nothing

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)

        Public ReadOnly Property TotalAPRows As Integer
            Get
                Return _allRows.Count
            End Get
        End Property

        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _notifications As INotificationService

        ''' <summary>
        ''' True when the current role is Manager. Owner role receives read-only access (DA5 UI enforcement).
        ''' Binds to IsEnabled on the Record Payment button in APLedgerView.
        ''' </summary>
        Public ReadOnly Property CanEdit As Boolean
            Get
                Return _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer
            End Get
        End Property

        Public Sub New(session As ISessionService, apService As IAccountsPayableService, conflictPresenter As IConflictPresenter, notifications As INotificationService)
            _session = session
            _apService = apService
            _conflictPresenter = conflictPresenter
            _notifications = notifications

            ' Restore session filters
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedActiveFilter = "All"
                _savedVendorId = 0
                _lastUser = currentUser
            End If

            _activeFilter = _savedActiveFilter

            Entries = New ObservableCollection(Of APLedgerRow)()
            VendorItems = New ObservableCollection(Of VendorSelectorItem)()
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)
            SetFilterCommand = New RelayCommand(Of String)(Sub(filterName) ActiveFilter = filterName)
            OpenPaymentDialogCommand = New RelayCommand(
                AddressOf OpenPaymentDialog,
                Function() SelectedEntry IsNot Nothing AndAlso Not SelectedEntry.IsPaid)
            ConfirmPaymentCommand = New AsyncRelayCommand(
                AddressOf ConfirmPaymentAsync,
                Function() IsPaymentDialogOpen AndAlso Not IsBusy)
            CancelPaymentCommand = New RelayCommand(AddressOf ClosePaymentDialog)

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Grid Data ────────────────────────────────────────────────────────────

        Public Property Entries As ObservableCollection(Of APLedgerRow)

        Private _selectedEntry As APLedgerRow
        Public Property SelectedEntry As APLedgerRow
            Get
                Return _selectedEntry
            End Get
            Set(value As APLedgerRow)
                If SetProperty(_selectedEntry, value) Then
                    OpenPaymentDialogCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Summary Header ───────────────────────────────────────────────────────

        Private _totalOutstanding As Decimal
        Public Property TotalOutstanding As Decimal
            Get
                Return _totalOutstanding
            End Get
            Set(value As Decimal)
                SetProperty(_totalOutstanding, value)
            End Set
        End Property

        ' ─── Filters ─────────────────────────────────────────────────────────────

        Private _activeFilter As String = "All"
        Public Property ActiveFilter As String
            Get
                Return _activeFilter
            End Get
            Set(value As String)
                If SetProperty(_activeFilter, value) Then
                    _savedActiveFilter = value
                    OnPropertyChanged(NameOf(IsAllFilterActive))
                    OnPropertyChanged(NameOf(IsOutstandingFilterActive))
                    OnPropertyChanged(NameOf(IsOverdueFilterActive))
                    OnPropertyChanged(NameOf(IsPaidFilterActive))
                    ApplyFilter()
                End If
            End Set
        End Property

        Public ReadOnly Property IsAllFilterActive As Boolean
            Get
                Return _activeFilter = "All"
            End Get
        End Property

        Public ReadOnly Property IsOutstandingFilterActive As Boolean
            Get
                Return _activeFilter = "Outstanding"
            End Get
        End Property

        Public ReadOnly Property IsOverdueFilterActive As Boolean
            Get
                Return _activeFilter = "Overdue"
            End Get
        End Property

        Public ReadOnly Property IsPaidFilterActive As Boolean
            Get
                Return _activeFilter = "Paid"
            End Get
        End Property

        Public Property VendorItems As ObservableCollection(Of VendorSelectorItem)

        Private _selectedVendorItem As VendorSelectorItem
        Public Property SelectedVendorItem As VendorSelectorItem
            Get
                Return _selectedVendorItem
            End Get
            Set(value As VendorSelectorItem)
                If SetProperty(_selectedVendorItem, value) Then
                    If value IsNot Nothing Then
                        _savedVendorId = value.VendorId
                    End If
                    ApplyFilter()
                End If
            End Set
        End Property

        ' ─── Status ───────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                If SetProperty(_isBusy, value) Then
                    ConfirmPaymentCommand.NotifyCanExecuteChanged()
                    OnPropertyChanged(NameOf(IsEmpty))
                End If
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Set(value As Boolean)
                SetProperty(_isError, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return Entries.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
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

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property RefreshCommand As AsyncRelayCommand
        Public Property ClearFiltersCommand As RelayCommand
        Public Property SetFilterCommand As RelayCommand(Of String)
        Public Property OpenPaymentDialogCommand As RelayCommand
        Public Property ConfirmPaymentCommand As AsyncRelayCommand
        Public Property CancelPaymentCommand As RelayCommand

        ' ─── Payment Dialog State ─────────────────────────────────────────────────

        Private _isPaymentDialogOpen As Boolean
        Public Property IsPaymentDialogOpen As Boolean
            Get
                Return _isPaymentDialogOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isPaymentDialogOpen, value) Then
                    ConfirmPaymentCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _paymentDialogVendorName As String = String.Empty
        Public Property PaymentDialogVendorName As String
            Get
                Return _paymentDialogVendorName
            End Get
            Set(value As String)
                SetProperty(_paymentDialogVendorName, value)
            End Set
        End Property

        Private _paymentDialogInvoice As String = String.Empty
        Public Property PaymentDialogInvoice As String
            Get
                Return _paymentDialogInvoice
            End Get
            Set(value As String)
                SetProperty(_paymentDialogInvoice, value)
            End Set
        End Property

        Private _paymentDialogBalance As Decimal
        Public Property PaymentDialogBalance As Decimal
            Get
                Return _paymentDialogBalance
            End Get
            Set(value As Decimal)
                SetProperty(_paymentDialogBalance, value)
            End Set
        End Property

        Private _paymentAmountText As String = String.Empty
        Public Property PaymentAmountText As String
            Get
                Return _paymentAmountText
            End Get
            Set(value As String)
                SetProperty(_paymentAmountText, value)
            End Set
        End Property

        Private _paymentError As String = String.Empty
        Public Property PaymentError As String
            Get
                Return _paymentError
            End Get
            Set(value As String)
                If SetProperty(_paymentError, value) Then
                    OnPropertyChanged(NameOf(HasPaymentError))
                End If
            End Set
        End Property

        Public ReadOnly Property HasPaymentError As Boolean
            Get
                Return Not String.IsNullOrEmpty(_paymentError)
            End Get
        End Property

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim apEntries = Await _apService.GetAllAsync()

                _allRows = apEntries.Select(Function(apEnt) New APLedgerRow With {
                    .Id = apEnt.Id,
                    .VendorId = apEnt.VendorId,
                    .VendorName = If(apEnt.Vendor IsNot Nothing, apEnt.Vendor.Name, $"Vendor #{apEnt.VendorId}"),
                    .InvoiceNumber = apEnt.InvoiceNumber,
                    .InvoiceDate = apEnt.InvoiceDate,
                    .DueDate = apEnt.DueDate,
                    .TotalAmount = apEnt.TotalAmount,
                    .AmountPaid = apEnt.AmountPaid,
                    .Balance = apEnt.Balance,
                    .IsPaid = apEnt.IsPaid
                }).ToList()

                RebuildVendorFilter()
                ApplyFilter()
                TotalOutstanding = Await _apService.GetTotalOutstandingAsync()
                StatusMessage = $"Loaded {_allRows.Count} invoice(s)."
                IsError = False
                LastLoadedAt = DateTime.Now
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub RebuildVendorFilter()
            Dim prevVendorId As Integer = If(_selectedVendorItem IsNot Nothing, _selectedVendorItem.VendorId, 0)

            VendorItems.Clear()
            VendorItems.Add(New VendorSelectorItem With {.VendorId = 0, .DisplayName = "All Vendors"})

            Dim distinctVendors = _allRows.
                GroupBy(Function(apR) apR.VendorId).
                Select(Function(grp) New VendorSelectorItem With {
                    .VendorId = grp.Key,
                    .DisplayName = grp.First().VendorName
                }).
                OrderBy(Function(vi) vi.DisplayName).
                ToList()

            For Each vendorSel In distinctVendors
                VendorItems.Add(vendorSel)
            Next

            Dim targetVendorId As Integer = If(_selectedVendorItem IsNot Nothing, _selectedVendorItem.VendorId, _savedVendorId)
            Dim restoredItem = VendorItems.FirstOrDefault(Function(vi) vi.VendorId = targetVendorId)
            _selectedVendorItem = If(restoredItem IsNot Nothing, restoredItem, VendorItems(0))
            OnPropertyChanged(NameOf(SelectedVendorItem))
        End Sub

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return (_activeFilter <> "All") OrElse (_selectedVendorItem IsNot Nothing AndAlso _selectedVendorItem.VendorId <> 0)
            End Get
        End Property

        Private Sub RefreshFilterChips()
            If ActiveFilterChips Is Nothing Then
                ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Else
                ActiveFilterChips.Clear()
            End If

            If _activeFilter <> "All" Then
                ActiveFilterChips.Add(New FilterChipItem($"Status: {_activeFilter}", "Status", New RelayCommand(Sub() ActiveFilter = "All")))
            End If

            If _selectedVendorItem IsNot Nothing AndAlso _selectedVendorItem.VendorId <> 0 Then
                Dim selectedVendor = _selectedVendorItem
                ActiveFilterChips.Add(New FilterChipItem($"Vendor: {selectedVendor.DisplayName}", "Vendor", New RelayCommand(Sub() SelectedVendorItem = VendorItems.FirstOrDefault(Function(v) v.VendorId = 0))))
            End If

            OnPropertyChanged(NameOf(IsFilterActive))
        End Sub

        Private Sub ClearFilters()
            _activeFilter = "All"
            _savedActiveFilter = "All"
            OnPropertyChanged(NameOf(ActiveFilter))
            OnPropertyChanged(NameOf(IsAllFilterActive))
            OnPropertyChanged(NameOf(IsOutstandingFilterActive))
            OnPropertyChanged(NameOf(IsOverdueFilterActive))
            OnPropertyChanged(NameOf(IsPaidFilterActive))
            
            Dim allVendorsItem = VendorItems.FirstOrDefault(Function(vi) vi.VendorId = 0)
            _selectedVendorItem = allVendorsItem
            _savedVendorId = 0
            OnPropertyChanged(NameOf(SelectedVendorItem))

            ApplyFilter()
        End Sub

        Private Sub ApplyFilter()
            Dim filtered As IEnumerable(Of APLedgerRow) = _allRows

            Select Case _activeFilter
                Case "Outstanding"
                    filtered = filtered.Where(Function(apR) Not apR.IsPaid)
                Case "Overdue"
                    filtered = filtered.Where(Function(apR) apR.IsOverdue)
                Case "Paid"
                    filtered = filtered.Where(Function(apR) apR.IsPaid)
            End Select

            If _selectedVendorItem IsNot Nothing AndAlso _selectedVendorItem.VendorId <> 0 Then
                Dim targetId As Integer = _selectedVendorItem.VendorId
                filtered = filtered.Where(Function(apR) apR.VendorId = targetId)
            End If

            Entries.Clear()
            For Each apRow In filtered.OrderBy(Function(apR) apR.DueDate)
                Entries.Add(apRow)
            Next
            
            RefreshFilterChips()
            OnPropertyChanged(NameOf(TotalAPRows))
            OnPropertyChanged(NameOf(IsEmpty))
        End Sub

        ' ─── Payment Dialog Actions ───────────────────────────────────────────────

        Private Sub OpenPaymentDialog()
            If SelectedEntry Is Nothing OrElse SelectedEntry.IsPaid Then Return

            _payingEntryId = SelectedEntry.Id
            PaymentDialogVendorName = SelectedEntry.VendorName
            PaymentDialogInvoice = SelectedEntry.InvoiceNumber
            PaymentDialogBalance = SelectedEntry.Balance
            PaymentAmountText = String.Empty
            PaymentError = String.Empty
            IsPaymentDialogOpen = True
        End Sub

        Private Async Function ConfirmPaymentAsync() As Task
            Dim parsedAmount As Decimal
            If Not Decimal.TryParse(PaymentAmountText, parsedAmount) OrElse parsedAmount <= 0D Then
                PaymentError = "Enter a valid payment amount greater than zero."
                Return
            End If

            If parsedAmount > PaymentDialogBalance Then
                PaymentError = $"Amount exceeds outstanding balance of ₱{PaymentDialogBalance:N2}."
                Return
            End If

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty

            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function() Await _apService.RecordPaymentAsync(_payingEntryId, parsedAmount),
                    Async Function()
                        ClosePaymentDialog()
                        Await LoadDataAsync()
                    End Function,
                    _conflictPresenter)

                If saved Then
                    ClosePaymentDialog()
                    Await LoadDataAsync()
                    StatusMessage = $"Payment of ₱{parsedAmount:N2} recorded."
                    _notifications.ShowSuccess($"Payment of ₱{parsedAmount:N2} recorded.")
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                PaymentError = errorMessage
            End If
        End Function

        Private Sub ClosePaymentDialog()
            IsPaymentDialogOpen = False
            PaymentAmountText = String.Empty
            PaymentError = String.Empty
        End Sub

    End Class

End Namespace
