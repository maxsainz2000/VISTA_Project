Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Services

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
        Inherits ObservableObject

        Private ReadOnly _db As InventoryDbContext
        Private _allProducts As List(Of ProductManagementRowItem) = New List(Of ProductManagementRowItem)()
        Private _loadedProducts As List(Of Product)
        Private _loadedCategories As List(Of ProductCategory)

        Public Sub New(db As InventoryDbContext)
            _db = db

            Products = New ObservableCollection(Of ProductManagementRowItem)()
            Categories = New ObservableCollection(Of CategoryManagementItem)()
            CategoryFilters = New ObservableCollection(Of String) From {"All"}
            UnitOptions = New ObservableCollection(Of String) From {"bag", "bottle", "pack", "kg", "liter", "box", "piece", "set"}

            LoadDataCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            AddProductCommand = New RelayCommand(AddressOf OpenAddProductEditor)
            EditProductCommand = New RelayCommand(Of ProductManagementRowItem)(AddressOf OpenEditProductEditor)
            DeactivateProductCommand = New AsyncRelayCommand(Of ProductManagementRowItem)(AddressOf ToggleActiveAsync)
            SaveProductCommand = New AsyncRelayCommand(AddressOf SaveProductAsync)
            CancelEditorCommand = New RelayCommand(AddressOf CloseProductEditor)
            AddCategoryCommand = New RelayCommand(AddressOf OpenAddCategoryEditor)
            EditCategoryCommand = New RelayCommand(Of CategoryManagementItem)(AddressOf OpenEditCategoryEditor)
            DeleteCategoryCommand = New AsyncRelayCommand(Of CategoryManagementItem)(AddressOf DeleteCategoryAsync)
            SaveCategoryCommand = New AsyncRelayCommand(AddressOf SaveCategoryAsync)
            CancelCategoryEditorCommand = New RelayCommand(AddressOf CloseCategoryEditor)

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
                If SetProperty(_searchText, value) Then ApplyFilters()
            End Set
        End Property

        Private _selectedCategoryFilter As String = "All"
        Public Property SelectedCategoryFilter As String
            Get
                Return _selectedCategoryFilter
            End Get
            Set(value As String)
                If SetProperty(_selectedCategoryFilter, value) Then ApplyFilters()
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

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
            End Set
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
                SetProperty(_editorId, value)
            End Set
        End Property

        Private _editorName As String = String.Empty
        Public Property EditorName As String
            Get
                Return _editorName
            End Get
            Set(value As String)
                SetProperty(_editorName, value)
            End Set
        End Property

        Private _editorSku As String = String.Empty
        Public Property EditorSku As String
            Get
                Return _editorSku
            End Get
            Set(value As String)
                SetProperty(_editorSku, value)
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
        Public Property EditorRetailPriceText As String
            Get
                Return _editorRetailPriceText
            End Get
            Set(value As String)
                SetProperty(_editorRetailPriceText, value)
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
        Public Property EditorMinThresholdText As String
            Get
                Return _editorMinThresholdText
            End Get
            Set(value As String)
                SetProperty(_editorMinThresholdText, value)
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
            IsBusy = True
            Try
                _loadedProducts = New List(Of Product)()
                _loadedCategories = New List(Of ProductCategory)()
                Dim pmConnStr = _db.Database.GetConnectionString()
                Using pmConn As New SqliteConnection(pmConnStr)
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
        End Sub

        ' ─── Product Editor ────────────────────────────────────────────────────────

        Private Sub OpenAddProductEditor()
            EditorId = 0
            EditorTitle = "Add Product"
            EditorName = String.Empty
            EditorSku = String.Empty
            EditorCategoryId = If(Categories.Count > 0, Categories(0).CategoryId, 0)
            EditorRetailPriceText = "0.00"
            EditorUnit = "bag"
            EditorHasExpiry = False
            EditorMinThresholdText = "0"
            EditorDescription = String.Empty
            EditorError = String.Empty
            IsEditorOpen = True
        End Sub

        Private Sub OpenEditProductEditor(row As ProductManagementRowItem)
            If row Is Nothing Then Return

            EditorId = row.ProductId
            EditorTitle = "Edit Product"
            EditorName = row.Name
            EditorSku = row.Sku

            Dim cat = Categories.FirstOrDefault(Function(c) c.Name = row.CategoryName)
            EditorCategoryId = If(cat IsNot Nothing, cat.CategoryId, 0)

            EditorRetailPriceText = row.RetailPrice.ToString("F2")
            EditorUnit = If(String.IsNullOrEmpty(row.Unit), "bag", row.Unit)
            EditorHasExpiry = row.HasExpiry
            EditorMinThresholdText = row.MinThreshold.ToString()
            EditorDescription = row.Description
            EditorError = String.Empty
            IsEditorOpen = True
        End Sub

        Private Sub CloseProductEditor()
            IsEditorOpen = False
            EditorError = String.Empty
        End Sub

        Private Async Function SaveProductAsync() As Task
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
            Dim skuInUse = Await _db.Products.
                AnyAsync(Function(p) Not p.IsDeleted AndAlso
                                     p.Sku.ToUpper() = skuUpper AndAlso
                                     p.Id <> EditorId)

            If skuInUse Then
                EditorError = $"SKU '{EditorSku.Trim()}' is already used by another product."
                Return
            End If

            IsBusy = True
            Try
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
                        EditorError = "Product not found. Please refresh and try again."
                        Return
                    End If
                    existing.Name = EditorName.Trim()
                    existing.Sku = EditorSku.Trim()
                    existing.CategoryId = EditorCategoryId
                    existing.RetailPrice = price
                    existing.Unit = EditorUnit
                    existing.HasExpiry = EditorHasExpiry
                    existing.MinimumThreshold = threshold
                    existing.Description = If(String.IsNullOrWhiteSpace(EditorDescription), Nothing, EditorDescription.Trim())
                End If

                Await _db.SaveChangesAsync()
                CloseProductEditor()
                Await LoadDataAsync()

            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function ToggleActiveAsync(row As ProductManagementRowItem) As Task
            If row Is Nothing Then Return

            Dim product = Await _db.Products.FindAsync(row.ProductId)
            If product Is Nothing Then Return

            product.IsActive = Not product.IsActive
            Await _db.SaveChangesAsync()
            Await LoadDataAsync()
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

            Dim nameInUse = Await _db.ProductCategories.
                AnyAsync(Function(c) Not c.IsDeleted AndAlso
                                     c.Name.ToLower() = nameTrimmed.ToLower() AndAlso
                                     c.Id <> CategoryEditorId)

            If nameInUse Then
                CategoryEditorError = $"Category '{nameTrimmed}' already exists."
                Return
            End If

            IsBusy = True
            Try
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
                        CategoryEditorError = "Category not found. Please refresh and try again."
                        Return
                    End If
                    existing.Name = nameTrimmed
                    existing.Description = If(String.IsNullOrWhiteSpace(CategoryEditorDescription), Nothing, CategoryEditorDescription.Trim())
                End If

                Await _db.SaveChangesAsync()
                CloseCategoryEditor()
                Await LoadDataAsync()

            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function DeleteCategoryAsync(item As CategoryManagementItem) As Task
            If item Is Nothing Then Return
            If item.ProductCount > 0 Then Return

            Dim cat = Await _db.ProductCategories.FindAsync(item.CategoryId)
            If cat Is Nothing Then Return

            cat.IsDeleted = True
            Await _db.SaveChangesAsync()
            Await LoadDataAsync()
        End Function

    End Class

End Namespace
