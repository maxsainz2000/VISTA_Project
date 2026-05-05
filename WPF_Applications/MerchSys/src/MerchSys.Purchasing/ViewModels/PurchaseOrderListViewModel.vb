Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums

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

        Private ReadOnly _poService As IPurchaseOrderService
        Private ReadOnly _vendorService As IVendorService

        Private _allOrders As List(Of PORowItem) = New List(Of PORowItem)()
        Private _vendorList As List(Of Vendor) = New List(Of Vendor)()

        Public Sub New(poService As IPurchaseOrderService, vendorService As IVendorService)
            _poService = poService
            _vendorService = vendorService

            Orders = New ObservableCollection(Of PORowItem)()
            StatusOptions = New ObservableCollection(Of String) From {"All", "Draft", "Submitted", "Received", "Verified", "Closed"}
            Editor = New PurchaseOrderEditorViewModel()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            NewPOCommand = New AsyncRelayCommand(AddressOf OpenNewEditorAsync)
            EditPOCommand = New AsyncRelayCommand(AddressOf OpenEditEditorAsync, Function() CanEditSelected())
            SubmitPOCommand = New AsyncRelayCommand(AddressOf SubmitSelectedAsync, Function() CanSubmitSelected())
            DeletePOCommand = New AsyncRelayCommand(AddressOf DeleteSelectedAsync, Function() CanDeleteSelected())
            SaveDraftCommand = New AsyncRelayCommand(AddressOf SaveDraftAsync, Function() IsEditorOpen)
            SubmitEditorCommand = New AsyncRelayCommand(AddressOf SubmitFromEditorAsync, Function() IsEditorOpen)
            CancelEditorCommand = New RelayCommand(AddressOf CloseEditor)

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
                    EditPOCommand.NotifyCanExecuteChanged()
                    SubmitPOCommand.NotifyCanExecuteChanged()
                    DeletePOCommand.NotifyCanExecuteChanged()
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
                If SetProperty(_selectedStatus, value) Then ApplyFilters()
            End Set
        End Property

        Private _searchText As String = String.Empty
        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(value As String)
                If SetProperty(_searchText, value) Then ApplyFilters()
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
                    SaveDraftCommand.NotifyCanExecuteChanged()
                    SubmitEditorCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Role ─────────────────────────────────────────────────────────────────

        Private _isManager As Boolean = True
        Public Property IsManager As Boolean
            Get
                Return _isManager
            End Get
            Set(value As Boolean)
                SetProperty(_isManager, value)
            End Set
        End Property

        ' ─── Status ───────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
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
            IsBusy = True
            Try
                Dim poTask = _poService.GetAllAsync()
                Dim vendorTask = _vendorService.GetAllAsync()
                Await Task.WhenAll(poTask, vendorTask)

                Dim pos As List(Of PurchaseOrder) = poTask.Result
                _vendorList = vendorTask.Result
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
                StatusMessage = $"Loaded {_allOrders.Count} purchase orders"
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
        End Sub

        ' ─── Editor Lifecycle ─────────────────────────────────────────────────────

        Private Async Function OpenNewEditorAsync() As Task
            If _vendorList.Count = 0 Then
                Await LoadDataAsync()
            End If
            Editor.PrepareForNew(_vendorList)
            IsEditorOpen = True
        End Function

        Private Async Function OpenEditEditorAsync() As Task
            If SelectedOrder Is Nothing Then Return
            IsBusy = True
            Try
                Dim po = Await _poService.GetByIdAsync(SelectedOrder.Id)
                If po IsNot Nothing Then
                    Editor.LoadFromPO(po, _vendorList)
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
            IsBusy = True
            Try
                Dim orderNum = SelectedOrder.OrderNumber
                Await _poService.SubmitAsync(SelectedOrder.Id)
                Await LoadDataAsync()
                StatusMessage = $"PO {orderNum} submitted."
            Catch ex As InvalidOperationException
                StatusMessage = $"Submit failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function DeleteSelectedAsync() As Task
            If SelectedOrder Is Nothing Then Return
            IsBusy = True
            Try
                Await _poService.DeleteDraftAsync(SelectedOrder.Id)
                Await LoadDataAsync()
                StatusMessage = "Draft deleted."
            Catch ex As InvalidOperationException
                StatusMessage = $"Delete failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Editor Save / Submit ─────────────────────────────────────────────────

        Private Async Function SaveDraftAsync() As Task
            If Not IsEditorOpen Then Return
            If Editor.SelectedVendor Is Nothing Then
                StatusMessage = "Please select a vendor."
                Return
            End If
            If Not Editor.LineItems.Any() Then
                StatusMessage = "Please add at least one line item."
                Return
            End If
            IsBusy = True
            Try
                Dim lineDtos = Editor.ToLineDtos()
                If Editor.IsNewPO Then
                    Await _poService.CreateDraftAsync(Editor.SelectedVendor.Id, lineDtos,
                                                      Editor.Notes, Editor.ExpectedDeliveryDate)
                Else
                    Await _poService.UpdateDraftAsync(Editor.EditingPOId.Value, lineDtos,
                                                      Editor.Notes, Editor.ExpectedDeliveryDate)
                End If
                CloseEditor()
                Await LoadDataAsync()
                StatusMessage = "Draft saved."
            Catch ex As InvalidOperationException
                StatusMessage = $"Save failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function SubmitFromEditorAsync() As Task
            If Not IsEditorOpen Then Return
            If Editor.SelectedVendor Is Nothing Then
                StatusMessage = "Please select a vendor."
                Return
            End If
            If Not Editor.LineItems.Any() Then
                StatusMessage = "Please add at least one line item."
                Return
            End If
            IsBusy = True
            Try
                Dim lineDtos = Editor.ToLineDtos()
                Dim savedPO As PurchaseOrder
                If Editor.IsNewPO Then
                    savedPO = Await _poService.CreateDraftAsync(Editor.SelectedVendor.Id, lineDtos,
                                                                Editor.Notes, Editor.ExpectedDeliveryDate)
                Else
                    savedPO = Await _poService.UpdateDraftAsync(Editor.EditingPOId.Value, lineDtos,
                                                                Editor.Notes, Editor.ExpectedDeliveryDate)
                End If
                Await _poService.SubmitAsync(savedPO.Id)
                CloseEditor()
                Await LoadDataAsync()
                StatusMessage = $"PO {savedPO.OrderNumber} submitted."
            Catch ex As InvalidOperationException
                StatusMessage = $"Submit failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
