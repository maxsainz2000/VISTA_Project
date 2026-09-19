Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services
Imports MerchSys.SharedKernel.Enums

Namespace ViewModels

    Public Enum IncomeStatementPeriodType
        Monthly = 0
        Quarterly = 1
        Annual = 2
    End Enum

    Public Class MonthOption
        Public Property Number As Integer
        Public Property Name As String
        Public Sub New(num As Integer, nm As String)
            Number = num
            Name = nm
        End Sub
    End Class

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

        Public ReadOnly Property AvailableMonths As IReadOnlyList(Of MonthOption)
            Get
                Return New List(Of MonthOption) From {
                    New MonthOption(1, "January"),
                    New MonthOption(2, "February"),
                    New MonthOption(3, "March"),
                    New MonthOption(4, "April"),
                    New MonthOption(5, "May"),
                    New MonthOption(6, "June"),
                    New MonthOption(7, "July"),
                    New MonthOption(8, "August"),
                    New MonthOption(9, "September"),
                    New MonthOption(10, "October"),
                    New MonthOption(11, "November"),
                    New MonthOption(12, "December")
                }
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

        ' ─── Prior Period Labels & Display Strings ──────────────────────────────────

        Private _prevPeriodLabel As String = "Prior Period"
        Public Property PrevPeriodLabel As String
            Get
                Return _prevPeriodLabel
            End Get
            Set(value As String)
                SetProperty(_prevPeriodLabel, value)
            End Set
        End Property

        Private _prevNetSalesDisplay As String = "₱0.00"
        Public Property PrevNetSalesDisplay As String
            Get
                Return _prevNetSalesDisplay
            End Get
            Set(value As String)
                SetProperty(_prevNetSalesDisplay, value)
            End Set
        End Property

        Private _prevCOGSDisplay As String = "(₱0.00)"
        Public Property PrevCOGSDisplay As String
            Get
                Return _prevCOGSDisplay
            End Get
            Set(value As String)
                SetProperty(_prevCOGSDisplay, value)
            End Set
        End Property

        Private _prevGrossProfitDisplay As String = "₱0.00"
        Public Property PrevGrossProfitDisplay As String
            Get
                Return _prevGrossProfitDisplay
            End Get
            Set(value As String)
                SetProperty(_prevGrossProfitDisplay, value)
            End Set
        End Property

        Private _prevOtherOperatingExpensesDisplay As String = "(₱0.00)"
        Public Property PrevOtherOperatingExpensesDisplay As String
            Get
                Return _prevOtherOperatingExpensesDisplay
            End Get
            Set(value As String)
                SetProperty(_prevOtherOperatingExpensesDisplay, value)
            End Set
        End Property

        Private _prevShrinkageLossDisplay As String = "(₱0.00)"
        Public Property PrevShrinkageLossDisplay As String
            Get
                Return _prevShrinkageLossDisplay
            End Get
            Set(value As String)
                SetProperty(_prevShrinkageLossDisplay, value)
            End Set
        End Property

        Private _prevOperatingExpensesDisplay As String = "(₱0.00)"
        Public Property PrevOperatingExpensesDisplay As String
            Get
                Return _prevOperatingExpensesDisplay
            End Get
            Set(value As String)
                SetProperty(_prevOperatingExpensesDisplay, value)
            End Set
        End Property

        Private _prevNetIncomeDisplay As String = "₱0.00"
        Public Property PrevNetIncomeDisplay As String
            Get
                Return _prevNetIncomeDisplay
            End Get
            Set(value As String)
                SetProperty(_prevNetIncomeDisplay, value)
            End Set
        End Property

        Private _otherOperatingExpensesDisplay As String = "(₱0.00)"
        Public Property OtherOperatingExpensesDisplay As String
            Get
                Return _otherOperatingExpensesDisplay
            End Get
            Set(value As String)
                SetProperty(_otherOperatingExpensesDisplay, value)
            End Set
        End Property

        Private _prevGrossMarginPercentDisplay As String = ""
        Public Property PrevGrossMarginPercentDisplay As String
            Get
                Return _prevGrossMarginPercentDisplay
            End Get
            Set(value As String)
                SetProperty(_prevGrossMarginPercentDisplay, value)
            End Set
        End Property

        Private _prevNetMarginPercentDisplay As String = ""
        Public Property PrevNetMarginPercentDisplay As String
            Get
                Return _prevNetMarginPercentDisplay
            End Get
            Set(value As String)
                SetProperty(_prevNetMarginPercentDisplay, value)
            End Set
        End Property

        Private _prevGrossMarginPercent As Decimal = 0D
        Public Property PrevGrossMarginPercent As Decimal
            Get
                Return _prevGrossMarginPercent
            End Get
            Set(value As Decimal)
                SetProperty(_prevGrossMarginPercent, value)
            End Set
        End Property

        Private _prevNetMarginPercent As Decimal = 0D
        Public Property PrevNetMarginPercent As Decimal
            Get
                Return _prevNetMarginPercent
            End Get
            Set(value As Decimal)
                SetProperty(_prevNetMarginPercent, value)
            End Set
        End Property

        ' ─── Delta Properties ───────────────────────────────────────────────────────

        Private _netSalesDelta As Double
        Public Property NetSalesDelta As Double
            Get
                Return _netSalesDelta
            End Get
            Set(value As Double)
                SetProperty(_netSalesDelta, value)
            End Set
        End Property

        Private _showNetSalesDelta As Boolean
        Public Property ShowNetSalesDelta As Boolean
            Get
                Return _showNetSalesDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showNetSalesDelta, value)
            End Set
        End Property

        Private _cogsDelta As Double
        Public Property COGSDelta As Double
            Get
                Return _cogsDelta
            End Get
            Set(value As Double)
                SetProperty(_cogsDelta, value)
            End Set
        End Property

        Private _showCOGSDelta As Boolean
        Public Property ShowCOGSDelta As Boolean
            Get
                Return _showCOGSDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showCOGSDelta, value)
            End Set
        End Property

        Private _grossProfitDelta As Double
        Public Property GrossProfitDelta As Double
            Get
                Return _grossProfitDelta
            End Get
            Set(value As Double)
                SetProperty(_grossProfitDelta, value)
            End Set
        End Property

        Private _showGrossProfitDelta As Boolean
        Public Property ShowGrossProfitDelta As Boolean
            Get
                Return _showGrossProfitDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showGrossProfitDelta, value)
            End Set
        End Property

        Private _otherOperatingExpensesDelta As Double
        Public Property OtherOperatingExpensesDelta As Double
            Get
                Return _otherOperatingExpensesDelta
            End Get
            Set(value As Double)
                SetProperty(_otherOperatingExpensesDelta, value)
            End Set
        End Property

        Private _showOtherOperatingExpensesDelta As Boolean
        Public Property ShowOtherOperatingExpensesDelta As Boolean
            Get
                Return _showOtherOperatingExpensesDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showOtherOperatingExpensesDelta, value)
            End Set
        End Property

        Private _shrinkageLossDelta As Double
        Public Property ShrinkageLossDelta As Double
            Get
                Return _shrinkageLossDelta
            End Get
            Set(value As Double)
                SetProperty(_shrinkageLossDelta, value)
            End Set
        End Property

        Private _showShrinkageLossDelta As Boolean
        Public Property ShowShrinkageLossDelta As Boolean
            Get
                Return _showShrinkageLossDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showShrinkageLossDelta, value)
            End Set
        End Property

        Private _operatingExpensesDelta As Double
        Public Property OperatingExpensesDelta As Double
            Get
                Return _operatingExpensesDelta
            End Get
            Set(value As Double)
                SetProperty(_operatingExpensesDelta, value)
            End Set
        End Property

        Private _showOperatingExpensesDelta As Boolean
        Public Property ShowOperatingExpensesDelta As Boolean
            Get
                Return _showOperatingExpensesDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showOperatingExpensesDelta, value)
            End Set
        End Property

        Private _netIncomeDelta As Double
        Public Property NetIncomeDelta As Double
            Get
                Return _netIncomeDelta
            End Get
            Set(value As Double)
                SetProperty(_netIncomeDelta, value)
            End Set
        End Property

        Private _showNetIncomeDelta As Boolean
        Public Property ShowNetIncomeDelta As Boolean
            Get
                Return _showNetIncomeDelta
            End Get
            Set(value As Boolean)
                SetProperty(_showNetIncomeDelta, value)
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

        Private _whatThisMeansSeverity As InsightSeverity = InsightSeverity.Info
        Public Property WhatThisMeansSeverity As InsightSeverity
            Get
                Return _whatThisMeansSeverity
            End Get
            Set(value As InsightSeverity)
                SetProperty(_whatThisMeansSeverity, value)
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
                Return ProductMargins.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property LoadCommand As AsyncRelayCommand

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
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
                OtherOperatingExpensesDisplay = FormatDeduction(statement.OperatingExpenses - statement.ShrinkageLoss)

                If prevResult IsNot Nothing Then
                    PrevPeriodLabel = prevResult.PeriodDescription
                    PrevNetSalesDisplay = FormatAmount(prevResult.NetSales)
                    PrevCOGSDisplay = FormatDeduction(prevResult.CostOfGoodsSold)
                    PrevGrossProfitDisplay = FormatAmount(prevResult.GrossProfit)
                    PrevOtherOperatingExpensesDisplay = FormatDeduction(prevResult.OperatingExpenses - prevResult.ShrinkageLoss)
                    PrevShrinkageLossDisplay = FormatDeduction(prevResult.ShrinkageLoss)
                    PrevOperatingExpensesDisplay = FormatDeduction(prevResult.OperatingExpenses)
                    PrevNetIncomeDisplay = FormatAmount(prevResult.NetIncome)
                    PrevGrossMarginPercentDisplay = $"{prevResult.GrossMarginPercent:N1}%"
                    PrevNetMarginPercentDisplay = $"{prevResult.NetMarginPercent:N1}%"
                    PrevGrossMarginPercent = prevResult.GrossMarginPercent
                    PrevNetMarginPercent = prevResult.NetMarginPercent

                    Dim deltaVal As Double = 0.0
                    Dim show As Boolean = False

                    CalculateDelta(statement.NetSales, prevResult.NetSales, deltaVal, show)
                    NetSalesDelta = deltaVal : ShowNetSalesDelta = show

                    CalculateDelta(statement.CostOfGoodsSold, prevResult.CostOfGoodsSold, deltaVal, show)
                    COGSDelta = deltaVal : ShowCOGSDelta = show

                    CalculateDelta(statement.GrossProfit, prevResult.GrossProfit, deltaVal, show)
                    GrossProfitDelta = deltaVal : ShowGrossProfitDelta = show

                    CalculateDelta(statement.OperatingExpenses - statement.ShrinkageLoss, prevResult.OperatingExpenses - prevResult.ShrinkageLoss, deltaVal, show)
                    OtherOperatingExpensesDelta = deltaVal : ShowOtherOperatingExpensesDelta = show

                    CalculateDelta(statement.ShrinkageLoss, prevResult.ShrinkageLoss, deltaVal, show)
                    ShrinkageLossDelta = deltaVal : ShowShrinkageLossDelta = show

                    CalculateDelta(statement.OperatingExpenses, prevResult.OperatingExpenses, deltaVal, show)
                    OperatingExpensesDelta = deltaVal : ShowOperatingExpensesDelta = show

                    CalculateDelta(statement.NetIncome, prevResult.NetIncome, deltaVal, show)
                    NetIncomeDelta = deltaVal : ShowNetIncomeDelta = show
                Else
                    PrevPeriodLabel = "Prior Period"
                    PrevNetSalesDisplay = "₱0.00"
                    PrevCOGSDisplay = "(₱0.00)"
                    PrevGrossProfitDisplay = "₱0.00"
                    PrevOtherOperatingExpensesDisplay = "(₱0.00)"
                    PrevShrinkageLossDisplay = "(₱0.00)"
                    PrevOperatingExpensesDisplay = "(₱0.00)"
                    PrevNetIncomeDisplay = "₱0.00"
                    PrevGrossMarginPercentDisplay = ""
                    PrevNetMarginPercentDisplay = ""
                    PrevGrossMarginPercent = 0D
                    PrevNetMarginPercent = 0D

                    NetSalesDelta = 0.0 : ShowNetSalesDelta = False
                    COGSDelta = 0.0 : ShowCOGSDelta = False
                    GrossProfitDelta = 0.0 : ShowGrossProfitDelta = False
                    OtherOperatingExpensesDelta = 0.0 : ShowOtherOperatingExpensesDelta = False
                    ShrinkageLossDelta = 0.0 : ShowShrinkageLossDelta = False
                    OperatingExpensesDelta = 0.0 : ShowOperatingExpensesDelta = False
                    NetIncomeDelta = 0.0 : ShowNetIncomeDelta = False
                End If

                WhatThisMeansText = _whatThisMeansService.GenerateIncomeStatementInterpretation(statement, previousMargin)
                WhatThisMeansSeverity = _whatThisMeansService.GetIncomeStatementSeverity(statement, previousMargin)

                Dim margins = Await _incomeService.GetPerProductMarginsAsync(statement.StartDate, statement.EndDate)
                Dim sorted = margins.OrderBy(Function(m) m.GrossMarginPercent).ToList()

                ProductMargins.Clear()
                For Each m In sorted
                    ProductMargins.Add(m)
                Next
                IsError = False

            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Formatting Helpers ───────────────────────────────────────────────────

        Private Sub CalculateDelta(currentVal As Decimal, priorVal As Decimal, ByRef deltaValue As Double, ByRef showDelta As Boolean)
            If priorVal = 0D Then
                deltaValue = 0.0
                showDelta = False
            Else
                deltaValue = CDbl(Math.Round(((currentVal - priorVal) / Math.Abs(priorVal)) * 100D, 1))
                showDelta = True
            End If
        End Sub

        Private Shared Function FormatAmount(amount As Decimal) As String
            If amount < 0D Then Return $"(₱{Math.Abs(amount):N2})"
            Return $"₱{amount:N2}"
        End Function

        Private Shared Function FormatDeduction(amount As Decimal) As String
            Return $"(₱{Math.Abs(amount):N2})"
        End Function

    End Class

End Namespace
