Imports System.Collections.ObjectModel
Imports System.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MediatR
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Queries

Namespace ViewModels

    ''' <summary>
    ''' ViewModel for the Vendor-Product Catalog management panel.
    ''' Supports master-detail selection of vendors and CRUD operations for their supplied products.
    ''' Access restricted to Managers at the database and service layers.
    ''' </summary>
    Public Class VendorCatalogViewModel
        Inherits ObservableObject

        Private ReadOnly _session As ISessionService
        Private ReadOnly _vendorService As IVendorService
        Private ReadOnly _vendorProductService As IVendorProductService
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _notifications As INotificationService

        Public Sub New(session As ISessionService,
                       vendorService As IVendorService,
                       vendorProductService As IVendorProductService,
                       mediator As IMediator,
                       notifications As INotificationService)
            _session = session
            _vendorService = vendorService
            _vendorProductService = vendorProductService
            _mediator = mediator
            _notifications = notifications

            Vendors = New ObservableCollection(Of Vendor)()
            CatalogEntries = New ObservableCollection(Of VendorProductDto)()
            ProductSearchResults = New ObservableCollection(Of ProductLookupDto)()

            LoadVendorsCommand = New AsyncRelayCommand(AddressOf LoadVendorsAsync)
            LoadCatalogCommand = New AsyncRelayCommand(AddressOf LoadCatalogAsync)
            AddProductCommand = New AsyncRelayCommand(AddressOf AddProductAsync, Function() CanModify())
            SaveEntryCommand = New AsyncRelayCommand(Of VendorProductDto)(AddressOf SaveEntryAsync, Function(entry) CanModify())
            DeleteEntryCommand = New AsyncRelayCommand(Of VendorProductDto)(AddressOf DeleteEntryAsync, Function(entry) CanModify())
            SearchProductsCommand = New AsyncRelayCommand(AddressOf SearchProductsAsync)

            Dim initTask = LoadVendorsAsync()
        End Sub

        ' --- Properties ---

        Public Property Vendors As ObservableCollection(Of Vendor)
        Public Property CatalogEntries As ObservableCollection(Of VendorProductDto)
        Public Property ProductSearchResults As ObservableCollection(Of ProductLookupDto)

        Private _selectedVendor As Vendor
        Public Property SelectedVendor As Vendor
            Get
                Return _selectedVendor
            End Get
            Set(value As Vendor)
                If SetProperty(_selectedVendor, value) Then
                    OnPropertyChanged(NameOf(HasSelectedVendor))
                    Dim t = LoadCatalogAsync()
                End If
            End Set
        End Property

        Public ReadOnly Property HasSelectedVendor As Boolean
            Get
                Return _selectedVendor IsNot Nothing
            End Get
        End Property

        Private _selectedEntry As VendorProductDto
        Public Property SelectedEntry As VendorProductDto
            Get
                Return _selectedEntry
            End Get
            Set(value As VendorProductDto)
                SetProperty(_selectedEntry, value)
            End Set
        End Property

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                If SetProperty(_isBusy, value) Then
                    AddProductCommand.NotifyCanExecuteChanged()
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

        ' Product Lookup Dialog properties
        Private _productSearchTerm As String = String.Empty
        Public Property ProductSearchTerm As String
            Get
                Return _productSearchTerm
            End Get
            Set(value As String)
                SetProperty(_productSearchTerm, value)
            End Set
        End Property

        Private _selectedProductResult As ProductLookupDto
        Public Property SelectedProductResult As ProductLookupDto
            Get
                Return _selectedProductResult
            End Get
            Set(value As ProductLookupDto)
                SetProperty(_selectedProductResult, value)
            End Set
        End Property

        Private _newEntryCost As Decimal = 0D
        Public Property NewEntryCost As Decimal
            Get
                Return _newEntryCost
            End Get
            Set(value As Decimal)
                SetProperty(_newEntryCost, value)
            End Set
        End Property

        Private _newEntryNotes As String = String.Empty
        Public Property NewEntryNotes As String
            Get
                Return _newEntryNotes
            End Get
            Set(value As String)
                SetProperty(_newEntryNotes, value)
            End Set
        End Property

        Private _isSearchDialogOpen As Boolean
        Public Property IsSearchDialogOpen As Boolean
            Get
                Return _isSearchDialogOpen
            End Get
            Set(value As Boolean)
                SetProperty(_isSearchDialogOpen, value)
            End Set
        End Property

        Public ReadOnly Property IsManager As Boolean
            Get
                Return _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer
            End Get
        End Property

        Private Function CanModify() As Boolean
            Return IsManager AndAlso SelectedVendor IsNot Nothing
        End Function

        ' --- Commands ---
        Public Property LoadVendorsCommand As AsyncRelayCommand
        Public Property LoadCatalogCommand As AsyncRelayCommand
        Public Property AddProductCommand As AsyncRelayCommand
        Public Property SaveEntryCommand As AsyncRelayCommand(Of VendorProductDto)
        Public Property DeleteEntryCommand As AsyncRelayCommand(Of VendorProductDto)
        Public Property SearchProductsCommand As AsyncRelayCommand

        ' --- Implementations ---

        Public Async Function LoadVendorsAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim list = Await _vendorService.GetAllAsync()
                Vendors.Clear()
                For Each v In list
                    Vendors.Add(v)
                Next
                StatusMessage = $"Loaded {Vendors.Count} vendors."
                IsError = False
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            End Try
            IsBusy = False
        End Function

        Public Async Function LoadCatalogAsync() As Task
            If SelectedVendor Is Nothing Then
                CatalogEntries.Clear()
                Return
            End If

            IsBusy = True
            Try
                Dim entries = Await _vendorProductService.GetCatalogForVendorAsync(SelectedVendor.Id)
                CatalogEntries.Clear()
                For Each entry In entries
                    CatalogEntries.Add(entry)
                Next
                StatusMessage = $"Loaded {CatalogEntries.Count} catalog entries for {SelectedVendor.Name}."
                AddProductCommand.NotifyCanExecuteChanged()
            Catch ex As Exception
                StatusMessage = $"[ERROR] Failed to load catalog: {ex.Message}"
                _notifications.ShowError("Failed to load vendor's product catalog.")
            End Try
            IsBusy = False
        End Function

        Public Async Function SearchProductsAsync() As Task
            IsBusy = True
            Try
                Dim query As New GetProductsForCatalogQuery With {.SearchTerm = ProductSearchTerm}
                Dim results = Await _mediator.Send(query)
                ProductSearchResults.Clear()
                For Each r In results
                    ProductSearchResults.Add(r)
                Next
            Catch ex As Exception
                _notifications.ShowError($"Search failed: {ex.Message}")
            End Try
            IsBusy = False
        End Function

        Public Async Function AddProductAsync() As Task
            If SelectedVendor Is Nothing Then Return
            If SelectedProductResult Is Nothing Then
                _notifications.ShowError("Please select a product from search results.")
                Return
            End If

            IsBusy = True
            Try
                Await _vendorProductService.AddCatalogEntryAsync(
                    SelectedVendor.Id,
                    SelectedProductResult.Id,
                    SelectedProductResult.Name,
                    0D,
                    NewEntryNotes
                )
                _notifications.ShowSuccess($"Added {SelectedProductResult.Name} to catalog.")
                IsSearchDialogOpen = False
                ProductSearchTerm = String.Empty
                ProductSearchResults.Clear()
                NewEntryCost = 0D
                NewEntryNotes = String.Empty
                SelectedProductResult = Nothing
                Await LoadCatalogAsync()
            Catch ex As Exception
                _notifications.ShowError($"Failed to add product: {ex.Message}")
            End Try
            IsBusy = False
        End Function

        Public Async Function SaveEntryAsync(entry As VendorProductDto) As Task
            If entry Is Nothing Then Return

            IsBusy = True
            Try
                Await _vendorProductService.UpdateCatalogEntryAsync(entry.Id, entry.LastUnitCost, entry.Notes)
                _notifications.ShowSuccess($"Updated notes for {entry.ProductName}.")
                Await LoadCatalogAsync()
            Catch ex As Exception
                _notifications.ShowError($"Update failed: {ex.Message}")
            End Try
            IsBusy = False
        End Function

        Public Async Function DeleteEntryAsync(entry As VendorProductDto) As Task
            If entry Is Nothing Then Return
            IsBusy = True
            Try
                Await _vendorProductService.RemoveCatalogEntryAsync(entry.Id)
                _notifications.ShowSuccess($"Removed {entry.ProductName} from catalog.")
                Await LoadCatalogAsync()
            Catch ex As Exception
                _notifications.ShowError($"Remove failed: {ex.Message}")
            End Try
            IsBusy = False
        End Function

    End Class

End Namespace
