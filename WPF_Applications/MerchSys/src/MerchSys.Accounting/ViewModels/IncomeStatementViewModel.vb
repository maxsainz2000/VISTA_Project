Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services

Namespace ViewModels

    Public Enum IncomeStatementPeriodType
        Monthly = 0
        Quarterly = 1
        Annual = 2
    End Enum

    Public Class IncomeStatementViewModel
        Inherits ObservableObject

        Private ReadOnly _incomeService As IIncomeStatementService
        Private ReadOnly _whatThisMeansService As IWhatThisMeansService

        Public Sub New(incomeService As IIncomeStatementService,
                       whatThisMeansService As IWhatThisMeansService)
            _incomeService = incomeService
            _whatThisMeansService = whatThisMeansService

            ProductMargins = New ObservableCollection(Of ProductMarginDto)()

            _selectedYear = DateTime.Today.Year
            _selectedMonth = DateTime.Today.Month
            _selectedQuarter = CInt(Math.Ceiling(DateTime.Today.Month / 3.0))

            LoadCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Period Type ──────────────────────────────────────────────────────────

        Private _periodType As IncomeStatementPeriodType = IncomeStatementPeriodType.Monthly

        Public Property IsMonthly As Boolean
            Get
                Return _periodType = IncomeStatementPeriodType.Monthly
            End Get
            Set(value As Boolean)
                If value Then PeriodType = IncomeStatementPeriodType.Monthly
            End Set
        End Property

        Public Property IsQuarterly As Boolean
            Get
                Return _periodType = IncomeStatementPeriodType.Quarterly
            End Get
            Set(value As Boolean)
                If value Then PeriodType = IncomeStatementPeriodType.Quarterly
            End Set
        End Property

        Public Property IsAnnual As Boolean
            Get
                Return _periodType = IncomeStatementPeriodType.Annual
            End Get
            Set(value As Boolean)
                If value Then PeriodType = IncomeStatementPeriodType.Annual
            End Set
        End Property

        Private Property PeriodType As IncomeStatementPeriodType
            Get
                Return _periodType
            End Get
            Set(value As IncomeStatementPeriodType)
                If _periodType <> value Then
                    _periodType = value
                    OnPropertyChanged(NameOf(IsMonthly))
                    OnPropertyChanged(NameOf(IsQuarterly))
                    OnPropertyChanged(NameOf(IsAnnual))
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        ' ─── Period Selectors ─────────────────────────────────────────────────────

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

        Private _selectedQuarter As Integer

        Public Property SelectedQuarterLabel As String
            Get
                Return $"Q{_selectedQuarter}"
            End Get
            Set(value As String)
                If value IsNot Nothing AndAlso value.Length = 2 Then
                    Dim q As Integer
                    If Integer.TryParse(value.Substring(1), q) AndAlso _selectedQuarter <> q Then
                        _selectedQuarter = q
                        OnPropertyChanged(NameOf(SelectedQuarterLabel))
                        Dim t = LoadDataAsync()
                    End If
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

        Public ReadOnly Property AvailableQuarterLabels As IReadOnlyList(Of String)
            Get
                Return New List(Of String) From {"Q1", "Q2", "Q3", "Q4"}
            End Get
        End Property

        ' ─── Income Statement Lines ───────────────────────────────────────────────
        ' Amounts are exposed as pre-formatted display strings:
        '   positive → ₱X,XXX.XX   |   negative/deduction → (₱X,XXX.XX)

        Private _periodDescription As String = String.Empty
        Public Property PeriodDescription As String
            Get
                Return _periodDescription
            End Get
            Set(value As String)
                SetProperty(_periodDescription, value)
            End Set
        End Property

        Private _netSalesDisplay As String = "₱0.00"
        Public Property NetSalesDisplay As String
            Get
                Return _netSalesDisplay
            End Get
            Set(value As String)
                SetProperty(_netSalesDisplay, value)
            End Set
        End Property

        Private _cogsDisplay As String = "(₱0.00)"
        Public Property COGSDisplay As String
            Get
                Return _cogsDisplay
            End Get
            Set(value As String)
                SetProperty(_cogsDisplay, value)
            End Set
        End Property

        Private _grossProfitDisplay As String = "₱0.00"
        Public Property GrossProfitDisplay As String
            Get
                Return _grossProfitDisplay
            End Get
            Set(value As String)
                SetProperty(_grossProfitDisplay, value)
            End Set
        End Property

        Private _grossMarginPercent As Decimal
        Public Property GrossMarginPercent As Decimal
            Get
                Return _grossMarginPercent
            End Get
            Set(value As Decimal)
                SetProperty(_grossMarginPercent, value)
            End Set
        End Property

        Private _operatingExpensesDisplay As String = "(₱0.00)"
        Public Property OperatingExpensesDisplay As String
            Get
                Return _operatingExpensesDisplay
            End Get
            Set(value As String)
                SetProperty(_operatingExpensesDisplay, value)
            End Set
        End Property

        Private _shrinkageLossDisplay As String = "(₱0.00)"
        Public Property ShrinkageLossDisplay As String
            Get
                Return _shrinkageLossDisplay
            End Get
            Set(value As String)
                SetProperty(_shrinkageLossDisplay, value)
            End Set
        End Property

        Private _netIncomeDisplay As String = "₱0.00"
        Public Property NetIncomeDisplay As String
            Get
                Return _netIncomeDisplay
            End Get
            Set(value As String)
                SetProperty(_netIncomeDisplay, value)
            End Set
        End Property

        Private _netMarginPercent As Decimal
        Public Property NetMarginPercent As Decimal
            Get
                Return _netMarginPercent
            End Get
            Set(value As Decimal)
                SetProperty(_netMarginPercent, value)
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

        ' ─── Per-Product Margins ──────────────────────────────────────────────────

        Public Property ProductMargins As ObservableCollection(Of ProductMarginDto)

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
                ' Declare shared locals — VB.NET Dim in Case blocks shares function scope
                Dim pm As Integer = 0
                Dim py As Integer = 0
                Dim pq As Integer = 0
                Dim statement As IncomeStatementDto = Nothing
                Dim prevResult As IncomeStatementDto = Nothing

                Select Case _periodType
                    Case IncomeStatementPeriodType.Monthly
                        statement = Await _incomeService.GenerateMonthlyAsync(_selectedYear, _selectedMonth)
                        pm = _selectedMonth - 1
                        py = _selectedYear
                        If pm < 1 Then pm = 12 : py -= 1
                        prevResult = Await _incomeService.GenerateMonthlyAsync(py, pm)

                    Case IncomeStatementPeriodType.Quarterly
                        statement = Await _incomeService.GenerateQuarterlyAsync(_selectedYear, _selectedQuarter)
                        pq = _selectedQuarter - 1
                        py = _selectedYear
                        If pq < 1 Then pq = 4 : py -= 1
                        prevResult = Await _incomeService.GenerateQuarterlyAsync(py, pq)

                    Case Else
                        statement = Await _incomeService.GenerateAnnualAsync(_selectedYear)
                        prevResult = Await _incomeService.GenerateAnnualAsync(_selectedYear - 1)
                End Select

                Dim previousMargin As Decimal = If(prevResult IsNot Nothing, prevResult.GrossMarginPercent, -1D)

                PeriodDescription = statement.PeriodDescription
                NetSalesDisplay = FormatAmount(statement.NetSales)
                COGSDisplay = FormatDeduction(statement.CostOfGoodsSold)
                GrossProfitDisplay = FormatAmount(statement.GrossProfit)
                GrossMarginPercent = statement.GrossMarginPercent
                OperatingExpensesDisplay = FormatDeduction(statement.OperatingExpenses)
                ShrinkageLossDisplay = FormatDeduction(statement.ShrinkageLoss)
                NetIncomeDisplay = FormatAmount(statement.NetIncome)
                NetMarginPercent = statement.NetMarginPercent

                WhatThisMeansText = _whatThisMeansService.GenerateIncomeStatementInterpretation(statement, previousMargin)

                Dim margins = Await _incomeService.GetPerProductMarginsAsync(statement.StartDate, statement.EndDate)
                Dim sorted = margins.OrderBy(Function(m) m.GrossMarginPercent).ToList()

                ProductMargins.Clear()
                For Each m In sorted
                    ProductMargins.Add(m)
                Next

            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Formatting Helpers ───────────────────────────────────────────────────

        Private Shared Function FormatAmount(amount As Decimal) As String
            If amount < 0D Then Return $"(₱{Math.Abs(amount):N2})"
            Return $"₱{amount:N2}"
        End Function

        Private Shared Function FormatDeduction(amount As Decimal) As String
            Return $"(₱{Math.Abs(amount):N2})"
        End Function

    End Class

End Namespace
