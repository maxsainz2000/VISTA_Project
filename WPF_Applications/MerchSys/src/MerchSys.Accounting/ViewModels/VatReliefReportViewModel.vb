Imports System.Collections.ObjectModel
Imports System.Globalization
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services

Namespace ViewModels

    Public Class VatReliefReportViewModel
        Inherits ObservableObject

        Private ReadOnly _service As IVatReliefReportService
        Private Shared ReadOnly _enPh As New CultureInfo("en-PH")

        Public ReadOnly Property AvailableYears As List(Of Integer)
        Public ReadOnly Property AvailableMonths As List(Of Integer)

        Public ReadOnly Property LoadCommand As AsyncRelayCommand
        Public ReadOnly Property RefreshCommand As AsyncRelayCommand

        Public Sub New(service As IVatReliefReportService)
            _service = service

            AvailableYears = Enumerable.Range(2025, DateTime.Now.Year - 2024).Reverse().ToList()
            AvailableMonths = Enumerable.Range(1, 12).ToList()
            _selectedYear = DateTime.Now.Year
            _selectedMonth = DateTime.Now.Month

            LoadCommand = New AsyncRelayCommand(AddressOf LoadAsync)
            RefreshCommand = New AsyncRelayCommand(AddressOf LoadAsync)
            TrailingMonths = New ObservableCollection(Of VatReliefSummary)()
        End Sub

        ' ─── Period Selectors ────────────────────────────────────────────────────────

        Private _selectedYear As Integer
        Public Property SelectedYear As Integer
            Get
                Return _selectedYear
            End Get
            Set(value As Integer)
                SetProperty(_selectedYear, value)
            End Set
        End Property

        Private _selectedMonth As Integer
        Public Property SelectedMonth As Integer
            Get
                Return _selectedMonth
            End Get
            Set(value As Integer)
                SetProperty(_selectedMonth, value)
            End Set
        End Property

        ' ─── Data ────────────────────────────────────────────────────────────────────

        Private _summary As VatReliefSummary
        Public Property Summary As VatReliefSummary
            Get
                Return _summary
            End Get
            Set(value As VatReliefSummary)
                If SetProperty(_summary, value) Then
                    OnPropertyChanged(NameOf(NetVatPayableColor))
                End If
            End Set
        End Property

        Public ReadOnly Property TrailingMonths As ObservableCollection(Of VatReliefSummary)

        Private _isLoading As Boolean
        Public Property IsLoading As Boolean
            Get
                Return _isLoading
            End Get
            Set(value As Boolean)
                SetProperty(_isLoading, value)
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
                Return TrailingMonths.Count = 0 AndAlso Not IsLoading AndAlso Not IsError
            End Get
        End Property

        Private _whatThisMeans As String = "Select a year and month, then click Refresh to load the VAT Relief summary."
        Public Property WhatThisMeans As String
            Get
                Return _whatThisMeans
            End Get
            Set(value As String)
                SetProperty(_whatThisMeans, value)
            End Set
        End Property

        ''' <summary>Colour token for the NetVatPayable banner (Red / Green / Gray).</summary>
        Public ReadOnly Property NetVatPayableColor As String
            Get
                If _summary Is Nothing Then Return "Gray"
                If _summary.NetVatPayable > 0D Then Return "Red"
                If _summary.NetVatPayable < 0D Then Return "Green"
                Return "Gray"
            End Get
        End Property

        ' ─── Commands ────────────────────────────────────────────────────────────────

        Private Async Function LoadAsync() As Task
            IsError = False
            IsLoading = True
            Try
                Dim s = Await _service.GetMonthlySummaryAsync(_selectedYear, _selectedMonth)
                Summary = s

                Dim trailing = Await _service.GetTrailingMonthsAsync(_selectedYear, _selectedMonth, 12)
                TrailingMonths.Clear()
                For Each item In trailing
                    TrailingMonths.Add(item)
                Next

                WhatThisMeans = BuildWhatThisMeans(s)
                IsError = False
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsLoading = False
            End Try
        End Function

        Private Function BuildWhatThisMeans(s As VatReliefSummary) As String
            Dim monthName = New DateTime(s.Year, s.Month, 1).ToString("MMMM", _enPh)
            If s.NetVatPayable > 0D Then
                Return $"You owe {s.NetVatPayable.ToString("C2", _enPh)} in net VAT for {monthName} {s.Year}. " &
                       "This is what you'll remit when you file Form 2550M."
            ElseIf s.NetVatPayable = 0D Then
                Return $"Your output VAT matched your input VAT for {monthName} {s.Year}. " &
                       "Nothing to remit, nothing to carry forward."
            Else
                Return $"Your input VAT exceeded output VAT by {Math.Abs(s.NetVatPayable).ToString("C2", _enPh)} " &
                       $"for {monthName} {s.Year}. This is a VAT credit that carries forward to next month."
            End If
        End Function

    End Class

End Namespace
