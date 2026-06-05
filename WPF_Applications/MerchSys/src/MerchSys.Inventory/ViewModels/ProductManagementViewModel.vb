Imports System.Collections.ObjectModel
Imports System.ComponentModel.DataAnnotations
Imports System.Windows.Input
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Enums

Namespace ViewModels

    Public Class ProductManagementRowItem
        Inherits ObservableObject

        Public Property ProductId As Integer
        Public Property Name As String
        Public Property Sku As String
        Public Property CategoryName As String
        Public Property RetailPrice As Decimal
        Public Property Unit As String
        Public Property HasExpiry As Boolean
        Public Property MinThreshold As Integer
        Public Property IsActive As Boolean
        Public Property Description As String

    End Class

    Public Class CategoryManagementItem
        Public Property CategoryId As Integer
        Public Property Name As String
        Public Property Description As String
        Public Property ProductCount As Integer
    End Class

    Public Class ProductManagementViewModel
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

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _session As ISessionService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _confirmationPresenter As IConfirmationPresenter
        Private ReadOnly _notifications As INotificationService
        Private _allProducts As List(Of ProductManagementRowItem) = New List(Of ProductManagementRowItem)()
        Private _loadedProducts As List(Of Product)
        Private _loadedCategories As List(Of ProductCategory)

        ' Session memory fields
        Private Shared _savedCategoryFilter As String = "All"
        Private Shared _savedSearchText As String = String.Empty
        Private Shared _lastUser As String = Nothing

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)
        Public Property ClearFiltersCommand As RelayCommand

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return (SelectedCategoryFilter <> "All") OrElse Not String.IsNullOrWhiteSpace(SearchText)
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
                Return "No Products Found"
            End Get
        End Property

        Public ReadOnly Property EmptyStateDescription As String
            Get
                If IsFilterActive Then
                    Return "Try adjusting your filters or search term to find what you're looking for."
                End If
                Return "No products match the selected criteria or search term."
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionCommand As ICommand
            Get
                If IsFilterActive Then
                    Return ClearFiltersCommand
                End If
                Return AddProductCommand
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionText As String
            Get
                If IsFilterActive Then
                    Return "Clear filters"
                End If
                Return "Add Product"
            End Get
        End Property

        Public Sub New(db As InventoryDbContext, session As ISessionService, conflictPresenter As IConflictPresenter, confirmationPresenter As IConfirmationPresenter, notifications As INotificationService)
            _db = db
            _session = session
            _conflictPresenter = conflictPresenter
            _confirmationPresenter = confirmationPresenter
            _notifications = notifications

            ' Restore session filters, resetting if user changed
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedCategoryFilter = "All"
                _savedSearchText = String.Empty
                _lastUser = currentUser
            End If

            _selectedCategoryFilter = _savedCategoryFilter
            _searchText = _savedSearchText

            Products = New ObservableCollection(Of ProductManagementRowItem)()
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Categories = New ObservableCollection(Of CategoryManagementItem)()
            CategoryFilters = New ObservableCollection(Of String) From {"All"}
            UnitOptions = New ObservableCollection(Of String) From {"bag", "bottle", "pack", "kg", "liter", "box", "piece", "set"}

            LoadDataCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)

            If IsManager Then
                AddProductCommand = New RelayCommand(AddressOf OpenAddProductEditor)
                EditProductCommand = New RelayCommand(Of ProductManagementRowItem)(AddressOf OpenEditProductEditor)
                DeactivateProductCommand = New AsyncRelayCommand(Of ProductManagementRowItem)(AddressOf ToggleActiveAsync)
                SaveProductCommand = New AsyncRelayCommand(AddressOf SaveProductAsync, Function() Not HasErrors)
                CancelEditorCommand = New RelayCommand(AddressOf CloseProductEditor)
                AddCategoryCommand = New RelayCommand(AddressOf OpenAddCategoryEditor)
                EditCategoryCommand = New RelayCommand(Of CategoryManagementItem)(AddressOf OpenEditCategoryEditor)
                DeleteCategoryCommand = New AsyncRelayCommand(Of CategoryManagementItem)(AddressOf DeleteCategoryAsync)
                SaveCategoryCommand = New AsyncRelayCommand(AddressOf SaveCategoryAsync)
                CancelCategoryEditorCommand = New RelayCommand(AddressOf CloseCategoryEditor)
            End If

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Product List ──────────────────────────────────────────────────────────

        Public Property Products As ObservableCollection(Of ProductManagementRowItem)
        Public Property Categories As ObservableCollection(Of CategoryManagementItem)
        Public Property CategoryFilters As ObservableCollection(Of String)
        Public Property UnitOptions As ObservableCollection(Of String)

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

        Private _selectedCategoryFilter As String = "All"
        Public Property SelectedCategoryFilter As String
            Get
                Return _selectedCategoryFilter
            End Get
            Set(value As String)
                If SetProperty(_selectedCategoryFilter, value) Then
                    _savedCategoryFilter = value
                    ApplyFilters()
                End If
            End Set
        End Property

        Private _selectedProduct As ProductManagementRowItem
        Public Property SelectedProduct As ProductManagementRowItem
            Get
                Return _selectedProduct
            End Get
            Set(value As ProductManagementRowItem)
                SetProperty(_selectedProduct, value)
                OnPropertyChanged(NameOf(HasSelectedProduct))
                OnPropertyChanged(NameOf(SelectedProductIsActive))
            End Set
        End Property

        Public ReadOnly Property HasSelectedProduct As Boolean
            Get
                Return SelectedProduct IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property SelectedProductIsActive As Boolean
            Get
                Return SelectedProduct IsNot Nothing AndAlso SelectedProduct.IsActive
            End Get
        End Property

        Public ReadOnly Property TotalProducts As Integer
            Get
                Return _allProducts.Count
            End Get
        End Property

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
                Return Products.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Public ReadOnly Property IsManager As Boolean
            Get
                Return _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer
            End Get
        End Property

        ' ─── Product Editor State ─────────────────────────────────────────────────

        Private _isEditorOpen As Boolean
        Public Property IsEditorOpen As Boolean
            Get
                Return _isEditorOpen
            End Get
            Set(value As Boolean)
                SetProperty(_isEditorOpen, value)
            End Set
        End Property

        Private _editorTitle As String = "Add Product"
        Public Property EditorTitle As String
            Get
                Return _editorTitle
            End Get
            Set(value As String)
                SetProperty(_editorTitle, value)
            End Set
        End Property

        Private _editorId As Integer
        Public Property EditorId As Integer
            Get
                Return _editorId
            End Get
            Set(value As Integer)
                If SetProperty(_editorId, value) Then
                    OnPropertyChanged(NameOf(ShowCostingHelper))
                End If
            End Set
        End Property

        Private _editorName As String = String.Empty
        <Required(ErrorMessage:="Product name is required.")>
        Public Property EditorName As String
            Get
                Return _editorName
            End Get
            Set(value As String)
                If SetProperty(_editorName, value, True) Then
                    If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _editorSku As String = String.Empty
        <Required(ErrorMessage:="SKU is required.")>
        Public Property EditorSku As String
            Get
                Return _editorSku
            End Get
            Set(value As String)
                If SetProperty(_editorSku, value, True) Then
                    If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _editorCategoryId As Integer
        Public Property EditorCategoryId As Integer
            Get
                Return _editorCategoryId
            End Get
            Set(value As Integer)
                SetProperty(_editorCategoryId, value)
            End Set
        End Property

        Private _editorRetailPriceText As String = "0.00"
        <Required(ErrorMessage:="Retail price is required.")>
        <RegularExpression("^\d+(\.\d{1,2})?$", ErrorMessage:="Retail price must be a positive number with up to 2 decimal places.")>
        Public Property EditorRetailPriceText As String
            Get
                Return _editorRetailPriceText
            End Get
            Set(value As String)
                If SetProperty(_editorRetailPriceText, value, True) Then
                    OnPropertyChanged(NameOf(EditorMargin))
                    OnPropertyChanged(NameOf(EditorMarginPercent))
                    If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _editorUnit As String = "bag"
        Public Property EditorUnit As String
            Get
                Return _editorUnit
            End Get
            Set(value As String)
                SetProperty(_editorUnit, value)
            End Set
        End Property

        Private _editorHasExpiry As Boolean
        Public Property EditorHasExpiry As Boolean
            Get
                Return _editorHasExpiry
            End Get
            Set(value As Boolean)
                SetProperty(_editorHasExpiry, value)
            End Set
        End Property

        Private _editorMinThresholdText As String = "0"
        <Required(ErrorMessage:="Minimum threshold is required.")>
        <RegularExpression("^\d+$", ErrorMessage:="Minimum threshold must be a non-negative integer.")>
        Public Property EditorMinThresholdText As String
            Get
                Return _editorMinThresholdText
            End Get
            Set(value As String)
                If SetProperty(_editorMinThresholdText, value, True) Then
                    If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _editorDescription As String = String.Empty
        Public Property EditorDescription As String
            Get
                Return _editorDescription
            End Get
            Set(value As String)
                SetProperty(_editorDescription, value)
            End Set
        End Property

        Private _editorPriceChangeReason As String = String.Empty
        Public Property EditorPriceChangeReason As String
            Get
                Return _editorPriceChangeReason
            End Get
            Set(value As String)
                SetProperty(_editorPriceChangeReason, value)
            End Set
        End Property

        Private _editorFifoCost As Decimal = 0D
        Public Property EditorFifoCost As Decimal
            Get
                Return _editorFifoCost
            End Get
            Set(value As Decimal)
                If SetProperty(_editorFifoCost, value) Then
                    OnPropertyChanged(NameOf(EditorSuggestedPrice))
                    OnPropertyChanged(NameOf(EditorMargin))
                    OnPropertyChanged(NameOf(EditorMarginPercent))
                    OnPropertyChanged(NameOf(ShowCostingHelper))
                End If
            End Set
        End Property

        Public ReadOnly Property EditorSuggestedPrice As Decimal
            Get
                Return Math.Round(EditorFifoCost * 1.20D, 2)
            End Get
        End Property

        Public ReadOnly Property EditorMargin As Decimal
            Get
                Dim price As Decimal
                If Decimal.TryParse(EditorRetailPriceText, price) Then
                    Return price - EditorFifoCost
                End If
                Return 0D
            End Get
        End Property

        Public ReadOnly Property EditorMarginPercent As Double
            Get
                Dim price As Decimal
                If Decimal.TryParse(EditorRetailPriceText, price) AndAlso EditorFifoCost > 0 Then
                    Return CDbl(Math.Round(((price - EditorFifoCost) / EditorFifoCost) * 100D, 1))
                End If
                Return 0.0
            End Get
        End Property

        Public ReadOnly Property ShowCostingHelper As Boolean
            Get
                Return EditorId <> 0 AndAlso EditorFifoCost > 0
            End Get
        End Property

        Private _editorError As String = String.Empty
        Public Property EditorError As String
            Get
                Return _editorError
            End Get
            Set(value As String)
                SetProperty(_editorError, value)
                OnPropertyChanged(NameOf(HasEditorError))
            End Set
        End Property

        Public ReadOnly Property HasEditorError As Boolean
            Get
                Return Not String.IsNullOrEmpty(EditorError)
            End Get
        End Property

        ' ─── Category Editor State ─────────────────────────────────────────────────

        Private _isCategoryEditorOpen As Boolean
        Public Property IsCategoryEditorOpen As Boolean
            Get
                Return _isCategoryEditorOpen
            End Get
            Set(value As Boolean)
                SetProperty(_isCategoryEditorOpen, value)
            End Set
        End Property

        Private _categoryEditorTitle As String = "Add Category"
        Public Property CategoryEditorTitle As String
            Get
                Return _categoryEditorTitle
            End Get
            Set(value As String)
                SetProperty(_categoryEditorTitle, value)
            End Set
        End Property

        Private _categoryEditorId As Integer
        Public Property CategoryEditorId As Integer
            Get
                Return _categoryEditorId
            End Get
            Set(value As Integer)
                SetProperty(_categoryEditorId, value)
            End Set
        End Property

        Private _categoryEditorName As String = String.Empty
        Public Property CategoryEditorName As String
            Get
                Return _categoryEditorName
            End Get
            Set(value As String)
                SetProperty(_categoryEditorName, value)
            End Set
        End Property

        Private _categoryEditorDescription As String = String.Empty
        Public Property CategoryEditorDescription As String
            Get
                Return _categoryEditorDescription
            End Get
            Set(value As String)
                SetProperty(_categoryEditorDescription, value)
            End Set
        End Property

        Private _categoryEditorError As String = String.Empty
        Public Property CategoryEditorError As String
            Get
                Return _categoryEditorError
            End Get
            Set(value As String)
                SetProperty(_categoryEditorError, value)
                OnPropertyChanged(NameOf(HasCategoryEditorError))
            End Set
        End Property

        Public ReadOnly Property HasCategoryEditorError As Boolean
            Get
                Return Not String.IsNullOrEmpty(CategoryEditorError)
            End Get
        End Property

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property LoadDataCommand As AsyncRelayCommand
        Public Property AddProductCommand As RelayCommand
        Public Property EditProductCommand As RelayCommand(Of ProductManagementRowItem)
        Public Property DeactivateProductCommand As AsyncRelayCommand(Of ProductManagementRowItem)
        Public Property SaveProductCommand As AsyncRelayCommand
        Public Property CancelEditorCommand As RelayCommand
        Public Property AddCategoryCommand As RelayCommand
        Public Property EditCategoryCommand As RelayCommand(Of CategoryManagementItem)
        Public Property DeleteCategoryCommand As AsyncRelayCommand(Of CategoryManagementItem)
        Public Property SaveCategoryCommand As AsyncRelayCommand
        Public Property CancelCategoryEditorCommand As RelayCommand

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                _loadedProducts = New List(Of Product)()
                _loadedCategories = New List(Of ProductCategory)()
                Dim pmConnStr = _db.Database.GetConnectionString()
                Using pmConn As New MySqlConnection(pmConnStr)
                    Await pmConn.OpenAsync()
                    Using pmCmd = pmConn.CreateCommand()
                        pmCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                             "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                             "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                             "FROM Inv_Products WHERE IsDeleted = 0 ORDER BY Name"
                        Using pmReader = pmCmd.ExecuteReader()
                            While pmReader.Read()
                                _loadedProducts.Add(StockService.ReadProduct(pmReader))
                            End While
                        End Using
                    End Using
                    Using cCmd = pmConn.CreateCommand()
                        cCmd.CommandText = "SELECT Id, Name, Description, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           "FROM Inv_ProductCategories WHERE IsDeleted = 0 ORDER BY Name"
                        Using cReader = cCmd.ExecuteReader()
                            While cReader.Read()
                                _loadedCategories.Add(New ProductCategory With {
                                    .Id = cReader.GetInt32(0),
                                    .Name = cReader.GetString(1),
                                    .Description = If(cReader.IsDBNull(2), Nothing, cReader.GetString(2)),
                                    .IsDeleted = cReader.GetBoolean(3),
                                    .DeletedBy = If(cReader.IsDBNull(4), Nothing, cReader.GetString(4)),
                                    .DeletedAt = If(cReader.IsDBNull(5), Nothing, CType(cReader.GetDateTime(5), DateTime?)),
                                    .CreatedBy = cReader.GetString(6),
                                    .CreatedAt = cReader.GetDateTime(7),
                                    .ModifiedBy = If(cReader.IsDBNull(8), Nothing, cReader.GetString(8)),
                                    .ModifiedAt = If(cReader.IsDBNull(9), Nothing, CType(cReader.GetDateTime(9), DateTime?))
                                })
                            End While
                        End Using
                    End Using
                End Using
                Dim categoryLookup = _loadedCategories.ToDictionary(Function(c) c.Id)
                For Each p In _loadedProducts
                    Dim cat As ProductCategory = Nothing
                    If categoryLookup.TryGetValue(p.CategoryId, cat) Then p.Category = cat
                Next
                Dim dbProducts As List(Of Product) = _loadedProducts
                Dim dbCategories As List(Of ProductCategory) = _loadedCategories

                _allProducts = dbProducts.
                    Select(Function(p) New ProductManagementRowItem With {
                        .ProductId = p.Id,
                        .Name = p.Name,
                        .Sku = p.Sku,
                        .CategoryName = If(p.Category IsNot Nothing, p.Category.Name, "—"),
                        .RetailPrice = p.RetailPrice,
                        .Unit = p.Unit,
                        .HasExpiry = p.HasExpiry,
                        .MinThreshold = p.MinimumThreshold,
                        .IsActive = p.IsActive,
                        .Description = If(p.Description, String.Empty)
                    }).
                    ToList()

                Dim countMap = dbProducts.
                    GroupBy(Function(p) p.CategoryId).
                    ToDictionary(Function(g) g.Key, Function(g) g.Count())

                Categories.Clear()
                For Each cat In dbCategories
                    Dim cnt As Integer = 0
                    countMap.TryGetValue(cat.Id, cnt)
                    Categories.Add(New CategoryManagementItem With {
                        .CategoryId = cat.Id,
                        .Name = cat.Name,
                        .Description = If(cat.Description, String.Empty),
                        .ProductCount = cnt
                    })
                Next

                CategoryFilters.Clear()
                CategoryFilters.Add("All")
                For Each cat In dbCategories
                    CategoryFilters.Add(cat.Name)
                Next

                ApplyFilters()
                OnPropertyChanged(NameOf(TotalProducts))
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
            Dim filtered = _allProducts.AsEnumerable()

            If Not String.IsNullOrEmpty(SelectedCategoryFilter) AndAlso SelectedCategoryFilter <> "All" Then
                filtered = filtered.Where(Function(p) p.CategoryName = SelectedCategoryFilter)
            End If

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                filtered = filtered.Where(Function(p) p.Name.ToLowerInvariant().Contains(term) OrElse
                                                       p.Sku.ToLowerInvariant().Contains(term))
            End If

            Products.Clear()
            For Each item In filtered
                Products.Add(item)
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

            If Not String.IsNullOrEmpty(SelectedCategoryFilter) AndAlso SelectedCategoryFilter <> "All" Then
                ActiveFilterChips.Add(New FilterChipItem($"Category: {SelectedCategoryFilter}", "Category", New RelayCommand(Sub() SelectedCategoryFilter = "All")))
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
            _selectedCategoryFilter = "All"
            _searchText = String.Empty

            OnPropertyChanged(NameOf(SelectedCategoryFilter))
            OnPropertyChanged(NameOf(SearchText))

            _savedCategoryFilter = "All"
            _savedSearchText = String.Empty

            ApplyFilters()
        End Sub

        ' ─── Product Editor ────────────────────────────────────────────────────────

        Private Sub ClearProductEditorErrors()
            ClearErrors(NameOf(EditorName))
            ClearErrors(NameOf(EditorSku))
            ClearErrors(NameOf(EditorRetailPriceText))
            ClearErrors(NameOf(EditorMinThresholdText))
        End Sub

        Private Sub OpenAddProductEditor()
            EditorId = 0
            EditorTitle = "Add Product"
            _editorName = String.Empty
            _editorSku = String.Empty
            EditorCategoryId = If(Categories.Count > 0, Categories(0).CategoryId, 0)
            _editorRetailPriceText = "0.00"
            EditorUnit = "bag"
            EditorHasExpiry = False
            _editorMinThresholdText = "0"
            EditorDescription = String.Empty
            EditorPriceChangeReason = String.Empty
            EditorError = String.Empty
            EditorFifoCost = 0D
            ClearProductEditorErrors()
            OnPropertyChanged(NameOf(EditorName))
            OnPropertyChanged(NameOf(EditorSku))
            OnPropertyChanged(NameOf(EditorRetailPriceText))
            OnPropertyChanged(NameOf(EditorMinThresholdText))
            IsEditorOpen = True
            If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()
        End Sub

        Private Async Sub OpenEditProductEditor(row As ProductManagementRowItem)
            If row Is Nothing Then Return

            EditorId = row.ProductId
            EditorTitle = "Edit Product"
            _editorName = row.Name
            _editorSku = row.Sku

            Dim cat = Categories.FirstOrDefault(Function(c) c.Name = row.CategoryName)
            EditorCategoryId = If(cat IsNot Nothing, cat.CategoryId, 0)

            _editorRetailPriceText = row.RetailPrice.ToString("F2")
            EditorUnit = If(String.IsNullOrEmpty(row.Unit), "bag", row.Unit)
            EditorHasExpiry = row.HasExpiry
            _editorMinThresholdText = row.MinThreshold.ToString()
            EditorDescription = row.Description
            EditorError = String.Empty
            ClearProductEditorErrors()

            OnPropertyChanged(NameOf(EditorName))
            OnPropertyChanged(NameOf(EditorSku))
            OnPropertyChanged(NameOf(EditorRetailPriceText))
            OnPropertyChanged(NameOf(EditorMinThresholdText))
            If SaveProductCommand IsNot Nothing Then SaveProductCommand.NotifyCanExecuteChanged()

            ' Load current FIFO cost (Option A)
            IsBusy = True
            Try
                Dim now = DateTime.UtcNow
                Dim oldestBatch = Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = row.ProductId AndAlso
                                       b.QuantityRemaining > 0 AndAlso
                                       (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                    .OrderBy(Function(b) b.ReceiptDate) _
                    .FirstOrDefaultAsync()
                EditorFifoCost = If(oldestBatch IsNot Nothing, oldestBatch.UnitCost, 0D)
            Catch ex As Exception
                EditorFifoCost = 0D
            Finally
                IsBusy = False
            End Try

            IsEditorOpen = True
        End Sub

        Private Sub CloseProductEditor()
            IsEditorOpen = False
            EditorError = String.Empty
            ClearProductEditorErrors()
        End Sub

        Private Async Function SaveProductAsync() As Task
            ValidateAllProperties()
            If HasErrors Then Return

            If String.IsNullOrWhiteSpace(EditorName) Then
                EditorError = "Product name is required."
                Return
            End If

            If String.IsNullOrWhiteSpace(EditorSku) Then
                EditorError = "SKU is required."
                Return
            End If

            Dim price As Decimal
            If Not Decimal.TryParse(EditorRetailPriceText, price) OrElse price <= 0 Then
                EditorError = "Retail price must be greater than 0."
                Return
            End If

            ' Enforce negative profitability guard (reject if price < FIFO cost)
            Dim fifoCost As Decimal = 0D
            If EditorId <> 0 Then
                Dim now = DateTime.UtcNow
                Dim oldestBatch = Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = EditorId AndAlso
                                       b.QuantityRemaining > 0 AndAlso
                                       (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                    .OrderBy(Function(b) b.ReceiptDate) _
                    .FirstOrDefaultAsync()
                If oldestBatch IsNot Nothing Then
                    fifoCost = oldestBatch.UnitCost
                End If
            End If

            If fifoCost > 0D AndAlso price < fifoCost Then
                EditorError = $"Retail price (₱{price:F2}) cannot be less than the current vendor cost (₱{fifoCost:F2}), resulting in negative profitability."
                Return
            End If

            Dim threshold As Integer
            If Not Integer.TryParse(EditorMinThresholdText, threshold) OrElse threshold < 0 Then
                EditorError = "Minimum threshold must be 0 or greater."
                Return
            End If

            If EditorCategoryId = 0 Then
                EditorError = "Please select a category."
                Return
            End If

            Dim skuUpper = EditorSku.Trim().ToUpperInvariant()
            Dim skuInUseActive = Await _db.Products.
                IgnoreQueryFilters().
                AnyAsync(Function(p) Not p.IsDeleted AndAlso
                                     p.Sku.ToUpper() = skuUpper AndAlso
                                     p.Id <> EditorId)

            Dim skuInUseDeleted = Await _db.Products.
                IgnoreQueryFilters().
                AnyAsync(Function(p) p.IsDeleted AndAlso
                                     p.Sku.ToUpper() = skuUpper AndAlso
                                     p.Id <> EditorId)

            If skuInUseActive Then
                EditorError = $"SKU '{EditorSku.Trim()}' is already used by another product."
                Return
            End If

            If skuInUseDeleted Then
                EditorError = $"SKU '{EditorSku.Trim()}' is already used by a deleted product. Please contact your administrator or choose a different SKU."
                Return
            End If

            IsBusy = True
            Dim productNotFound As Boolean = False
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        If EditorId = 0 Then
                            Dim newProduct As New Product With {
                                .Name = EditorName.Trim(),
                                .Sku = EditorSku.Trim(),
                                .CategoryId = EditorCategoryId,
                                .RetailPrice = price,
                                .Unit = EditorUnit,
                                .HasExpiry = EditorHasExpiry,
                                .MinimumThreshold = threshold,
                                .Description = If(String.IsNullOrWhiteSpace(EditorDescription), Nothing, EditorDescription.Trim()),
                                .IsActive = True,
                                .IsDeleted = False
                            }
                            _db.Products.Add(newProduct)
                        Else
                            Dim existing = Await _db.Products.FindAsync(EditorId)
                            If existing Is Nothing Then
                                productNotFound = True
                                Return
                            End If
                            existing.Name = EditorName.Trim()
                            existing.Sku = EditorSku.Trim()
                            existing.CategoryId = EditorCategoryId

                            Dim oldPrice As Decimal = existing.RetailPrice
                            If oldPrice <> price Then
                                Dim historyRow As New ProductPriceHistory With {
                                    .ProductId = existing.Id,
                                    .OldPrice = oldPrice,
                                    .NewPrice = price,
                                    .ChangedAt = DateTime.UtcNow,
                                    .ChangedBy = If(_session IsNot Nothing AndAlso Not String.IsNullOrEmpty(_session.CurrentUsername), _session.CurrentUsername, Environment.UserName),
                                    .Reason = If(String.IsNullOrWhiteSpace(EditorPriceChangeReason), Nothing, EditorPriceChangeReason.Trim())
                                }
                                _db.ProductPriceHistory.Add(historyRow)
                            End If

                            existing.RetailPrice = price
                            existing.Unit = EditorUnit
                            existing.HasExpiry = EditorHasExpiry
                            existing.MinimumThreshold = threshold
                            existing.Description = If(String.IsNullOrWhiteSpace(EditorDescription), Nothing, EditorDescription.Trim())
                        End If
                        Await _db.SaveChangesAsync()
                    End Function,
                    Async Function()
                        CloseProductEditor()
                        Await LoadDataAsync()
                    End Function,
                    _conflictPresenter)

                If productNotFound Then
                    EditorError = "Product not found. Please refresh and try again."
                    Return
                End If

                If saved Then
                    CloseProductEditor()
                    Await LoadDataAsync()
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function ToggleActiveAsync(row As ProductManagementRowItem) As Task
            If row Is Nothing Then Return

            Dim product = Await _db.Products.FindAsync(row.ProductId)
            If product Is Nothing Then Return

            IsBusy = True
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        product.IsActive = Not product.IsActive
                        Await _db.SaveChangesAsync()
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Await LoadDataAsync()
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Category Editor ──────────────────────────────────────────────────────

        Private Sub OpenAddCategoryEditor()
            CategoryEditorId = 0
            CategoryEditorTitle = "Add Category"
            CategoryEditorName = String.Empty
            CategoryEditorDescription = String.Empty
            CategoryEditorError = String.Empty
            IsCategoryEditorOpen = True
        End Sub

        Private Sub OpenEditCategoryEditor(item As CategoryManagementItem)
            If item Is Nothing Then Return
            CategoryEditorId = item.CategoryId
            CategoryEditorTitle = "Edit Category"
            CategoryEditorName = item.Name
            CategoryEditorDescription = item.Description
            CategoryEditorError = String.Empty
            IsCategoryEditorOpen = True
        End Sub

        Private Sub CloseCategoryEditor()
            IsCategoryEditorOpen = False
            CategoryEditorError = String.Empty
        End Sub

        Private Async Function SaveCategoryAsync() As Task
            If String.IsNullOrWhiteSpace(CategoryEditorName) Then
                CategoryEditorError = "Category name is required."
                Return
            End If

            Dim nameTrimmed = CategoryEditorName.Trim()

            Dim nameInUseActive = Await _db.ProductCategories.
                IgnoreQueryFilters().
                AnyAsync(Function(c) Not c.IsDeleted AndAlso
                                     c.Name.ToLower() = nameTrimmed.ToLower() AndAlso
                                     c.Id <> CategoryEditorId)

            Dim nameInUseDeleted = Await _db.ProductCategories.
                IgnoreQueryFilters().
                AnyAsync(Function(c) c.IsDeleted AndAlso
                                     c.Name.ToLower() = nameTrimmed.ToLower() AndAlso
                                     c.Id <> CategoryEditorId)

            If nameInUseActive Then
                CategoryEditorError = $"Category '{nameTrimmed}' already exists."
                Return
            End If

            If nameInUseDeleted Then
                CategoryEditorError = $"Category '{nameTrimmed}' already exists in a deleted state. Please choose a different name."
                Return
            End If

            IsBusy = True
            Dim catNotFound As Boolean = False
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        If CategoryEditorId = 0 Then
                            Dim newCat As New ProductCategory With {
                                .Name = nameTrimmed,
                                .Description = If(String.IsNullOrWhiteSpace(CategoryEditorDescription), Nothing, CategoryEditorDescription.Trim()),
                                .IsDeleted = False
                            }
                            _db.ProductCategories.Add(newCat)
                        Else
                            Dim existing = Await _db.ProductCategories.FindAsync(CategoryEditorId)
                            If existing Is Nothing Then
                                catNotFound = True
                                Return
                            End If
                            existing.Name = nameTrimmed
                            existing.Description = If(String.IsNullOrWhiteSpace(CategoryEditorDescription), Nothing, CategoryEditorDescription.Trim())
                        End If
                        Await _db.SaveChangesAsync()
                    End Function,
                    Async Function()
                        CloseCategoryEditor()
                        Await LoadDataAsync()
                    End Function,
                    _conflictPresenter)

                If catNotFound Then
                    CategoryEditorError = "Category not found. Please refresh and try again."
                    Return
                End If

                If saved Then
                    CloseCategoryEditor()
                    Await LoadDataAsync()
                End If
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function DeleteCategoryAsync(item As CategoryManagementItem) As Task
            If item Is Nothing Then Return
            If item.ProductCount > 0 Then Return

            Dim req As New ConfirmationRequest("Delete Category", $"This will permanently delete the category '{item.Name}'.", "_Delete", True)
            If Not Await _confirmationPresenter.PromptAsync(req) Then Return

            Dim cat = Await _db.ProductCategories.FindAsync(item.CategoryId)
            If cat Is Nothing Then Return

            Dim categoryId = item.CategoryId
            Dim categoryName = item.Name

            IsBusy = True
            Try
                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        cat.IsDeleted = True
                        Await _db.SaveChangesAsync()
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    Await LoadDataAsync()

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
                                               Dim restoreSaved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                                                   Async Function()
                                                       Dim c = Await _db.ProductCategories.IgnoreQueryFilters().FirstOrDefaultAsync(Function(x) x.Id = categoryId)
                                                       If c IsNot Nothing Then
                                                           c.IsDeleted = False
                                                           c.DeletedBy = Nothing
                                                           c.DeletedAt = Nothing
                                                           Await _db.SaveChangesAsync()
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
                                               _notifications.ShowError($"Restore failed: {errMsg}")
                                           ElseIf success Then
                                               Await LoadDataAsync()
                                               _notifications.ShowSuccess($"Category '{categoryName}' restored.")
                                           Else
                                               _notifications.ShowError("Could not undo.")
                                           End If
                                       End Sub

                    Dim undoAction = New NotificationAction("Undo", undoCallback)
                    _notifications.ShowSuccess($"Category '{categoryName}' deleted.", undoAction)
                End If
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
