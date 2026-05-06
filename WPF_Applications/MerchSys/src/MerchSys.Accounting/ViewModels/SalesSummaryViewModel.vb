Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services

Namespace ViewModels

    Public Enum SalesSummaryPeriodType
        Daily = 0
        Weekly = 1
        Monthly = 2
    End Enum

    Public Class SalesSummaryViewModel
        Inherits ObservableObject

        Private ReadOnly _salesService As ISalesSummaryService
        Private ReadOnly _whatThisMeansService As IWhatThisMeansService

        Public Sub New(salesService As ISalesSummaryService,
                       whatThisMeansService As IWhatThisMeansService)
            _salesService = salesService
            _whatThisMeansService = whatThisMeansService

            PaymentBreakdown = New ObservableCollection(Of PaymentBreakdownDto)()
            DailyBreakdown = New ObservableCollection(Of DailySalesDto)()

            _selectedDate = DateTime.Today
            _selectedYear = DateTime.Today.Year
            _selectedMonth = DateTime.Today.Month

            LoadCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Period Type ──────────────────────────────────────────────────────────

        Private _periodType As SalesSummaryPeriodType = SalesSummaryPeriodType.Monthly

        Public Property IsDaily As Boolean
            Get
                Return _periodType = SalesSummaryPeriodType.Daily
            End Get
            Set(value As Boolean)
                If value Then PeriodType = SalesSummaryPeriodType.Daily
            End Set
        End Property

        Public Property IsWeekly As Boolean
            Get
                Return _periodType = SalesSummaryPeriodType.Weekly
            End Get
            Set(value As Boolean)
                If value Then PeriodType = SalesSummaryPeriodType.Weekly
            End Set
        End Property

        Public Property IsMonthly As Boolean
            Get
                Return _periodType = SalesSummaryPeriodType.Monthly
            End Get
            Set(value As Boolean)
                If value Then PeriodType = SalesSummaryPeriodType.Monthly
            End Set
        End Property

        Private Property PeriodType As SalesSummaryPeriodType
            Get
                Return _periodType
            End Get
            Set(value As SalesSummaryPeriodType)
                If _periodType <> value Then
                    _periodType = value
                    OnPropertyChanged(NameOf(IsDaily))
                    OnPropertyChanged(NameOf(IsWeekly))
                    OnPropertyChanged(NameOf(IsMonthly))
                    OnPropertyChanged(NameOf(ShowDatePicker))
                    OnPropertyChanged(NameOf(ShowMonthSelectors))
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        ' ─── Visibility Helpers ───────────────────────────────────────────────────

        Public ReadOnly Property ShowDatePicker As Boolean
            Get
                Return _periodType = SalesSummaryPeriodType.Daily OrElse
                       _periodType = SalesSummaryPeriodType.Weekly
            End Get
        End Property

        Public ReadOnly Property ShowMonthSelectors As Boolean
            Get
                Return _periodType = SalesSummaryPeriodType.Monthly
            End Get
        End Property

        Private _showDailyBreakdown As Boolean
        Public Property ShowDailyBreakdown As Boolean
            Get
                Return _showDailyBreakdown
            End Get
            Set(value As Boolean)
                SetProperty(_showDailyBreakdown, value)
            End Set
        End Property

        ' ─── Date Selectors ───────────────────────────────────────────────────────

        Private _selectedDate As DateTime
        Public Property SelectedDate As DateTime
            Get
                Return _selectedDate
            End Get
            Set(value As DateTime)
                If SetProperty(_selectedDate, value) Then
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        Private _selectedYear As Integer
        Public Property SelectedYear As Integer
            Get
                Return _selectedYear
            End Get
            Set(value As Integer)
                If SetProperty(_selectedYear, value) Then
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        Private _selectedMonth As Integer
        Public Property SelectedMonth As Integer
            Get
                Return _selectedMonth
            End Get
            Set(value As Integer)
                If SetProperty(_selectedMonth, value) Then
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        Public ReadOnly Property AvailableYears As IReadOnlyList(Of Integer)
            Get
                Dim current = DateTime.Today.Year
                Return Enumerable.Range(current - 4, 5).OrderByDescending(Function(y) y).ToList()
            End Get
        End Property

        Public ReadOnly Property AvailableMonths As IReadOnlyList(Of Integer)
            Get
                Return Enumerable.Range(1, 12).ToList()
            End Get
        End Property

        ' ─── Summary Cards ────────────────────────────────────────────────────────

        Private _periodDescription As String = String.Empty
        Public Property PeriodDescription As String
            Get
                Return _periodDescription
            End Get
            Set(value As String)
                SetProperty(_periodDescription, value)
            End Set
        End Property

        Private _totalNetSalesDisplay As String = "₱0.00"
        Public Property TotalNetSalesDisplay As String
            Get
                Return _totalNetSalesDisplay
            End Get
            Set(value As String)
                SetProperty(_totalNetSalesDisplay, value)
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

        Private _avgTransactionValueDisplay As String = "₱0.00"
        Public Property AvgTransactionValueDisplay As String
            Get
                Return _avgTransactionValueDisplay
            End Get
            Set(value As String)
                SetProperty(_avgTransactionValueDisplay, value)
            End Set
        End Property

        Private _totalReturnsDisplay As String = "₱0.00"
        Public Property TotalReturnsDisplay As String
            Get
                Return _totalReturnsDisplay
            End Get
            Set(value As String)
                SetProperty(_totalReturnsDisplay, value)
            End Set
        End Property

        ' Totals displayed in the payment breakdown footer row
        Private _totalGrossSalesDisplay As String = "₱0.00"
        Public Property TotalGrossSalesDisplay As String
            Get
                Return _totalGrossSalesDisplay
            End Get
            Set(value As String)
                SetProperty(_totalGrossSalesDisplay, value)
            End Set
        End Property

        Private _totalTxCountDisplay As Integer
        Public Property TotalTxCountDisplay As Integer
            Get
                Return _totalTxCountDisplay
            End Get
            Set(value As Integer)
                SetProperty(_totalTxCountDisplay, value)
            End Set
        End Property

        ' ─── Credit Warning ───────────────────────────────────────────────────────

        Private _hasCreditWarning As Boolean
        Public Property HasCreditWarning As Boolean
            Get
                Return _hasCreditWarning
            End Get
            Set(value As Boolean)
                SetProperty(_hasCreditWarning, value)
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

        ' ─── Collections ──────────────────────────────────────────────────────────

        Public Property PaymentBreakdown As ObservableCollection(Of PaymentBreakdownDto)
        Public Property DailyBreakdown As ObservableCollection(Of DailySalesDto)

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

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property LoadCommand As AsyncRelayCommand

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsBusy = True
            Try
                Dim summary As AccountingSalesSummaryDto

                Select Case _periodType
                    Case SalesSummaryPeriodType.Daily
                        summary = Await _salesService.GetDailySummaryAsync(_selectedDate)

                    Case SalesSummaryPeriodType.Weekly
                        ' Anchor to Sunday of the selected week
                        Dim weekStart = _selectedDate.AddDays(-(CInt(_selectedDate.DayOfWeek)))
                        summary = Await _salesService.GetWeeklySummaryAsync(weekStart)

                    Case Else
                        summary = Await _salesService.GetMonthlySummaryAsync(_selectedYear, _selectedMonth)
                End Select

                PeriodDescription = summary.PeriodDescription
                TotalNetSalesDisplay = FormatAmount(summary.TotalNetSales)
                TransactionCount = summary.TransactionCount
                AvgTransactionValueDisplay = FormatAmount(
                    If(summary.TransactionCount > 0,
                       Math.Round(summary.TotalNetSales / summary.TransactionCount, 2),
                       0D))
                TotalReturnsDisplay = FormatAmount(summary.TotalReturns)
                TotalGrossSalesDisplay = FormatAmount(summary.TotalGrossSales)
                TotalTxCountDisplay = summary.TransactionCount

                Dim creditEntry = summary.PaymentBreakdown?.Find(
                    Function(p) p.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                HasCreditWarning = creditEntry IsNot Nothing AndAlso creditEntry.Percentage >= 30D

                WhatThisMeansText = _whatThisMeansService.GenerateSalesSummaryInterpretation(summary)

                PaymentBreakdown.Clear()
                If summary.PaymentBreakdown IsNot Nothing Then
                    For Each p In summary.PaymentBreakdown
                        PaymentBreakdown.Add(p)
                    Next
                End If

                DailyBreakdown.Clear()
                If summary.DailyBreakdown IsNot Nothing Then
                    For Each d In summary.DailyBreakdown
                        DailyBreakdown.Add(d)
                    Next
                End If

                ShowDailyBreakdown = (_periodType <> SalesSummaryPeriodType.Daily) AndAlso
                                     DailyBreakdown.Count > 0

            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Formatting Helpers ───────────────────────────────────────────────────

        Private Shared Function FormatAmount(amount As Decimal) As String
            Return $"₱{amount:N2}"
        End Function

    End Class

End Namespace
