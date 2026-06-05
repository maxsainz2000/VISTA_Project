Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Timers
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation

Namespace ViewModels

    ''' <summary>
    ''' Flat item for the 6-month trend bar chart.
    ''' Bar heights are pre-computed relative to the period's maximum revenue (max = 120px).
    ''' </summary>
    Public Class TrendBarItem
        Public Property Month As String
        Public Property Revenue As Decimal
        Public Property COGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property GrossMarginPercent As Decimal
        Public Property RevenueBarHeight As Double
        Public Property COGSBarHeight As Double
        Public Property GrossProfitBarHeight As Double
    End Class

    ''' <summary>
    ''' ViewModel for the Financial Overview Dashboard — the primary Accounting screen.
    ''' Aggregates KPIs, 6-month trend, top products, alerts, and plain-language summary.
    ''' Auto-refreshes every 5 minutes and on each navigation to the view.
    ''' </summary>
    Public Class FinancialOverviewViewModel
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

        Private ReadOnly _overviewService As IFinancialOverviewService
        Private ReadOnly _whatThisMeansService As IWhatThisMeansService
        Private ReadOnly _refreshTimer As System.Timers.Timer
        Private ReadOnly _uiContext As SynchronizationContext

        Public Sub New(overviewService As IFinancialOverviewService,
                       whatThisMeansService As IWhatThisMeansService)

            _overviewService = overviewService
            _whatThisMeansService = whatThisMeansService
            _uiContext = SynchronizationContext.Current

            TopProducts = New ObservableCollection(Of TopProductDto)()
            MonthlyTrend = New ObservableCollection(Of TrendBarItem)()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)

            _refreshTimer = New System.Timers.Timer(300_000) With {.AutoReset = True}
            AddHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
            _refreshTimer.Start()

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── KPI Properties ───────────────────────────────────────────────────────

        Private _todayRevenue As Decimal
        Public Property TodayRevenue As Decimal
            Get
                Return _todayRevenue
            End Get
            Set(value As Decimal)
                SetProperty(_todayRevenue, value)
            End Set
        End Property

        Private _monthToDateRevenue As Decimal
        Public Property MonthToDateRevenue As Decimal
            Get
                Return _monthToDateRevenue
            End Get
            Set(value As Decimal)
                SetProperty(_monthToDateRevenue, value)
            End Set
        End Property

        Private _yearToDateRevenue As Decimal
        Public Property YearToDateRevenue As Decimal
            Get
                Return _yearToDateRevenue
            End Get
            Set(value As Decimal)
                SetProperty(_yearToDateRevenue, value)
            End Set
        End Property

        Private _currentGrossMargin As Decimal
        Public Property CurrentGrossMargin As Decimal
            Get
                Return _currentGrossMargin
            End Get
            Set(value As Decimal)
                SetProperty(_currentGrossMargin, value)
            End Set
        End Property

        Private _totalAR As Decimal
        Public Property TotalAR As Decimal
            Get
                Return _totalAR
            End Get
            Set(value As Decimal)
                SetProperty(_totalAR, value)
            End Set
        End Property

        Private _totalAP As Decimal
        Public Property TotalAP As Decimal
            Get
                Return _totalAP
            End Get
            Set(value As Decimal)
                SetProperty(_totalAP, value)
            End Set
        End Property

        Private _inventoryValue As Decimal
        Public Property InventoryValue As Decimal
            Get
                Return _inventoryValue
            End Get
            Set(value As Decimal)
                SetProperty(_inventoryValue, value)
            End Set
        End Property

        ' ─── What This Means ──────────────────────────────────────────────────────

        Private _whatThisMeansText As String = String.Empty
        Public Property WhatThisMeansText As String
            Get
                Return _whatThisMeansText
            End Get
            Set(value As String)
                SetProperty(_whatThisMeansText, value)
            End Set
        End Property

        ' ─── Alerts ───────────────────────────────────────────────────────────────

        Private _overdueARCount As Integer
        Public Property OverdueARCount As Integer
            Get
                Return _overdueARCount
            End Get
            Set(value As Integer)
                SetProperty(_overdueARCount, value)
                OnPropertyChanged(NameOf(HasOverdueAR))
            End Set
        End Property

        Private _overdueAPCount As Integer
        Public Property OverdueAPCount As Integer
            Get
                Return _overdueAPCount
            End Get
            Set(value As Integer)
                SetProperty(_overdueAPCount, value)
                OnPropertyChanged(NameOf(HasOverdueAP))
            End Set
        End Property

        Private _lowStockAlertCount As Integer
        Public Property LowStockAlertCount As Integer
            Get
                Return _lowStockAlertCount
            End Get
            Set(value As Integer)
                SetProperty(_lowStockAlertCount, value)
                OnPropertyChanged(NameOf(HasLowStock))
            End Set
        End Property

        Public ReadOnly Property HasOverdueAR As Boolean
            Get
                Return OverdueARCount > 0
            End Get
        End Property

        Public ReadOnly Property HasOverdueAP As Boolean
            Get
                Return OverdueAPCount > 0
            End Get
        End Property

        Public ReadOnly Property HasLowStock As Boolean
            Get
                Return LowStockAlertCount > 0
            End Get
        End Property

        ' ─── Collections ──────────────────────────────────────────────────────────

        Public Property TopProducts As ObservableCollection(Of TopProductDto)
        Public Property MonthlyTrend As ObservableCollection(Of TrendBarItem)

        Private _revenueDeltaPercent As Double
        Public Property RevenueDeltaPercent As Double
            Get
                Return _revenueDeltaPercent
            End Get
            Private Set(value As Double)
                SetProperty(_revenueDeltaPercent, value)
            End Set
        End Property

        Private _revenueSparkPoints As IEnumerable(Of Double)
        Public Property RevenueSparkPoints As IEnumerable(Of Double)
            Get
                Return _revenueSparkPoints
            End Get
            Private Set(value As IEnumerable(Of Double))
                SetProperty(_revenueSparkPoints, value)
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
                Return TopProducts.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
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

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim data = Await _overviewService.GetOverviewAsync()

                TodayRevenue = data.TodayRevenue
                MonthToDateRevenue = data.MonthToDateRevenue
                YearToDateRevenue = data.YearToDateRevenue
                CurrentGrossMargin = data.CurrentGrossMargin
                TotalAR = data.TotalAR
                TotalAP = data.TotalAP
                InventoryValue = data.InventoryValue
                OverdueARCount = data.OverdueARCount
                OverdueAPCount = data.OverdueAPCount
                LowStockAlertCount = data.LowStockAlertCount

                WhatThisMeansText = _whatThisMeansService.GenerateOverviewInterpretation(data)

                BuildTrendBars(data.MonthlyTrend)

                RevenueSparkPoints = data.MonthlyTrend.Select(Function(t) CDbl(t.Revenue)).ToList()

                If data.MonthlyTrend.Count >= 2 Then
                    Dim priorMonthRevenue = data.MonthlyTrend(data.MonthlyTrend.Count - 2).Revenue
                    If priorMonthRevenue > 0 Then
                        RevenueDeltaPercent = CDbl(Math.Round(((MonthToDateRevenue - priorMonthRevenue) / priorMonthRevenue) * 100D, 1))
                    Else
                        RevenueDeltaPercent = 0.0
                    End If
                Else
                    RevenueDeltaPercent = 0.0
                End If

                TopProducts.Clear()
                For Each p In data.TopProducts
                    TopProducts.Add(p)
                Next

                LastRefreshed = $"Refreshed {DateTime.Now:HH:mm:ss}"
                IsError = False
                LastLoadedAt = DateTime.Now

            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub BuildTrendBars(trend As List(Of MonthlyTrendDto))
            Const MaxBarPx As Double = 120.0
            Dim maxRevenue As Decimal = trend.Select(Function(t) t.Revenue).DefaultIfEmpty(1D).Max()
            If maxRevenue <= 0 Then maxRevenue = 1D

            MonthlyTrend.Clear()
            For Each t In trend
                Dim gpClamped As Decimal = Math.Max(0, t.GrossProfit)
                MonthlyTrend.Add(New TrendBarItem With {
                    .Month = t.Month,
                    .Revenue = t.Revenue,
                    .COGS = t.COGS,
                    .GrossProfit = t.GrossProfit,
                    .GrossMarginPercent = t.GrossMarginPercent,
                    .RevenueBarHeight = Math.Max(2.0, CDbl(t.Revenue / maxRevenue) * MaxBarPx),
                    .COGSBarHeight = Math.Max(2.0, CDbl(t.COGS / maxRevenue) * MaxBarPx),
                    .GrossProfitBarHeight = Math.Max(0.0, CDbl(gpClamped / maxRevenue) * MaxBarPx)
                })
            Next
        End Sub

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
