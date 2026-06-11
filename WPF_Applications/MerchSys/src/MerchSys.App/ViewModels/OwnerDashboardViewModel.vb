Imports System.Collections.Generic
Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services
Imports MerchSys.Inventory.Services
Imports MerchSys.POS.Services
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation
Imports Microsoft.Extensions.Configuration

Namespace ViewModels

    Public Class OwnerDashboardViewModel
        Inherits ObservableObject
        Implements IDisposable, IFreshnessAware

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
        Private ReadOnly _stockDashboard As IStockDashboardService
        Private ReadOnly _lowStockAlert As ILowStockAlertService
        Private ReadOnly _expiryTracking As IExpiryTrackingService
        Private ReadOnly _purchaseOrder As IPurchaseOrderService
        Private ReadOnly _vendor As IVendorService
        Private ReadOnly _accountsPayable As IAccountsPayableService
        Private ReadOnly _dailySummary As IDailySummaryService
        Private ReadOnly _financialOverview As IFinancialOverviewService
        Private ReadOnly _incomeStatement As IIncomeStatementService
        Private ReadOnly _refreshTimer As DispatcherTimer
        Private _disposed As Boolean

        ' ── Purchasing KPIs ──────────────────────────────────────────────────────

        Private _activeVendorCount As Integer
        Public Property ActiveVendorCount As Integer
            Get
                Return _activeVendorCount
            End Get
            Private Set(value As Integer)
                SetProperty(_activeVendorCount, value)
            End Set
        End Property

        Private _openPurchaseOrderCount As Integer
        Public Property OpenPurchaseOrderCount As Integer
            Get
                Return _openPurchaseOrderCount
            End Get
            Private Set(value As Integer)
                SetProperty(_openPurchaseOrderCount, value)
            End Set
        End Property

        Private _pendingDeliveryCount As Integer
        Public Property PendingDeliveryCount As Integer
            Get
                Return _pendingDeliveryCount
            End Get
            Private Set(value As Integer)
                SetProperty(_pendingDeliveryCount, value)
            End Set
        End Property

        Private _overdueApTotal As Decimal
        Public Property OverdueApTotal As Decimal
            Get
                Return _overdueApTotal
            End Get
            Private Set(value As Decimal)
                SetProperty(_overdueApTotal, value)
            End Set
        End Property

        Private _purchasingInterpretation As String = String.Empty
        Public Property PurchasingInterpretation As String
            Get
                Return _purchasingInterpretation
            End Get
            Private Set(value As String)
                SetProperty(_purchasingInterpretation, value)
            End Set
        End Property

        ' ── Inventory KPIs ───────────────────────────────────────────────────────

        Private _totalSkuCount As Integer
        Public Property TotalSkuCount As Integer
            Get
                Return _totalSkuCount
            End Get
            Private Set(value As Integer)
                SetProperty(_totalSkuCount, value)
            End Set
        End Property

        Private _totalStockValue As Decimal
        Public Property TotalStockValue As Decimal
            Get
                Return _totalStockValue
            End Get
            Private Set(value As Decimal)
                SetProperty(_totalStockValue, value)
            End Set
        End Property

        Private _lowStockItemCount As Integer
        Public Property LowStockItemCount As Integer
            Get
                Return _lowStockItemCount
            End Get
            Private Set(value As Integer)
                SetProperty(_lowStockItemCount, value)
            End Set
        End Property

        Private _expiringSoonCount As Integer
        Public Property ExpiringSoonCount As Integer
            Get
                Return _expiringSoonCount
            End Get
            Private Set(value As Integer)
                SetProperty(_expiringSoonCount, value)
            End Set
        End Property

        Private _inventoryInterpretation As String = String.Empty
        Public Property InventoryInterpretation As String
            Get
                Return _inventoryInterpretation
            End Get
            Private Set(value As String)
                SetProperty(_inventoryInterpretation, value)
            End Set
        End Property

        ' ── Sales KPIs ───────────────────────────────────────────────────────────

        Private _todayRevenue As Decimal
        Public Property TodayRevenue As Decimal
            Get
                Return _todayRevenue
            End Get
            Private Set(value As Decimal)
                SetProperty(_todayRevenue, value)
            End Set
        End Property

        Private _weekRevenue As Decimal
        Public Property WeekRevenue As Decimal
            Get
                Return _weekRevenue
            End Get
            Private Set(value As Decimal)
                SetProperty(_weekRevenue, value)
            End Set
        End Property

        Private _todayRevenueDelta As Double
        Public Property TodayRevenueDelta As Double
            Get
                Return _todayRevenueDelta
            End Get
            Private Set(value As Double)
                SetProperty(_todayRevenueDelta, value)
            End Set
        End Property

        Private _weekRevenueDelta As Double
        Public Property WeekRevenueDelta As Double
            Get
                Return _weekRevenueDelta
            End Get
            Private Set(value As Double)
                SetProperty(_weekRevenueDelta, value)
            End Set
        End Property

        Private _todayTransactionCount As Integer
        Public Property TodayTransactionCount As Integer
            Get
                Return _todayTransactionCount
            End Get
            Private Set(value As Integer)
                SetProperty(_todayTransactionCount, value)
            End Set
        End Property

        Private _topSellingProduct As String = "—"
        Public Property TopSellingProduct As String
            Get
                Return _topSellingProduct
            End Get
            Private Set(value As String)
                SetProperty(_topSellingProduct, value)
            End Set
        End Property

        Private _salesInterpretation As String = String.Empty
        Public Property SalesInterpretation As String
            Get
                Return _salesInterpretation
            End Get
            Private Set(value As String)
                SetProperty(_salesInterpretation, value)
            End Set
        End Property

        ' ── Accounting KPIs ──────────────────────────────────────────────────────

        Private _currentPeriodNetIncome As Decimal
        Public Property CurrentPeriodNetIncome As Decimal
            Get
                Return _currentPeriodNetIncome
            End Get
            Private Set(value As Decimal)
                SetProperty(_currentPeriodNetIncome, value)
            End Set
        End Property

        Private _totalArOutstanding As Decimal
        Public Property TotalArOutstanding As Decimal
            Get
                Return _totalArOutstanding
            End Get
            Private Set(value As Decimal)
                SetProperty(_totalArOutstanding, value)
            End Set
        End Property

        Private _totalApOutstanding As Decimal
        Public Property TotalApOutstanding As Decimal
            Get
                Return _totalApOutstanding
            End Get
            Private Set(value As Decimal)
                SetProperty(_totalApOutstanding, value)
            End Set
        End Property

        Private _accountingInterpretation As String = String.Empty
        Public Property AccountingInterpretation As String
            Get
                Return _accountingInterpretation
            End Get
            Private Set(value As String)
                SetProperty(_accountingInterpretation, value)
            End Set
        End Property

        ' ── Revenue Trend (additive read-only, period-selectable) ────────────────

        Private _selectedTrendPeriod As Integer = 30
        Public Property SelectedTrendPeriod As Integer
            Get
                Return _selectedTrendPeriod
            End Get
            Set(value As Integer)
                If SetProperty(_selectedTrendPeriod, value) Then
                    OnPropertyChanged(NameOf(Is7DaySelected))
                    OnPropertyChanged(NameOf(Is30DaySelected))
                    OnPropertyChanged(NameOf(Is90DaySelected))
                    Dim trendTask = LoadTrendDataAsync(value)
                End If
            End Set
        End Property

        Public Property Is7DaySelected As Boolean
            Get
                Return _selectedTrendPeriod = 7
            End Get
            Set(value As Boolean)
                If value Then SelectedTrendPeriod = 7
            End Set
        End Property

        Public Property Is30DaySelected As Boolean
            Get
                Return _selectedTrendPeriod = 30
            End Get
            Set(value As Boolean)
                If value Then SelectedTrendPeriod = 30
            End Set
        End Property

        Public Property Is90DaySelected As Boolean
            Get
                Return _selectedTrendPeriod = 90
            End Get
            Set(value As Boolean)
                If value Then SelectedTrendPeriod = 90
            End Set
        End Property

        Private _trendSparkPoints As IEnumerable(Of Double) = New List(Of Double)()
        Public Property TrendSparkPoints As IEnumerable(Of Double)
            Get
                Return _trendSparkPoints
            End Get
            Private Set(value As IEnumerable(Of Double))
                SetProperty(_trendSparkPoints, value)
            End Set
        End Property

        Private _trendSparkLabels As IEnumerable(Of String) = New List(Of String)()
        Public Property TrendSparkLabels As IEnumerable(Of String)
            Get
                Return _trendSparkLabels
            End Get
            Private Set(value As IEnumerable(Of String))
                SetProperty(_trendSparkLabels, value)
            End Set
        End Property

        ' ── State ────────────────────────────────────────────────────────────────

        Private _isLoading As Boolean
        Public Property IsLoading As Boolean
            Get
                Return _isLoading
            End Get
            Private Set(value As Boolean)
                SetProperty(_isLoading, value)
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Private Set(value As Boolean)
                SetProperty(_isError, value)
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Private Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Private _ownerDisplayName As String = String.Empty
        Public Property OwnerDisplayName As String
            Get
                Return _ownerDisplayName
            End Get
            Private Set(value As String)
                SetProperty(_ownerDisplayName, value)
            End Set
        End Property

        Private _lastRefreshedDisplay As String = "never"
        Public Property LastRefreshedDisplay As String
            Get
                Return _lastRefreshedDisplay
            End Get
            Private Set(value As String)
                SetProperty(_lastRefreshedDisplay, value)
            End Set
        End Property

        ' ── Commands & Events ────────────────────────────────────────────────────

        Public ReadOnly Property RefreshCommand As AsyncRelayCommand

        Public ReadOnly Property NavigateToPurchasingCommand As RelayCommand
        Public ReadOnly Property NavigateToInventoryCommand As RelayCommand
        Public ReadOnly Property NavigateToSalesCommand As RelayCommand
        Public ReadOnly Property NavigateToAccountingCommand As RelayCommand

        Public Event NavigateToPurchasingRequested As EventHandler
        Public Event NavigateToInventoryRequested As EventHandler
        Public Event NavigateToSalesRequested As EventHandler
        Public Event NavigateToAccountingRequested As EventHandler

        Public Sub New(session As ISessionService,
                       stockDashboard As IStockDashboardService,
                       lowStockAlert As ILowStockAlertService,
                       expiryTracking As IExpiryTrackingService,
                       purchaseOrder As IPurchaseOrderService,
                       vendorService As IVendorService,
                       accountsPayable As IAccountsPayableService,
                       dailySummary As IDailySummaryService,
                       financialOverview As IFinancialOverviewService,
                       incomeStatement As IIncomeStatementService)
            _session = session
            _stockDashboard = stockDashboard
            _lowStockAlert = lowStockAlert
            _expiryTracking = expiryTracking
            _purchaseOrder = purchaseOrder
            _vendor = vendorService
            _accountsPayable = accountsPayable
            _dailySummary = dailySummary
            _financialOverview = financialOverview
            _incomeStatement = incomeStatement

            OwnerDisplayName = _session.CurrentUsername
            RefreshCommand = New AsyncRelayCommand(AddressOf RefreshAsync)

            NavigateToPurchasingCommand = New RelayCommand(Sub() RaiseEvent NavigateToPurchasingRequested(Me, EventArgs.Empty))
            NavigateToInventoryCommand = New RelayCommand(Sub() RaiseEvent NavigateToInventoryRequested(Me, EventArgs.Empty))
            NavigateToSalesCommand = New RelayCommand(Sub() RaiseEvent NavigateToSalesRequested(Me, EventArgs.Empty))
            NavigateToAccountingCommand = New RelayCommand(Sub() RaiseEvent NavigateToAccountingRequested(Me, EventArgs.Empty))

            _refreshTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(60)}
            AddHandler _refreshTimer.Tick, AddressOf OnTimerTick
            _refreshTimer.Start()

            Dim loadTask = RefreshAsync()
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs)
            Dim loadTask = RefreshAsync()
        End Sub

        Private Async Function RefreshAsync() As Task
            If IsLoading Then Return
            IsError = False
            IsLoading = True
            Dim errMsg As String = Nothing
            Try
                Await LoadPurchasingKpisAsync()
                Await LoadInventoryKpisAsync()
                Await LoadSalesKpisAsync()
                Await LoadAccountingKpisAsync()
                Await LoadTrendDataAsync(_selectedTrendPeriod)
                LastRefreshedDisplay = $"Last refreshed: {DateTime.Now:HH:mm:ss}"
                LastLoadedAt = DateTime.Now
                IsError = False
            Catch ex As Exception
                errMsg = ex.Message
            End Try
            IsLoading = False
            ' errMsg captured to avoid Await-in-Catch (BC36943). KPI cards retain last-known-good values on failure.
            If errMsg IsNot Nothing Then
                ErrorMessage = errMsg
                IsError = True
            End If
        End Function

        Private Async Function LoadPurchasingKpisAsync() As Task
            Dim allVendors = Await _vendor.GetAllAsync()
            ActiveVendorCount = allVendors.Count

            Dim submittedPos = Await _purchaseOrder.GetAllAsync(PurchaseOrderStatus.Submitted)
            OpenPurchaseOrderCount = submittedPos.Count
            PendingDeliveryCount = submittedPos.Count

            Dim overdueEntries = Await _accountsPayable.GetOverdueAsync()
            OverdueApTotal = overdueEntries.Sum(Function(e) e.Balance)

            Dim outstandingAp = Await _accountsPayable.GetTotalOutstandingAsync()
            PurchasingInterpretation = BuildPurchasingInterpretation(outstandingAp)
        End Function

        Private Async Function LoadInventoryKpisAsync() As Task
            Dim dashboard = Await _stockDashboard.GetDashboardDataAsync()
            TotalSkuCount = dashboard.TotalProducts
            TotalStockValue = dashboard.TotalStockValue

            Dim alerts = Await _lowStockAlert.GetCurrentAlertsAsync()
            LowStockItemCount = alerts.Count

            Dim nearExpiryBatches = Await _expiryTracking.GetNearExpiryBatchesAsync(30)
            ExpiringSoonCount = nearExpiryBatches.Select(Function(b) b.ProductId).Distinct().Count()

            InventoryInterpretation = BuildInventoryInterpretation()
        End Function

        Private Async Function LoadSalesKpisAsync() As Task
            Dim today = DateTime.Today
            Dim dailyDto = Await _dailySummary.GetDailySummaryAsync(today)
            TodayRevenue = dailyDto.TotalSales
            TodayTransactionCount = dailyDto.TransactionCount

            Dim dayOfWeekNum = CInt(today.DayOfWeek)
            Dim weekStart = today.AddDays(-dayOfWeekNum)
            Dim weeklyDto = Await _dailySummary.GetWeeklySummaryAsync(weekStart)
            WeekRevenue = weeklyDto.TotalSales

            Dim yesterdayDto = Await _dailySummary.GetDailySummaryAsync(today.AddDays(-1))
            Dim yesterdayRevenue = yesterdayDto.TotalSales
            If yesterdayRevenue > 0 Then
                TodayRevenueDelta = CDbl(Math.Round(((TodayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100D, 1))
            Else
                TodayRevenueDelta = 0.0
            End If

            Dim lastWeekStart = weekStart.AddDays(-7)
            Dim lastWeekDto = Await _dailySummary.GetWeeklySummaryAsync(lastWeekStart)
            Dim lastWeekRevenue = lastWeekDto.TotalSales
            If lastWeekRevenue > 0 Then
                WeekRevenueDelta = CDbl(Math.Round(((WeekRevenue - lastWeekRevenue) / lastWeekRevenue) * 100D, 1))
            Else
                WeekRevenueDelta = 0.0
            End If

            If dailyDto.TopSellingProducts IsNot Nothing AndAlso dailyDto.TopSellingProducts.Count > 0 Then
                TopSellingProduct = dailyDto.TopSellingProducts(0).ProductName
            Else
                TopSellingProduct = "—"
            End If

            SalesInterpretation = BuildSalesInterpretation()
        End Function

        Private Async Function LoadAccountingKpisAsync() As Task
            Dim overview = Await _financialOverview.GetOverviewAsync()
            TotalArOutstanding = overview.TotalAR
            TotalApOutstanding = overview.TotalAP

            Dim today = DateTime.Today
            Dim statement = Await _incomeStatement.GenerateMonthlyAsync(today.Year, today.Month)
            CurrentPeriodNetIncome = statement.NetIncome

            AccountingInterpretation = BuildAccountingInterpretation()
        End Function

        ''' <summary>
        ''' Loads daily revenue totals for the past <paramref name="days"/> days using a raw
        ''' MySqlConnector reader (additive read-only; no existing VM property modified).
        ''' The read groups by calendar day; the result is then zero-filled across the full window
        ''' so a no-sales day reads as a zero bar and the sparkline's x-axis stays uniform
        ''' (left-to-right old→new), rather than silently collapsing missing days.
        ''' </summary>
        Private Async Function LoadTrendDataAsync(days As Integer) As Task
            Dim startDate = DateTime.Today.AddDays(-(days - 1)).Date
            Dim totalsByDay As Dictionary(Of DateTime, Decimal) = Nothing
            Dim errMsg As String = Nothing

            Try
                totalsByDay = Await _dailySummary.GetDailySalesTrendAsync(startDate)
            Catch ex As Exception
                errMsg = ex.Message
            End Try

            Dim pts As New List(Of Double)()
            Dim lbls As New List(Of String)()
            ' Zero-fill the full window so every day in the period has a bar (missing day → 0).
            ' On error, leave the series empty so the card shows no misleading flat-zero trend.
            If errMsg Is Nothing AndAlso totalsByDay IsNot Nothing Then
                For dayOffset = 0 To days - 1
                    Dim d = startDate.AddDays(dayOffset)
                    Dim dayTotalVal As Decimal = 0D
                    totalsByDay.TryGetValue(d, dayTotalVal)
                    pts.Add(CDbl(dayTotalVal))
                    lbls.Add(d.ToString("MMM d"))
                Next
            End If

            TrendSparkPoints = pts
            TrendSparkLabels = lbls
            If errMsg IsNot Nothing Then
                System.Console.WriteLine($"[OwnerDashboard] Revenue trend load error: {errMsg}")
            End If
        End Function

        Private Function BuildPurchasingInterpretation(outstandingAp As Decimal) As String
            If OpenPurchaseOrderCount = 0 AndAlso OverdueApTotal = 0 Then
                If outstandingAp = 0 Then
                    Return "No open purchase orders. All accounts payable are settled."
                Else
                    Return $"No open purchase orders. You have ₱{outstandingAp:N0} in accounts payable outstanding, but none are overdue."
                End If
            ElseIf OverdueApTotal > 0 Then
                Return $"You have {OpenPurchaseOrderCount} purchase order(s) awaiting delivery. ₱{OverdueApTotal:N0} in accounts payable is overdue and requires attention."
            Else
                Return $"You have {OpenPurchaseOrderCount} purchase order(s) awaiting delivery from {ActiveVendorCount} active vendor(s)."
            End If
        End Function

        Private Function BuildInventoryInterpretation() As String
            If LowStockItemCount = 0 AndAlso ExpiringSoonCount = 0 Then
                Return "All products are above minimum stock level and no items are expiring within 30 days."
            ElseIf LowStockItemCount > 0 AndAlso ExpiringSoonCount > 0 Then
                Return $"{LowStockItemCount} product(s) are below minimum stock level, and {ExpiringSoonCount} product(s) are expiring within 30 days. Both require attention."
            ElseIf LowStockItemCount > 0 Then
                Return $"{LowStockItemCount} product(s) are below minimum stock level. Check reorder suggestions."
            Else
                Return $"{ExpiringSoonCount} product(s) are expiring within 30 days. Review the Expiry Monitor."
            End If
        End Function

        Private Function BuildSalesInterpretation() As String
            If WeekRevenue = 0 Then
                Return "No sales recorded this week."
            ElseIf TodayRevenue = 0 Then
                Return $"This week's revenue is ₱{WeekRevenue:N0}. No sales have been recorded today yet."
            Else
                Return $"Today's revenue is ₱{TodayRevenue:N0} across {TodayTransactionCount} transaction(s). This week's total is ₱{WeekRevenue:N0}. Top product: {TopSellingProduct}."
            End If
        End Function

        Private Function BuildAccountingInterpretation() As String
            If CurrentPeriodNetIncome > 0 Then
                If TotalArOutstanding > 0 Then
                    Return $"Business is profitable this period with ₱{CurrentPeriodNetIncome:N0} net income. ₱{TotalArOutstanding:N0} in customer credit is outstanding."
                Else
                    Return $"Business is profitable this period with ₱{CurrentPeriodNetIncome:N0} net income. All customer credit is settled."
                End If
            ElseIf CurrentPeriodNetIncome = 0 Then
                Return "Business is at break-even this period. Review expenses to improve profitability."
            Else
                Return $"Business is operating at a loss of ₱{Math.Abs(CurrentPeriodNetIncome):N0} this period. Review cost controls immediately."
            End If
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposed Then Return
            _disposed = True
            _refreshTimer.Stop()
            RemoveHandler _refreshTimer.Tick, AddressOf OnTimerTick
        End Sub

    End Class

End Namespace
