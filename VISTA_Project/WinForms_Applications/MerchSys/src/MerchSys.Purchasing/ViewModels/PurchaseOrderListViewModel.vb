Imports System.Collections.ObjectModel
Imports System.Windows.Input
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation
Imports MerchSys.SharedKernel.Persistence

Namespace ViewModels

    ''' <summary>
    ''' Flat row item for the PO list DataGrid.
    ''' </summary>
    Public Class PORowItem
        Public Property Id As Integer
        Public Property OrderNumber As String
        Public Property VendorName As String
        Public Property Status As String
        Public Property StatusEnum As PurchaseOrderStatus
        Public Property OrderDate As DateTime
        Public Property TotalAmount As Decimal
        Public Property ExpectedDeliveryDate As DateTime?
    End Class

    ''' <summary>
    ''' ViewModel for the PO Management screen.
    ''' Hosts the PO list with status/search filtering and the embedded editor panel.
    ''' Manager role sees all action buttons; Owner role gets read-only access.
    ''' </summary>
    Public Class PurchaseOrderListViewModel
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
        Private ReadOnly _poService As IPurchaseOrderService
        Private ReadOnly _vendorService As IVendorService
        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _vendorProductService As IVendorProductService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _confirmationPresenter As IConfirmationPresenter

        Private _allOrders As List(Of PORowItem) = New List(Of PORowItem)()
        Private _vendorList As List(Of Vendor) = New List(Of Vendor)()

        ' Session memory fields
        Private Shared _savedStatus As String = "All"
        Private Shared _savedSearchText As String = String.Empty
        Private Shared _lastUser As String = Nothing

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)
        Public Property ClearFiltersCommand As RelayCommand

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return (SelectedStatus <> "All") OrElse Not String.IsNullOrWhiteSpace(SearchText)
            End Get
        End Property

        Public ReadOnly Property EmptyStateTitle As String
            Get
                If IsFilterActive Then
                    If Not String.IsNullOrWhiteSpace(SearchText) Then
                        Return $"No results for '{SearchText.Trim()}'"
                    Else
                        Return "No results matching filters"
                    End If
                End If
                Return "No Purchase Orders"
            End Get
        End Property

        Public ReadOnly Property EmptyStateDescription As String
            Get
                If IsFilterActive Then
                    Return "Try adjusting your filters or search term to find what you're looking for."
                End If
                Return "There are no purchase orders in the system."
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionCommand As ICommand
            Get
                If IsFilterActive Then
                    Return ClearFiltersCommand
                End If
                Return NewPOCommand
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionText As String
            Get
                If IsFilterActive Then
                    Return "Clear filters"
                End If
                Return "New PO"
            End Get
        End Property

        Public ReadOnly Property TotalOrders As Integer
            Get
                Return _allOrders.Count
            End Get
        End Property

        Public Sub New(session As ISessionService,
                       poService As IPurchaseOrderService,
                       vendorService As IVendorService,
                       notifications As INotificationService,
                       vendorProductService As IVendorProductService,
                       conflictPresenter As IConflictPresenter,
                       confirmationPresenter As IConfirmationPresenter)
            _session = session
            _poService = poService
            _vendorService = vendorService
            _notifications = notifications
            _vendorProductService = vendorProductService
            _conflictPresenter = conflictPresenter
            _confirmationPresenter = confirmationPresenter

            ' Restore session filters, resetting if user changed
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedStatus = "All"
                _savedSearchText = String.Empty
                _lastUser = currentUser
            End If

            _selectedStatus = _savedStatus
            _searchText = _savedSearchText

            Orders = New ObservableCollection(Of PORowItem)()
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            StatusOptions = New ObservableCollection(Of String) From {"All", "Draft", "Submitted", "Received", "Verified", "Closed"}
            Editor = New PurchaseOrderEditorViewModel()
            AddHandler Editor.PropertyChanged, AddressOf OnEditorPropertyChanged

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)

            If IsManager Then
                NewPOCommand = New AsyncRelayCommand(AddressOf OpenNewEditorAsync)
                EditPOCommand = New AsyncRelayCommand(AddressOf OpenEditEditorAsync, Function() CanEditSelected())
                SubmitPOCommand = New AsyncRelayCommand(AddressOf SubmitSelectedAsync, Function() CanSubmitSelected())
                DeletePOCommand = New AsyncRelayCommand(AddressOf DeleteSelectedAsync, Function() CanDeleteSelected())
                SaveDraftCommand = New AsyncRelayCommand(AddressOf SaveDraftAsync, Function() IsEditorOpen)
                SubmitEditorCommand = New AsyncRelayCommand(AddressOf SubmitFromEditorAsync, Function() IsEditorOpen)
                CancelEditorCommand = New RelayCommand(AddressOf CloseEditor)
            End If

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Orders Grid ─────────────────────────────────────────────────────────

        Public Property Orders As ObservableCollection(Of PORowItem)

        Private _selectedOrder As PORowItem
        Public Property SelectedOrder As PORowItem
            Get
                Return _selectedOrder
            End Get
            Set(value As PORowItem)
                If SetProperty(_selectedOrder, value) Then
                    If EditPOCommand IsNot Nothing Then EditPOCommand.NotifyCanExecuteChanged()
                    If SubmitPOCommand IsNot Nothing Then SubmitPOCommand.NotifyCanExecuteChanged()
                    If DeletePOCommand IsNot Nothing Then DeletePOCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Filters ─────────────────────────────────────────────────────────────

        Public Property StatusOptions As ObservableCollection(Of String)

        Private _selectedStatus As String = "All"
        Public Property SelectedStatus As String
            Get
                Return _selectedStatus
            End Get
            Set(value As String)
                If SetProperty(_selectedStatus, value) Then
                    _savedStatus = value
                    ApplyFilters()
                End If
            End Set
        End Property

        Private _searchText As String = String.Empty
        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then
                    _savedSearchText = value
                    ApplyFilters()
                End If
            End Set
        End Property

        ' ─── Editor Panel ─────────────────────────────────────────────────────────

        Public Property Editor As PurchaseOrderEditorViewModel

        Private _isEditorOpen As Boolean
        Public Property IsEditorOpen As Boolean
            Get
                Return _isEditorOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditorOpen, value) Then
                    If SaveDraftCommand IsNot Nothing Then SaveDraftCommand.NotifyCanExecuteChanged()
                    If SubmitEditorCommand IsNot Nothing Then SubmitEditorCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Role ─────────────────────────────────────────────────────────────────

        ''' <summary>
        ''' True when the current role is Manager. Owner role receives read-only access (DA5 UI enforcement).
        ''' Bound to Visibility of action buttons (New PO, Edit, Submit, Delete) in PurchaseOrderListView.
        ''' </summary>
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
                Return Orders.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
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
        Public Property NewPOCommand As AsyncRelayCommand
        Public Property EditPOCommand As AsyncRelayCommand
        Public Property SubmitPOCommand As AsyncRelayCommand
        Public Property DeletePOCommand As AsyncRelayCommand
        Public Property SaveDraftCommand As AsyncRelayCommand
        Public Property SubmitEditorCommand As AsyncRelayCommand
        Public Property CancelEditorCommand As RelayCommand

        ' ─── CanExecute ───────────────────────────────────────────────────────────

        Private Function CanEditSelected() As Boolean
            Return SelectedOrder IsNot Nothing AndAlso SelectedOrder.StatusEnum = PurchaseOrderStatus.Draft
        End Function

        Private Function CanSubmitSelected() As Boolean
            Return SelectedOrder IsNot Nothing AndAlso SelectedOrder.StatusEnum = PurchaseOrderStatus.Draft
        End Function

        Private Function CanDeleteSelected() As Boolean
            Return SelectedOrder IsNot Nothing AndAlso SelectedOrder.StatusEnum = PurchaseOrderStatus.Draft
        End Function

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                _vendorList = Await _vendorService.GetAllAsync()
                Dim pos As List(Of PurchaseOrder) = Await _poService.GetAllAsync()
                Dim vendorMap = _vendorList.ToDictionary(Function(v) v.Id, Function(v) v.Name)

                _allOrders = pos.
                    Select(Function(po) New PORowItem With {
                        .Id = po.Id,
                        .OrderNumber = po.OrderNumber,
                        .VendorName = If(vendorMap.ContainsKey(po.VendorId), vendorMap(po.VendorId), $"Vendor #{po.VendorId}"),
                        .Status = po.Status.ToString(),
                        .StatusEnum = po.Status,
                        .OrderDate = po.OrderDate,
                        .TotalAmount = po.TotalAmount,
                        .ExpectedDeliveryDate = po.ExpectedDeliveryDate
                    }).
                    OrderByDescending(Function(p) p.OrderDate).
                    ToList()

                Editor.LoadVendors(_vendorList)
                ApplyFilters()
                OnPropertyChanged(NameOf(TotalOrders))
                StatusMessage = $"Loaded {pos.Count} POs, {_vendorList.Count} vendors"
                IsError = False
                LastLoadedAt = DateTime.Now
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ApplyFilters()
            Dim filtered = _allOrders.AsEnumerable()

            If Not String.IsNullOrEmpty(SelectedStatus) AndAlso SelectedStatus <> "All" Then
                filtered = filtered.Where(Function(row) row.Status = SelectedStatus)
            End If

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                filtered = filtered.Where(Function(row) row.OrderNumber.ToLowerInvariant().Contains(term) OrElse
                                                         row.VendorName.ToLowerInvariant().Contains(term))
            End If

            Orders.Clear()
            For Each item In filtered
                Orders.Add(item)
            Next

            RefreshFilterChips()
            OnPropertyChanged(NameOf(IsEmpty))
        End Sub

        Private Sub RefreshFilterChips()
            If ActiveFilterChips Is Nothing Then
                ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Else
                ActiveFilterChips.Clear()
            End If

            If Not String.IsNullOrEmpty(SelectedStatus) AndAlso SelectedStatus <> "All" Then
                ActiveFilterChips.Add(New FilterChipItem($"Status: {SelectedStatus}", "Status", New RelayCommand(Sub() SelectedStatus = "All")))
            End If

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                ActiveFilterChips.Add(New FilterChipItem($"Search: {SearchText.Trim()}", "Search", New RelayCommand(Sub() SearchText = String.Empty)))
            End If

            OnPropertyChanged(NameOf(IsFilterActive))
            OnPropertyChanged(NameOf(EmptyStateTitle))
            OnPropertyChanged(NameOf(EmptyStateDescription))
            OnPropertyChanged(NameOf(EmptyStateActionCommand))
            OnPropertyChanged(NameOf(EmptyStateActionText))
        End Sub

        Private Sub ClearFilters()
            _selectedStatus = "All"
            _searchText = String.Empty

            OnPropertyChanged(NameOf(SelectedStatus))
            OnPropertyChanged(NameOf(SearchText))

            _savedStatus = "All"
            _savedSearchText = String.Empty

            ApplyFilters()
        End Sub

        ' ─── Editor Lifecycle ─────────────────────────────────────────────────────

        Private Async Function OpenNewEditorAsync() As Task
            If _vendorList.Count = 0 Then
                Await LoadDataAsync()
            End If
            Editor.PrepareForNew(_vendorList)
            Editor.ClearEditorErrors()
            IsEditorOpen = True
        End Function

        Private Async Function OpenEditEditorAsync() As Task
            If SelectedOrder Is Nothing Then Return
            IsBusy = True
            Try
                Dim po = Await _poService.GetByIdAsync(SelectedOrder.Id)
                If po IsNot Nothing Then
                    Editor.LoadFromPO(po, _vendorList)
                    Editor.ClearEditorErrors()
                    IsEditorOpen = True
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub CloseEditor()
            IsEditorOpen = False
        End Sub

        ' ─── List Actions ─────────────────────────────────────────────────────────

        Private Async Function SubmitSelectedAsync() As Task
            If SelectedOrder Is Nothing Then Return
            Dim orderNum = SelectedOrder.OrderNumber

            Dim req As New ConfirmationRequest("Submit PO", $"This will submit the purchase order '{orderNum}' to the vendor. It can no longer be edited as a draft.", "_Submit", False)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function() Await _poService.SubmitAsync(SelectedOrder.Id),
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Await LoadDataAsync()
                    StatusMessage = $"PO {orderNum} submitted."
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                StatusMessage = $"Submit failed: {errorMessage}"
            End If
        End Function

        Private Async Function DeleteSelectedAsync() As Task
            If SelectedOrder Is Nothing Then Return

            Dim poId = SelectedOrder.Id
            Dim orderNum = SelectedOrder.OrderNumber

            Dim req As New ConfirmationRequest("Delete Draft PO", $"This will permanently delete the draft purchase order '{orderNum}'.", "_Delete", True)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function() Await _poService.DeleteDraftAsync(poId),
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Await LoadDataAsync()
                    StatusMessage = "Draft deleted."

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
                                           Dim restoreErrMsg = String.Empty
                                           Try
                                               Dim restoreSaved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                                                   Async Function()
                                                       success = Await _poService.RestoreDraftAsync(poId)
                                                   End Function,
                                                   AddressOf LoadDataAsync,
                                                   _conflictPresenter)
                                           Catch dbEx As Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                                               conflict = True
                                           Catch ex As Exception
                                               restoreErrMsg = ex.Message
                                           End Try

                                           If conflict Then
                                               _notifications.ShowError("Could not undo — data was changed elsewhere.")
                                           ElseIf Not String.IsNullOrEmpty(restoreErrMsg) Then
                                               _notifications.ShowError($"Restore failed: {restoreErrMsg}")
                                           ElseIf success Then
                                               Await LoadDataAsync()
                                               _notifications.ShowSuccess($"Draft PO '{orderNum}' restored.")
                                               StatusMessage = $"Draft PO '{orderNum}' restored."
                                           Else
                                               _notifications.ShowError("Could not undo.")
                                           End If
                                       End Sub

                    Dim undoAction = New NotificationAction("Undo", undoCallback)
                    _notifications.ShowSuccess($"Draft PO '{orderNum}' deleted.", undoAction)
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

        ' ─── Editor Save / Submit ─────────────────────────────────────────────────

        Private Async Function SaveDraftAsync() As Task
            If Not IsEditorOpen Then Return
            
            Editor.IsSubmitting = False
            If Not Editor.ValidateAll() Then
                StatusMessage = "Please correct the validation errors."
                Return
            End If
            If Not Editor.LineItems.Any() Then
                StatusMessage = "Please add at least one line item."
                Return
            End If

            ' Pre-save validator: block saving if any line has ProductId = 0
            Dim invalidLines As New List(Of Integer)()
            For i As Integer = 0 To Editor.LineItems.Count - 1
                If Editor.LineItems(i).ProductId = 0 Then
                    invalidLines.Add(i + 1)
                End If
            Next

            If invalidLines.Count > 0 Then
                Dim errorMsg = $"Cannot save: Lines {String.Join(", ", invalidLines)} have no product selected (Product ID is 0)."
                _notifications.ShowError(errorMsg)
                StatusMessage = errorMsg
                Return
            End If

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Try
                Dim lineDtos = Editor.ToLineDtos()

                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        If Editor.IsNewPO Then
                            Await _poService.CreateDraftAsync(Editor.SelectedVendor.Id, lineDtos,
                                                              Editor.Notes, Editor.ExpectedDeliveryDate)
                        Else
                            Await _poService.UpdateDraftAsync(Editor.EditingPOId.Value, lineDtos,
                                                              Editor.Notes, Editor.ExpectedDeliveryDate)
                        End If
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    CloseEditor()
                    Await LoadDataAsync()
                    StatusMessage = "Draft saved."
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                StatusMessage = $"Save failed: {errorMessage}"
            End If
        End Function

        Private Async Function SubmitFromEditorAsync() As Task
            If Not IsEditorOpen Then Return

            Editor.IsSubmitting = True
            If Not Editor.ValidateAll() Then
                Editor.IsSubmitting = False
                StatusMessage = "Please correct the validation errors."
                Return
            End If
            Editor.IsSubmitting = False

            If Not Editor.LineItems.Any() Then
                StatusMessage = "Please add at least one line item."
                Return
            End If

            ' Pre-save validator: block saving if any line has ProductId = 0
            Dim invalidLines As New List(Of Integer)()
            For i As Integer = 0 To Editor.LineItems.Count - 1
                If Editor.LineItems(i).ProductId = 0 Then
                    invalidLines.Add(i + 1)
                End If
            Next

            If invalidLines.Count > 0 Then
                Dim errorMsg = $"Cannot save: Lines {String.Join(", ", invalidLines)} have no product selected (Product ID is 0)."
                _notifications.ShowError(errorMsg)
                StatusMessage = errorMsg
                Return
            End If

            Dim req As New ConfirmationRequest("Submit PO", "This will submit the purchase order to the vendor. It can no longer be edited as a draft.", "_Submit", False)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Dim submittedOrderNumber As String = String.Empty
            Try
                Dim lineDtos = Editor.ToLineDtos()

                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim poResult As PurchaseOrder
                        If Editor.IsNewPO Then
                            poResult = Await _poService.CreateDraftAsync(Editor.SelectedVendor.Id, lineDtos,
                                                                         Editor.Notes, Editor.ExpectedDeliveryDate)
                        Else
                            poResult = Await _poService.UpdateDraftAsync(Editor.EditingPOId.Value, lineDtos,
                                                                         Editor.Notes, Editor.ExpectedDeliveryDate)
                        End If
                        Await _poService.SubmitAsync(poResult.Id)
                        submittedOrderNumber = poResult.OrderNumber
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    CloseEditor()
                    Await LoadDataAsync()
                    StatusMessage = $"PO {submittedOrderNumber} submitted."
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                StatusMessage = $"Submit failed: {errorMessage}"
            End If
        End Function

        Private Async Sub OnEditorPropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
            If e.PropertyName = NameOf(PurchaseOrderEditorViewModel.SelectedVendor) Then
                Await OnSelectedVendorChangedAsync()
            End If
        End Sub

        Private Async Function OnSelectedVendorChangedAsync() As Task
            If Editor.SelectedVendor IsNot Nothing Then
                IsBusy = True
                Try
                    Await Editor.LoadVendorCatalogAsync(_vendorProductService, Editor.SelectedVendor.Id)
                Catch ex As Exception
                    StatusMessage = $"[ERROR] Failed to load catalog: {ex.Message}"
                Finally
                    IsBusy = False
                End Try
            Else
                Editor.VendorCatalog.Clear()
            End If
        End Function

    End Class

End Namespace
