Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Timers
Imports System.Windows.Input
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation

Namespace ViewModels

    ''' <summary>
    ''' Flat row item for the stock dashboard DataGrid.
    ''' Merges <see cref="ProductSummaryDto"/> and <see cref="StockoutEstimateDto"/> into one bindable object.
    ''' </summary>
    Public Class ProductRowItem
        Inherits ObservableObject

        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Category As String
        Public Property CurrentStock As Integer
        Public Property Unit As String
        Public Property RetailPrice As Decimal
        Public Property StockValue As Decimal
        ''' <summary>Weighted-average purchase cost across remaining non-expired batches.</summary>
        Public Property AverageUnitCost As Decimal
        ''' <summary>Unit cost of the oldest remaining batch — the cost the next sale draws from.</summary>
        Public Property FifoOldestUnitCost As Decimal
        ''' <summary>"Out" | "Low" | "Normal"</summary>
        Public Property StockStatus As String
        ''' <summary>"HasExpired" | "NearExpiry" | "OK"</summary>
        Public Property ExpiryStatus As String
        Public Property HasExpiry As Boolean
        ''' <summary>Decimal days remaining from <see cref="IStockoutEstimationService"/>. Nothing = dead stock or no data.</summary>
        Public Property DaysUntilStockout As Decimal?
        ''' <summary>"Critical" | "Warning" | "OK"</summary>
        Public Property RiskLevel As String

        ''' <summary>Formatted days-until-stockout for display. "—" when not applicable.</summary>
        Public ReadOnly Property DaysUntilStockoutDisplay As String
            Get
                If Not DaysUntilStockout.HasValue Then Return "—"
                Dim d = CInt(Math.Ceiling(DaysUntilStockout.Value))
                Return If(d <= 0, "0", d.ToString())
            End Get
        End Property

    End Class

    ''' <summary>
    ''' ViewModel for the Stock Dashboard — the main Inventory screen.
    ''' Aggregates real-time stock data, stockout estimates, and provides
    ''' category/status/text filtering, product-level drill-down, and 60-second auto-refresh.
    ''' </summary>
    Public Class StockDashboardViewModel
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

        Private ReadOnly _dashboardService As IStockDashboardService
        Private ReadOnly _stockoutService As IStockoutEstimationService
        Private ReadOnly _session As ISessionService
        Private ReadOnly _refreshTimer As System.Timers.Timer
        Private ReadOnly _uiContext As SynchronizationContext

        Private _allProducts As List(Of ProductRowItem) = New List(Of ProductRowItem)()

        ' Session memory fields
        Private Shared _savedCategory As String = "All"
        Private Shared _savedStatus As String = "All"
        Private Shared _savedSearchText As String = String.Empty
        Private Shared _lastUser As String = Nothing

        Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)
        Public Property ClearFiltersCommand As RelayCommand

        Public ReadOnly Property IsFilterActive As Boolean
            Get
                Return (SelectedCategory <> "All") OrElse (SelectedStatus <> "All") OrElse Not String.IsNullOrWhiteSpace(SearchText)
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
                Return "No Stock Products"
            End Get
        End Property

        Public ReadOnly Property EmptyStateDescription As String
            Get
                If IsFilterActive Then
                    Return "Try adjusting your filters or search term to find what you're looking for."
                End If
                Return "There are no products in the stock inventory."
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionCommand As ICommand
            Get
                If IsFilterActive Then
                    Return ClearFiltersCommand
                End If
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property EmptyStateActionText As String
            Get
                If IsFilterActive Then
                    Return "Clear filters"
                End If
                Return Nothing
            End Get
        End Property

        Public Sub New(dashboardService As IStockDashboardService,
                       stockoutService As IStockoutEstimationService,
                       session As ISessionService)

            _dashboardService = dashboardService
            _stockoutService = stockoutService
            _session = session

            ' Capture the UI SynchronizationContext so the timer callback can marshal
            ' ObservableCollection mutations back to the dispatcher thread.
            _uiContext = SynchronizationContext.Current

            ' Restore session filters, resetting if user changed
            Dim currentUser = _session.CurrentUsername
            If currentUser <> _lastUser Then
                _savedCategory = "All"
                _savedStatus = "All"
                _savedSearchText = String.Empty
                _lastUser = currentUser
            End If

            _selectedCategory = _savedCategory
            _selectedStatus = _savedStatus
            _searchText = _savedSearchText

            Products = New ObservableCollection(Of ProductRowItem)()
            ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
            Categories = New ObservableCollection(Of String) From {"All"}
            StatusOptions = New ObservableCollection(Of String) From {"All", "Out", "Low", "Normal"}

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            SelectProductCommand = New AsyncRelayCommand(Of ProductRowItem)(AddressOf LoadProductDetailAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)

            _refreshTimer = New System.Timers.Timer(60_000) With {.AutoReset = True}
            AddHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
            _refreshTimer.Start()

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Summary Cards ────────────────────────────────────────────────────────

        Private _totalProducts As Integer
        Public Property TotalProducts As Integer
            Get
                Return _totalProducts
            End Get
            Set(value As Integer)
                SetProperty(_totalProducts, value)
            End Set
        End Property

        Private _totalStockValue As Decimal
        Public Property TotalStockValue As Decimal
            Get
                Return _totalStockValue
            End Get
            Set(value As Decimal)
                SetProperty(_totalStockValue, value)
            End Set
        End Property

        Private _lowStockCount As Integer
        Public Property LowStockCount As Integer
            Get
                Return _lowStockCount
            End Get
            Set(value As Integer)
                SetProperty(_lowStockCount, value)
            End Set
        End Property

        Private _nearExpiryCount As Integer
        Public Property NearExpiryCount As Integer
            Get
                Return _nearExpiryCount
            End Get
            Set(value As Integer)
                SetProperty(_nearExpiryCount, value)
            End Set
        End Property

        Private _criticalStockoutCount As Integer
        Public Property CriticalStockoutCount As Integer
            Get
                Return _criticalStockoutCount
            End Get
            Set(value As Integer)
                SetProperty(_criticalStockoutCount, value)
            End Set
        End Property

        ' ─── Grid Data ────────────────────────────────────────────────────────────

        Public Property Products As ObservableCollection(Of ProductRowItem)

        ' ─── Filters ──────────────────────────────────────────────────────────────

        Public Property Categories As ObservableCollection(Of String)
        Public Property StatusOptions As ObservableCollection(Of String)

        Private _selectedCategory As String = "All"
        Public Property SelectedCategory As String
            Get
                Return _selectedCategory
            End Get
            Set(value As String)
                If SetProperty(_selectedCategory, value) Then
                    _savedCategory = value
                    ApplyFilters()
                End If
            End Set
        End Property

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

        ' ─── Product Detail ───────────────────────────────────────────────────────

        Private _selectedProduct As ProductRowItem
        Public Property SelectedProduct As ProductRowItem
            Get
                Return _selectedProduct
            End Get
            Set(value As ProductRowItem)
                SetProperty(_selectedProduct, value)
            End Set
        End Property

        Private _productDetail As ProductDetailDto
        Public Property ProductDetail As ProductDetailDto
            Get
                Return _productDetail
            End Get
            Set(value As ProductDetailDto)
                SetProperty(_productDetail, value)
                OnPropertyChanged(NameOf(IsDetailVisible))
            End Set
        End Property

        Public ReadOnly Property IsDetailVisible As Boolean
            Get
                Return ProductDetail IsNot Nothing
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
                Return Products.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Private _lastRefreshed As String = String.Empty
        Public Property LastRefreshed As String
            Get
                Return _lastRefreshed
            End Get
            Set(value As String)
                SetProperty(_lastRefreshed, value)
            End Set
        End Property

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property RefreshCommand As AsyncRelayCommand
        Public Property SelectProductCommand As AsyncRelayCommand(Of ProductRowItem)

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim dashboard As StockDashboardDto = Await _dashboardService.GetDashboardDataAsync()

                Dim stockoutMap As New Dictionary(Of Integer, StockoutEstimateDto)()
                Try
                    Dim stockouts As List(Of StockoutEstimateDto) = Await _stockoutService.EstimateAllAsync()
                    For Each s In stockouts
                        stockoutMap(s.ProductId) = s
                    Next
                Catch soEx As Exception
                    ' Stockout estimation is non-critical; products still display without it
                End Try

                TotalProducts = dashboard.TotalProducts
                TotalStockValue = dashboard.TotalStockValue
                LowStockCount = dashboard.LowStockCount
                NearExpiryCount = dashboard.NearExpiryCount

                _allProducts = dashboard.Products.
                    Select(Function(p)
                               Dim estimate As StockoutEstimateDto = Nothing
                               stockoutMap.TryGetValue(p.ProductId, estimate)
                               Return New ProductRowItem With {
                                   .ProductId = p.ProductId,
                                   .ProductName = p.ProductName,
                                   .Category = p.Category,
                                   .CurrentStock = p.CurrentStock,
                                   .Unit = p.Unit,
                                   .RetailPrice = p.RetailPrice,
                                   .StockValue = p.StockValue,
                                   .AverageUnitCost = p.AverageUnitCost,
                                   .FifoOldestUnitCost = p.FifoOldestUnitCost,
                                   .StockStatus = p.StockStatus,
                                   .ExpiryStatus = p.ExpiryStatus,
                                   .HasExpiry = p.HasExpiry,
                                   .DaysUntilStockout = If(estimate IsNot Nothing, estimate.EstimatedDaysUntilStockout, Nothing),
                                   .RiskLevel = If(estimate IsNot Nothing, estimate.RiskLevel, "OK")
                               }
                           End Function).
                    OrderBy(Function(p)
                                Select Case p.StockStatus
                                    Case "Out" : Return 0
                                    Case "Low" : Return 1
                                    Case Else : Return 2
                                End Select
                            End Function).
                    ThenBy(Function(p) p.ProductName).
                    ToList()

                CriticalStockoutCount = _allProducts.Where(Function(p) p.RiskLevel = "Critical").Count()

                Dim cats = _allProducts.Select(Function(p) p.Category).Distinct().OrderBy(Function(c) c).ToList()
                Categories.Clear()
                Categories.Add("All")
                For Each cat In cats
                    If Not String.IsNullOrEmpty(cat) Then Categories.Add(cat)
                Next

                ApplyFilters()
                LastRefreshed = $"Refreshed {DateTime.Now:HH:mm:ss}"
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

            If Not String.IsNullOrEmpty(SelectedCategory) AndAlso SelectedCategory <> "All" Then
                filtered = filtered.Where(Function(p) p.Category = SelectedCategory)
            End If

            If Not String.IsNullOrEmpty(SelectedStatus) AndAlso SelectedStatus <> "All" Then
                filtered = filtered.Where(Function(p) p.StockStatus = SelectedStatus)
            End If

            If Not String.IsNullOrWhiteSpace(SearchText) Then
                Dim term = SearchText.Trim().ToLowerInvariant()
                filtered = filtered.Where(Function(p) p.ProductName.ToLowerInvariant().Contains(term) OrElse
                                                       p.Category.ToLowerInvariant().Contains(term))
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

            If Not String.IsNullOrEmpty(SelectedCategory) AndAlso SelectedCategory <> "All" Then
                ActiveFilterChips.Add(New FilterChipItem($"Category: {SelectedCategory}", "Category", New RelayCommand(Sub() SelectedCategory = "All")))
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
            _selectedCategory = "All"
            _selectedStatus = "All"
            _searchText = String.Empty

            OnPropertyChanged(NameOf(SelectedCategory))
            OnPropertyChanged(NameOf(SelectedStatus))
            OnPropertyChanged(NameOf(SearchText))

            _savedCategory = "All"
            _savedStatus = "All"
            _savedSearchText = String.Empty

            ApplyFilters()
        End Sub

        Private Async Function LoadProductDetailAsync(row As ProductRowItem) As Task
            If row Is Nothing Then Return
            SelectedProduct = row
            IsBusy = True
            Try
                ProductDetail = Await _dashboardService.GetProductDetailAsync(row.ProductId)
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub OnRefreshTick(sender As Object, e As ElapsedEventArgs)
            If _uiContext IsNot Nothing Then
                _uiContext.Post(
                    Sub(o)
                        Dim t = LoadDataAsync()
                    End Sub, Nothing)
            End If
        End Sub

    End Class

End Namespace
