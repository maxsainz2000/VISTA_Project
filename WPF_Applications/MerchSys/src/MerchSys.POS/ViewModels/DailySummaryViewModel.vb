Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.POS.Services

Namespace ViewModels

    Public Enum SummaryPeriodMode
        Daily = 0
        Weekly = 1
        Monthly = 2
    End Enum

    Public Class TrendBarItem
        Public Property Label As String
        Public Property Sales As Decimal
        Public Property BarHeight As Double
        Public Property TooltipText As String
    End Class

    Public Class DailySummaryViewModel
        Inherits ObservableObject

        Private ReadOnly _summaryService As IDailySummaryService

        ' ── Period mode ───────────────────────────────────────────────────────────

        Private _periodMode As SummaryPeriodMode = SummaryPeriodMode.Daily
        Public Property PeriodMode As SummaryPeriodMode
            Get
                Return _periodMode
            End Get
            Set(value As SummaryPeriodMode)
                If SetProperty(_periodMode, value) Then
                    OnPropertyChanged(NameOf(IsDailyMode))
                    OnPropertyChanged(NameOf(IsWeeklyMode))
                    OnPropertyChanged(NameOf(IsMonthlyMode))
                    OnPropertyChanged(NameOf(IsTrendVisible))
                    OnPropertyChanged(NameOf(PeriodHeader))
                End If
            End Set
        End Property

        Public ReadOnly Property IsDailyMode As Boolean
            Get
                Return PeriodMode = SummaryPeriodMode.Daily
            End Get
        End Property

        Public ReadOnly Property IsWeeklyMode As Boolean
            Get
                Return PeriodMode = SummaryPeriodMode.Weekly
            End Get
        End Property

        Public ReadOnly Property IsMonthlyMode As Boolean
            Get
                Return PeriodMode = SummaryPeriodMode.Monthly
            End Get
        End Property

        Public ReadOnly Property IsTrendVisible As Boolean
            Get
                Return PeriodMode <> SummaryPeriodMode.Daily
            End Get
        End Property

        ' ── Period selection inputs ───────────────────────────────────────────────

        Private _selectedDate As DateTime = DateTime.Today
        Public Property SelectedDate As DateTime
            Get
                Return _selectedDate
            End Get
            Set(value As DateTime)
                If SetProperty(_selectedDate, value) Then
                    OnPropertyChanged(NameOf(PeriodHeader))
                End If
            End Set
        End Property

        Private _weekDate As DateTime = DateTime.Today
        Public Property WeekDate As DateTime
            Get
                Return _weekDate
            End Get
            Set(value As DateTime)
                If SetProperty(_weekDate, value) Then
                    OnPropertyChanged(NameOf(WeekRangeDisplay))
                    OnPropertyChanged(NameOf(PeriodHeader))
                End If
            End Set
        End Property

        Public ReadOnly Property WeekRangeDisplay As String
            Get
                Dim monday = GetWeekStart(WeekDate)
                Return monday.ToString("MMM d") & " – " & monday.AddDays(6).ToString("MMM d, yyyy")
            End Get
        End Property

        Private _selectedYear As Integer = DateTime.Today.Year
        Public Property SelectedYear As Integer
            Get
                Return _selectedYear
            End Get
            Set(value As Integer)
                If SetProperty(_selectedYear, value) Then
                    OnPropertyChanged(NameOf(PeriodHeader))
                End If
            End Set
        End Property

        Private _selectedMonthIndex As Integer = DateTime.Today.Month - 1
        Public Property SelectedMonthIndex As Integer
            Get
                Return _selectedMonthIndex
            End Get
            Set(value As Integer)
                If SetProperty(_selectedMonthIndex, value) Then
                    OnPropertyChanged(NameOf(PeriodHeader))
                End If
            End Set
        End Property

        Private ReadOnly Property SelectedMonth As Integer
            Get
                Return _selectedMonthIndex + 1
            End Get
        End Property

        Private _availableYears As List(Of Integer)
        Public Property AvailableYears As List(Of Integer)
            Get
                Return _availableYears
            End Get
            Private Set(value As List(Of Integer))
                _availableYears = value
            End Set
        End Property

        Private _availableMonths As List(Of String)
        Public Property AvailableMonths As List(Of String)
            Get
                Return _availableMonths
            End Get
            Private Set(value As List(Of String))
                _availableMonths = value
            End Set
        End Property

        Public ReadOnly Property PeriodHeader As String
            Get
                Select Case PeriodMode
                    Case SummaryPeriodMode.Daily
                        Return SelectedDate.ToString("dddd, MMMM d, yyyy")
                    Case SummaryPeriodMode.Weekly
                        Return "Week of " & GetWeekStart(WeekDate).ToString("MMMM d, yyyy")
                    Case Else
                        If _selectedMonthIndex >= 0 AndAlso _selectedMonthIndex < _availableMonths.Count Then
                            Return _availableMonths(_selectedMonthIndex) & " " & SelectedYear.ToString()
                        End If
                        Return SelectedYear.ToString()
                End Select
            End Get
        End Property

        ' ── KPI totals ───────────────────────────────────────────────────────────

        Private _totalSales As Decimal
        Public Property TotalSales As Decimal
            Get
                Return _totalSales
            End Get
            Set(value As Decimal)
                If SetProperty(_totalSales, value) Then
                    OnPropertyChanged(NameOf(TotalSalesDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property TotalSalesDisplay As String
            Get
                Return "₱" & TotalSales.ToString("N2")
            End Get
        End Property

        Private _salesDeltaPercent As Double
        Public Property SalesDeltaPercent As Double
            Get
                Return _salesDeltaPercent
            End Get
            Private Set(value As Double)
                SetProperty(_salesDeltaPercent, value)
            End Set
        End Property

        Private _transactionCount As Integer
        Public Property TransactionCount As Integer
            Get
                Return _transactionCount
            End Get
            Set(value As Integer)
                SetProperty(_transactionCount, value)
            End Set
        End Property

        Private _avgTransactionValue As Decimal
        Public Property AverageTransactionValue As Decimal
            Get
                Return _avgTransactionValue
            End Get
            Set(value As Decimal)
                If SetProperty(_avgTransactionValue, value) Then
                    OnPropertyChanged(NameOf(AvgTransactionDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property AvgTransactionDisplay As String
            Get
                Return "₱" & AverageTransactionValue.ToString("N2")
            End Get
        End Property

        Private _returnCount As Integer
        Public Property ReturnCount As Integer
            Get
                Return _returnCount
            End Get
            Set(value As Integer)
                If SetProperty(_returnCount, value) Then
                    OnPropertyChanged(NameOf(ReturnsDisplay))
                End If
            End Set
        End Property

        Private _returnValue As Decimal
        Public Property ReturnValue As Decimal
            Get
                Return _returnValue
            End Get
            Set(value As Decimal)
                If SetProperty(_returnValue, value) Then
                    OnPropertyChanged(NameOf(ReturnsDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property ReturnsDisplay As String
            Get
                Return ReturnCount.ToString() & " (₱" & ReturnValue.ToString("N2") & ")"
            End Get
        End Property

        ' ── Collections ──────────────────────────────────────────────────────────

        Private _paymentBreakdown As ObservableCollection(Of PaymentMethodBreakdownDto)
        Public Property PaymentBreakdown As ObservableCollection(Of PaymentMethodBreakdownDto)
            Get
                Return _paymentBreakdown
            End Get
            Private Set(value As ObservableCollection(Of PaymentMethodBreakdownDto))
                _paymentBreakdown = value
            End Set
        End Property

        Private _topProducts As ObservableCollection(Of TopProductDto)
        Public Property TopProducts As ObservableCollection(Of TopProductDto)
            Get
                Return _topProducts
            End Get
            Private Set(value As ObservableCollection(Of TopProductDto))
                _topProducts = value
            End Set
        End Property

        Private _trendBars As ObservableCollection(Of TrendBarItem)
        Public Property TrendBars As ObservableCollection(Of TrendBarItem)
            Get
                Return _trendBars
            End Get
            Private Set(value As ObservableCollection(Of TrendBarItem))
                _trendBars = value
            End Set
        End Property

        ' ── UI state ─────────────────────────────────────────────────────────────

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

        Private _isStatusSuccess As Boolean = True
        Public Property IsStatusSuccess As Boolean
            Get
                Return _isStatusSuccess
            End Get
            Set(value As Boolean)
                SetProperty(_isStatusSuccess, value)
            End Set
        End Property

        ' ── Commands ─────────────────────────────────────────────────────────────

        Private _loadCommand As AsyncRelayCommand
        Public Property LoadCommand As AsyncRelayCommand
            Get
                Return _loadCommand
            End Get
            Private Set(value As AsyncRelayCommand)
                _loadCommand = value
            End Set
        End Property

        Private _prevPeriodCommand As AsyncRelayCommand
        Public Property PrevPeriodCommand As AsyncRelayCommand
            Get
                Return _prevPeriodCommand
            End Get
            Private Set(value As AsyncRelayCommand)
                _prevPeriodCommand = value
            End Set
        End Property

        Private _nextPeriodCommand As AsyncRelayCommand
        Public Property NextPeriodCommand As AsyncRelayCommand
            Get
                Return _nextPeriodCommand
            End Get
            Private Set(value As AsyncRelayCommand)
                _nextPeriodCommand = value
            End Set
        End Property

        Private _setDailyModeCommand As RelayCommand
        Public Property SetDailyModeCommand As RelayCommand
            Get
                Return _setDailyModeCommand
            End Get
            Private Set(value As RelayCommand)
                _setDailyModeCommand = value
            End Set
        End Property

        Private _setWeeklyModeCommand As RelayCommand
        Public Property SetWeeklyModeCommand As RelayCommand
            Get
                Return _setWeeklyModeCommand
            End Get
            Private Set(value As RelayCommand)
                _setWeeklyModeCommand = value
            End Set
        End Property

        Private _setMonthlyModeCommand As RelayCommand
        Public Property SetMonthlyModeCommand As RelayCommand
            Get
                Return _setMonthlyModeCommand
            End Get
            Private Set(value As RelayCommand)
                _setMonthlyModeCommand = value
            End Set
        End Property

        ' ── Constructor ──────────────────────────────────────────────────────────

        Public Sub New(summaryService As IDailySummaryService)
            _summaryService = summaryService

            Dim currentYear = DateTime.Today.Year
            _availableYears = New List(Of Integer)()
            For y = currentYear - 3 To currentYear + 1
                _availableYears.Add(y)
            Next

            _availableMonths = New List(Of String) From {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            }

            _paymentBreakdown = New ObservableCollection(Of PaymentMethodBreakdownDto)()
            _topProducts = New ObservableCollection(Of TopProductDto)()
            _trendBars = New ObservableCollection(Of TrendBarItem)()

            _loadCommand = New AsyncRelayCommand(AddressOf LoadAsync)
            _prevPeriodCommand = New AsyncRelayCommand(AddressOf PrevPeriodAsync)
            _nextPeriodCommand = New AsyncRelayCommand(AddressOf NextPeriodAsync)
            _setDailyModeCommand = New RelayCommand(Sub() PeriodMode = SummaryPeriodMode.Daily)
            _setWeeklyModeCommand = New RelayCommand(Sub() PeriodMode = SummaryPeriodMode.Weekly)
            _setMonthlyModeCommand = New RelayCommand(Sub() PeriodMode = SummaryPeriodMode.Monthly)
        End Sub

        ' ── Load ─────────────────────────────────────────────────────────────────

        Public Async Function LoadAsync() As Task
            IsBusy = True
            StatusMessage = String.Empty
            Try
                Dim prevSales As Decimal = 0D
                Select Case PeriodMode
                    Case SummaryPeriodMode.Daily
                        Dim dto = Await _summaryService.GetDailySummaryAsync(SelectedDate)
                        ApplyDailySummary(dto)

                        Dim prevDto = Await _summaryService.GetDailySummaryAsync(SelectedDate.AddDays(-1))
                        prevSales = prevDto.TotalSales

                    Case SummaryPeriodMode.Weekly
                        Dim dto = Await _summaryService.GetWeeklySummaryAsync(GetWeekStart(WeekDate))
                        ApplyPeriodSummary(dto, isWeekly:=True)

                        Dim prevDto = Await _summaryService.GetWeeklySummaryAsync(GetWeekStart(WeekDate).AddDays(-7))
                        prevSales = prevDto.TotalSales

                    Case SummaryPeriodMode.Monthly
                        Dim dto = Await _summaryService.GetMonthlySummaryAsync(SelectedYear, SelectedMonth)
                        ApplyPeriodSummary(dto, isWeekly:=False)

                        Dim prevMonthDate = New DateTime(SelectedYear, SelectedMonth, 1).AddMonths(-1)
                        Dim prevDto = Await _summaryService.GetMonthlySummaryAsync(prevMonthDate.Year, prevMonthDate.Month)
                        prevSales = prevDto.TotalSales
                End Select

                If prevSales > 0 Then
                    SalesDeltaPercent = CDbl(Math.Round(((TotalSales - prevSales) / prevSales) * 100D, 1))
                Else
                    SalesDeltaPercent = 0.0
                End If

                IsStatusSuccess = True
                StatusMessage = "Loaded: " & PeriodHeader
            Catch ex As Exception
                IsStatusSuccess = False
                StatusMessage = "Error loading summary: " & ex.Message
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ApplyDailySummary(dto As DailySummaryDto)
            TotalSales = dto.TotalSales
            TransactionCount = dto.TransactionCount
            AverageTransactionValue = dto.AverageTransactionValue
            ReturnCount = dto.ReturnCount
            ReturnValue = dto.ReturnValue

            _paymentBreakdown.Clear()
            For Each item In dto.PaymentBreakdown
                _paymentBreakdown.Add(item)
            Next

            _topProducts.Clear()
            For Each item In dto.TopSellingProducts
                _topProducts.Add(item)
            Next

            _trendBars.Clear()
        End Sub

        Private Sub ApplyPeriodSummary(dto As PeriodSummaryDto, isWeekly As Boolean)
            TotalSales = dto.TotalSales
            TransactionCount = dto.TransactionCount
            AverageTransactionValue = dto.AverageTransactionValue
            ReturnCount = dto.ReturnCount
            ReturnValue = dto.ReturnValue

            _paymentBreakdown.Clear()
            For Each item In dto.PaymentBreakdown
                _paymentBreakdown.Add(item)
            Next

            _topProducts.Clear()
            For Each item In dto.TopSellingProducts
                _topProducts.Add(item)
            Next

            _trendBars.Clear()
            If dto.DailyBreakdown Is Nothing OrElse dto.DailyBreakdown.Count = 0 Then Return

            Dim maxSales = dto.DailyBreakdown.Max(Function(s) s.TotalSales)
            Const MaxBarPx As Double = 100.0

            For Each slot In dto.DailyBreakdown
                Dim rawH = If(maxSales > 0D, CDbl(slot.TotalSales / maxSales) * MaxBarPx, 0.0)
                Dim barH = If(slot.TotalSales > 0D, Math.Max(rawH, 2.0), 0.0)
                Dim lbl = If(isWeekly,
                             slot.[Date].ToString("ddd") & Environment.NewLine & slot.[Date].ToString("M/d"),
                             slot.[Date].Day.ToString())
                _trendBars.Add(New TrendBarItem() With {
                    .Label = lbl,
                    .Sales = slot.TotalSales,
                    .BarHeight = barH,
                    .TooltipText = slot.[Date].ToString("MMM d") & ": ₱" & slot.TotalSales.ToString("N2")
                })
            Next
        End Sub

        ' ── Period navigation ────────────────────────────────────────────────────

        Private Async Function PrevPeriodAsync() As Task
            Select Case PeriodMode
                Case SummaryPeriodMode.Daily
                    SelectedDate = SelectedDate.AddDays(-1)
                Case SummaryPeriodMode.Weekly
                    WeekDate = WeekDate.AddDays(-7)
                Case SummaryPeriodMode.Monthly
                    Dim d = New DateTime(SelectedYear, SelectedMonth, 1).AddMonths(-1)
                    SelectedYear = d.Year
                    SelectedMonthIndex = d.Month - 1
            End Select
            Await LoadAsync()
        End Function

        Private Async Function NextPeriodAsync() As Task
            Select Case PeriodMode
                Case SummaryPeriodMode.Daily
                    SelectedDate = SelectedDate.AddDays(1)
                Case SummaryPeriodMode.Weekly
                    WeekDate = WeekDate.AddDays(7)
                Case SummaryPeriodMode.Monthly
                    Dim d = New DateTime(SelectedYear, SelectedMonth, 1).AddMonths(1)
                    SelectedYear = d.Year
                    SelectedMonthIndex = d.Month - 1
            End Select
            Await LoadAsync()
        End Function

        ' ── Helper ───────────────────────────────────────────────────────────────

        Private Shared Function GetWeekStart(dt As DateTime) As DateTime
            Dim dow = CInt(dt.DayOfWeek)
            Dim daysToMonday = If(dow = 0, 6, dow - 1)
            Return dt.Date.AddDays(-daysToMonday)
        End Function

    End Class

End Namespace
