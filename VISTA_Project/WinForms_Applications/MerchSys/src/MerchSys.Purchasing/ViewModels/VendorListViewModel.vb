Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Enums

Namespace ViewModels

    ''' <summary>
    ''' Flat row for the recent POs grid in the vendor detail panel.
    ''' </summary>
    Public Class POSummaryRow
        Public Property Id As Integer
        Public Property OrderNumber As String
        Public Property OrderDate As DateTime
        Public Property Status As String
        Public Property Total As Decimal
    End Class

    ''' <summary>
    ''' ViewModel for the Vendor Directory screen.
    ''' Hosts the vendor list with real-time search, an inline editor panel,
    ''' and a detail panel showing purchase history for the selected vendor.
    ''' Manager role sees all action buttons; Owner role is read-only.
    ''' </summary>
    Public Class VendorListViewModel
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

        Private ReadOnly _vendorService As IVendorService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _session As ISessionService
        Private ReadOnly _confirmationPresenter As IConfirmationPresenter
        Private ReadOnly _notifications As INotificationService
        Private _allVendors As List(Of Vendor) = New List(Of Vendor)()

        ' Session memory fields
        Private Shared _savedSearchText As String = String.Empty
        Private Shared _lastUser As String = Nothing

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)

        Public ReadOnly Property TotalVendors As Integer
            Get
                Return _allVendors.Count
            End Get
        End Property

        Public Sub New(vendorService As IVendorService, conflictPresenter As IConflictPresenter, session As ISessionService, confirmationPresenter As IConfirmationPresenter, notifications As INotificationService)
            _vendorService = vendorService
            _conflictPresenter = conflictPresenter
            _session = session
            _confirmationPresenter = confirmationPresenter
            _notifications = notifications

            ' Restore session filters
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedSearchText = String.Empty
                _lastUser = currentUser
            End If

            _searchText = _savedSearchText

            Vendors = New ObservableCollection(Of Vendor)()
            RecentPOs = New ObservableCollection(Of POSummaryRow)()
            Editor = New VendorEditorViewModel()
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)

            If IsManager Then
                AddVendorCommand = New AsyncRelayCommand(AddressOf OpenNewEditorAsync)
                EditVendorCommand = New AsyncRelayCommand(AddressOf OpenEditEditorAsync, Function() SelectedVendor IsNot Nothing)
                DeleteVendorCommand = New AsyncRelayCommand(AddressOf DeleteSelectedAsync, Function() SelectedVendor IsNot Nothing)
                SaveVendorCommand = New AsyncRelayCommand(AddressOf SaveVendorAsync, Function() IsEditorOpen)
                CancelEditorCommand = New RelayCommand(AddressOf CloseEditor)
            End If

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Vendor List ──────────────────────────────────────────────────────────

        Public Property Vendors As ObservableCollection(Of Vendor)

        Private _selectedVendor As Vendor
        Public Property SelectedVendor As Vendor
            Get
                Return _selectedVendor
            End Get
            Set(value As Vendor)
                If SetProperty(_selectedVendor, value) Then
                    If EditVendorCommand IsNot Nothing Then EditVendorCommand.NotifyCanExecuteChanged()
                    If DeleteVendorCommand IsNot Nothing Then DeleteVendorCommand.NotifyCanExecuteChanged()
                    If value IsNot Nothing Then
                        Dim detailTask = LoadVendorDetailAsync(value.Id)
                    Else
                        ClearVendorDetail()
                    End If
                End If
            End Set
        End Property

        ' ─── Search ───────────────────────────────────────────────────────────────

        Private _searchText As String = String.Empty
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

        ' ─── Vendor Detail Panel ──────────────────────────────────────────────────

        Private _vendorDetail As VendorDetailDto
        Public Property VendorDetail As VendorDetailDto
            Get
                Return _vendorDetail
            End Get
            Set(value As VendorDetailDto)
                SetProperty(_vendorDetail, value)
            End Set
        End Property

        Private _hasDetailVisible As Boolean
        Public Property HasDetailVisible As Boolean
            Get
                Return _hasDetailVisible
            End Get
            Set(value As Boolean)
                SetProperty(_hasDetailVisible, value)
            End Set
        End Property

        Public Property RecentPOs As ObservableCollection(Of POSummaryRow)

        ' ─── Editor Panel ─────────────────────────────────────────────────────────

        Public Property Editor As VendorEditorViewModel

        Private _isEditorOpen As Boolean
        Public Property IsEditorOpen As Boolean
            Get
                Return _isEditorOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditorOpen, value) Then
                    If SaveVendorCommand IsNot Nothing Then SaveVendorCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Role ─────────────────────────────────────────────────────────────────

        Public ReadOnly Property IsManager As Boolean
            Get
                Return _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer
            End Get
        End Property

        ' ─── Status ───────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
                OnPropertyChanged(NameOf(IsEmpty))
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
                Return Vendors.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
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
        Public Property AddVendorCommand As AsyncRelayCommand
        Public Property EditVendorCommand As AsyncRelayCommand
        Public Property DeleteVendorCommand As AsyncRelayCommand
        Public Property SaveVendorCommand As AsyncRelayCommand
        Public Property CancelEditorCommand As RelayCommand

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                _allVendors = Await _vendorService.GetAllAsync()
                ApplyFilter()
                StatusMessage = $"Loaded {_allVendors.Count} vendors"
                IsError = False
                LastLoadedAt = DateTime.Now
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(SearchText)
            End Get
        End Property

        Private Sub RefreshFilterChips()
            If ActiveFilterChips Is Nothing Then
                ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Else
                ActiveFilterChips.Clear()
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
            ApplyFilter()
        End Sub

        Private Sub ApplyFilter()
            Dim filtered = _allVendors.AsEnumerable()

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                filtered = filtered.Where(Function(v) _
                    v.Name.ToLowerInvariant().Contains(term) OrElse
                    (v.ContactPerson IsNot Nothing AndAlso v.ContactPerson.ToLowerInvariant().Contains(term)) OrElse
                    (v.Phone IsNot Nothing AndAlso v.Phone.ToLowerInvariant().Contains(term)))
            End If

            Vendors.Clear()
            For Each vendor In filtered
                Vendors.Add(vendor)
            Next

            RefreshFilterChips()
            OnPropertyChanged(NameOf(TotalVendors))
            OnPropertyChanged(NameOf(IsEmpty))
        End Sub

        Private Async Function LoadVendorDetailAsync(vendorId As Integer) As Task
            IsBusy = True
            Try
                Dim detail = Await _vendorService.GetVendorWithPurchaseHistoryAsync(vendorId)
                VendorDetail = detail
                RecentPOs.Clear()

                If detail IsNot Nothing AndAlso detail.Vendor IsNot Nothing Then
                    Dim recent = detail.Vendor.PurchaseOrders.
                        OrderByDescending(Function(po) po.OrderDate).
                        Take(10).
                        Select(Function(p) New POSummaryRow With {
                            .Id = p.Id,
                            .OrderNumber = p.OrderNumber,
                            .OrderDate = p.OrderDate,
                            .Status = p.Status.ToString(),
                            .Total = p.TotalAmount
                        }).
                        ToList()

                    For Each row In recent
                        RecentPOs.Add(row)
                    Next

                    HasDetailVisible = True
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ClearVendorDetail()
            VendorDetail = Nothing
            RecentPOs.Clear()
            HasDetailVisible = False
        End Sub

        ' ─── Editor Lifecycle ─────────────────────────────────────────────────────

        Private Async Function OpenNewEditorAsync() As Task
            Editor.PrepareForNew()
            IsEditorOpen = True
            Await Task.CompletedTask
        End Function

        Private Async Function OpenEditEditorAsync() As Task
            If SelectedVendor Is Nothing Then Return
            Editor.LoadFromVendor(SelectedVendor)
            IsEditorOpen = True
            Await Task.CompletedTask
        End Function

        Private Sub CloseEditor()
            IsEditorOpen = False
            Editor.ValidationError = String.Empty
        End Sub

        ' ─── Save / Delete ────────────────────────────────────────────────────────

        Private Async Function SaveVendorAsync() As Task
            If Not IsEditorOpen Then Return
            If Not Editor.Validate() Then Return

            Dim isNew = Editor.IsNewVendor
            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Dim savedVendorName As String = String.Empty
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim result As Vendor
                        If isNew Then
                            result = Await _vendorService.CreateAsync(Editor.ToCreateDto())
                        Else
                            result = Await _vendorService.UpdateAsync(Editor.EditingVendorId.Value, Editor.ToUpdateDto())
                        End If
                        savedVendorName = result.Name
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    CloseEditor()
                    Await LoadDataAsync()
                    StatusMessage = If(isNew,
                        $"Vendor '{savedVendorName}' created.",
                        $"Vendor '{savedVendorName}' updated.")
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                Editor.ValidationError = errorMessage
            End If
        End Function

        Private Async Function DeleteSelectedAsync() As Task
            If SelectedVendor Is Nothing Then Return
            Dim vendorName = SelectedVendor.Name

            Dim req As New ConfirmationRequest("Delete Vendor", $"This will permanently delete the vendor '{vendorName}' and all associated details. This action cannot be undone.", "_Delete", True, vendorName)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function() Await _vendorService.DeleteAsync(SelectedVendor.Id),
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Dim vendorId = SelectedVendor.Id
                    ClearVendorDetail()
                    Await LoadDataAsync()
                    StatusMessage = $"Vendor '{vendorName}' deleted."

                    Dim hasUndone As Boolean = False
                    Dim deleteTime = DateTime.UtcNow
                    Dim undoCallback = Async Sub()
                                           If hasUndone Then Return
                                           If (DateTime.UtcNow - deleteTime).TotalSeconds > 8.0 Then
                                               _notifications.ShowWarning("Undo window has expired.")
                                               Return
                                           End If
                                           hasUndone = True

                                           Dim success = False
                                           Dim conflict = False
                                           Dim errMsg = String.Empty
                                           Try
                                               success = Await _vendorService.RestoreAsync(vendorId)
                                           Catch dbEx As Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                                               conflict = True
                                           Catch ex As Exception
                                               errMsg = ex.Message
                                           End Try

                                           If conflict Then
                                               _notifications.ShowError("Could not undo — data was changed elsewhere.")
                                           ElseIf Not String.IsNullOrEmpty(errMsg) Then
                                               _notifications.ShowError($"Restore failed: {errMsg}")
                                           ElseIf success Then
                                               Await LoadDataAsync()
                                               _notifications.ShowSuccess($"Vendor '{vendorName}' restored.")
                                           Else
                                               _notifications.ShowError("Could not undo.")
                                           End If
                                       End Sub

                    Dim undoAction = New NotificationAction("Undo", undoCallback)
                    _notifications.ShowSuccess($"Vendor '{vendorName}' deleted.", undoAction)
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                StatusMessage = $"Delete failed: {errorMessage}"
            End If
        End Function

    End Class

End Namespace
