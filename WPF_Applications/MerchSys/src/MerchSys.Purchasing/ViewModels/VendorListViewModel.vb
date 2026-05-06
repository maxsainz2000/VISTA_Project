Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services

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

        Private ReadOnly _vendorService As IVendorService
        Private _allVendors As List(Of Vendor) = New List(Of Vendor)()

        Public Sub New(vendorService As IVendorService)
            _vendorService = vendorService

            Vendors = New ObservableCollection(Of Vendor)()
            RecentPOs = New ObservableCollection(Of POSummaryRow)()
            Editor = New VendorEditorViewModel()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            AddVendorCommand = New AsyncRelayCommand(AddressOf OpenNewEditorAsync)
            EditVendorCommand = New AsyncRelayCommand(AddressOf OpenEditEditorAsync, Function() SelectedVendor IsNot Nothing)
            DeleteVendorCommand = New AsyncRelayCommand(AddressOf DeleteSelectedAsync, Function() SelectedVendor IsNot Nothing)
            SaveVendorCommand = New AsyncRelayCommand(AddressOf SaveVendorAsync, Function() IsEditorOpen)
            CancelEditorCommand = New RelayCommand(AddressOf CloseEditor)

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
                    EditVendorCommand.NotifyCanExecuteChanged()
                    DeleteVendorCommand.NotifyCanExecuteChanged()
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
                If SetProperty(_searchText, value) Then ApplyFilter()
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
                    SaveVendorCommand.NotifyCanExecuteChanged()
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
        Public Property AddVendorCommand As AsyncRelayCommand
        Public Property EditVendorCommand As AsyncRelayCommand
        Public Property DeleteVendorCommand As AsyncRelayCommand
        Public Property SaveVendorCommand As AsyncRelayCommand
        Public Property CancelEditorCommand As RelayCommand

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsBusy = True
            Try
                _allVendors = Await _vendorService.GetAllAsync()
                ApplyFilter()
                StatusMessage = $"Loaded {_allVendors.Count} vendors"
            Finally
                IsBusy = False
            End Try
        End Function

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

            IsBusy = True
            Try
                If Editor.IsNewVendor Then
                    Dim created = Await _vendorService.CreateAsync(Editor.ToCreateDto())
                    CloseEditor()
                    Await LoadDataAsync()
                    StatusMessage = $"Vendor '{created.Name}' created."
                Else
                    Dim updated = Await _vendorService.UpdateAsync(Editor.EditingVendorId.Value, Editor.ToUpdateDto())
                    CloseEditor()
                    Await LoadDataAsync()
                    StatusMessage = $"Vendor '{updated.Name}' updated."
                End If
            Catch ex As InvalidOperationException
                Editor.ValidationError = ex.Message
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function DeleteSelectedAsync() As Task
            If SelectedVendor Is Nothing Then Return
            Dim vendorName = SelectedVendor.Name
            IsBusy = True
            Try
                Await _vendorService.DeleteAsync(SelectedVendor.Id)
                ClearVendorDetail()
                Await LoadDataAsync()
                StatusMessage = $"Vendor '{vendorName}' deleted."
            Catch ex As InvalidOperationException
                StatusMessage = $"Delete failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
