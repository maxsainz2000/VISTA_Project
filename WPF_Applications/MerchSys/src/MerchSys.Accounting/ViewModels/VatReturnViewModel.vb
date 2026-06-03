Imports System.Collections.ObjectModel
Imports System.IO
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Enums
Imports MerchSys.Accounting.Exceptions
Imports MerchSys.Accounting.Services
Imports MerchSys.SharedKernel.Interfaces

Namespace ViewModels

    ''' <summary>Row class bound to the VAT return lines DataGrid.</summary>
    Public Class VatReturnLineRow
        Public Property SourceDescription As String
        Public Property TransactionDate As DateTime
        Public Property VatableAmount As Decimal
        Public Property VatExemptAmount As Decimal
        Public Property ZeroRatedAmount As Decimal
        Public Property OutputVat As Decimal
        Public Property InputVat As Decimal
        Public Property TreatmentDisplay As String
    End Class

    ''' <summary>
    ''' ViewModel for the BIR VAT Return view (ACC-11).
    ''' Drives Form 2550M (monthly VAT), Form 2550Q (quarterly VAT),
    ''' and Form 2551Q (quarterly 3% percentage tax) generation,
    ''' filing, amendment, and export workflows.
    ''' Manager-only: Owner role cannot navigate to <c>VatReturnView</c>.
    ''' </summary>
    Public Class VatReturnViewModel
        Inherits ObservableObject

        Private ReadOnly _reportingService As IVatReportingService
        Private ReadOnly _exporter As IVatReturnExporter
        Private ReadOnly _session As ISessionService

        Public Event ExportReady(sender As Object, e As ExportReadyEventArgs)

        Public Sub New(reportingService As IVatReportingService,
                       exporter As IVatReturnExporter,
                       session As ISessionService)
            _reportingService = reportingService
            _exporter = exporter
            _session = session

            Lines = New ObservableCollection(Of VatReturnLineRow)()
            AvailableYears = Enumerable.Range(2025, DateTime.Now.Year - 2024).Reverse().ToList()
            AvailableMonths = Enumerable.Range(1, 12).ToList()
            AvailableQuarters = Enumerable.Range(1, 4).ToList()
            AvailableFormTypes = New List(Of VatReturnFormType) From {
                VatReturnFormType.Form2550M,
                VatReturnFormType.Form2550Q,
                VatReturnFormType.Form2551Q
            }

            _selectedYear = DateTime.Now.Year
            _selectedPeriod = DateTime.Now.Month
            _selectedFormType = VatReturnFormType.Form2550M

            GenerateCommand = New AsyncRelayCommand(AddressOf GenerateAsync)
            FileCommand = New AsyncRelayCommand(AddressOf FileAsync, AddressOf CanFile)
            AmendCommand = New AsyncRelayCommand(AddressOf AmendAsync, AddressOf CanAmend)
            ExportCsvCommand = New AsyncRelayCommand(AddressOf ExportCsvAsync, AddressOf CanExport)
            ExportPdfCommand = New AsyncRelayCommand(AddressOf ExportPdfAsync, AddressOf CanExport)
        End Sub

        ' ─── Period Selectors ────────────────────────────────────────────────────────

        Public ReadOnly Property AvailableYears As List(Of Integer)
        Public ReadOnly Property AvailableMonths As List(Of Integer)
        Public ReadOnly Property AvailableQuarters As List(Of Integer)
        Public ReadOnly Property AvailableFormTypes As List(Of VatReturnFormType)

        Private _selectedYear As Integer
        Public Property SelectedYear As Integer
            Get
                Return _selectedYear
            End Get
            Set(value As Integer)
                SetProperty(_selectedYear, value)
            End Set
        End Property

        Private _selectedPeriod As Integer
        Public Property SelectedPeriod As Integer
            Get
                Return _selectedPeriod
            End Get
            Set(value As Integer)
                SetProperty(_selectedPeriod, value)
            End Set
        End Property

        Private _selectedFormType As VatReturnFormType
        Public Property SelectedFormType As VatReturnFormType
            Get
                Return _selectedFormType
            End Get
            Set(value As VatReturnFormType)
                If SetProperty(_selectedFormType, value) Then
                    OnPropertyChanged(NameOf(IsMonthly))
                    OnPropertyChanged(NameOf(AvailablePeriods))
                    OnPropertyChanged(NameOf(IsForm2550M))
                    OnPropertyChanged(NameOf(IsForm2550Q))
                    OnPropertyChanged(NameOf(IsForm2551Q))
                    SelectedPeriod = 1
                End If
            End Set
        End Property

        Public ReadOnly Property IsMonthly As Boolean
            Get
                Return _selectedFormType = VatReturnFormType.Form2550M
            End Get
        End Property

        Public Property IsForm2550M As Boolean
            Get
                Return _selectedFormType = VatReturnFormType.Form2550M
            End Get
            Set(value As Boolean)
                If value Then SelectedFormType = VatReturnFormType.Form2550M
            End Set
        End Property

        Public Property IsForm2550Q As Boolean
            Get
                Return _selectedFormType = VatReturnFormType.Form2550Q
            End Get
            Set(value As Boolean)
                If value Then SelectedFormType = VatReturnFormType.Form2550Q
            End Set
        End Property

        Public Property IsForm2551Q As Boolean
            Get
                Return _selectedFormType = VatReturnFormType.Form2551Q
            End Get
            Set(value As Boolean)
                If value Then SelectedFormType = VatReturnFormType.Form2551Q
            End Set
        End Property

        Public ReadOnly Property AvailablePeriods As List(Of Integer)
            Get
                Return If(IsMonthly, AvailableMonths, AvailableQuarters)
            End Get
        End Property

        ' ─── Financial Totals ────────────────────────────────────────────────────────

        Private _currentReturnId As Integer?
        Public ReadOnly Property CurrentReturnId As Integer?
            Get
                Return _currentReturnId
            End Get
        End Property

        Private _filingStatus As VatFilingStatus
        Public Property FilingStatus As VatFilingStatus
            Get
                Return _filingStatus
            End Get
            Set(value As VatFilingStatus)
                If SetProperty(_filingStatus, value) Then
                    FileCommand.NotifyCanExecuteChanged()
                    AmendCommand.NotifyCanExecuteChanged()
                    ExportCsvCommand.NotifyCanExecuteChanged()
                    ExportPdfCommand.NotifyCanExecuteChanged()
                    OnPropertyChanged(NameOf(FilingStatusDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property FilingStatusDisplay As String
            Get
                Select Case _filingStatus
                    Case VatFilingStatus.Generated : Return "Generated — not yet filed with BIR"
                    Case VatFilingStatus.Filed : Return "Filed with BIR"
                    Case VatFilingStatus.Amended : Return "Amended — not yet filed with BIR"
                    Case Else : Return "—"
                End Select
            End Get
        End Property

        Private _totalVatableSales As Decimal
        Public Property TotalVatableSales As Decimal
            Get
                Return _totalVatableSales
            End Get
            Set(value As Decimal)
                SetProperty(_totalVatableSales, value)
            End Set
        End Property

        Private _totalVatExemptSales As Decimal
        Public Property TotalVatExemptSales As Decimal
            Get
                Return _totalVatExemptSales
            End Get
            Set(value As Decimal)
                SetProperty(_totalVatExemptSales, value)
            End Set
        End Property

        Private _totalZeroRatedSales As Decimal
        Public Property TotalZeroRatedSales As Decimal
            Get
                Return _totalZeroRatedSales
            End Get
            Set(value As Decimal)
                SetProperty(_totalZeroRatedSales, value)
            End Set
        End Property

        Private _totalOutputVat As Decimal
        Public Property TotalOutputVat As Decimal
            Get
                Return _totalOutputVat
            End Get
            Set(value As Decimal)
                SetProperty(_totalOutputVat, value)
            End Set
        End Property

        Private _totalVatablePurchases As Decimal
        Public Property TotalVatablePurchases As Decimal
            Get
                Return _totalVatablePurchases
            End Get
            Set(value As Decimal)
                SetProperty(_totalVatablePurchases, value)
            End Set
        End Property

        Private _totalInputVat As Decimal
        Public Property TotalInputVat As Decimal
            Get
                Return _totalInputVat
            End Get
            Set(value As Decimal)
                SetProperty(_totalInputVat, value)
            End Set
        End Property

        Private _vatPayable As Decimal
        Public Property VatPayable As Decimal
            Get
                Return _vatPayable
            End Get
            Set(value As Decimal)
                If SetProperty(_vatPayable, value) Then
                    OnPropertyChanged(NameOf(VatPayableDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property VatPayableDisplay As String
            Get
                If _vatPayable >= 0 Then
                    Return $"₱{_vatPayable:N2} owed to BIR"
                End If
                Return $"₱{Math.Abs(_vatPayable):N2} credit (carry forward)"
            End Get
        End Property

        Public ReadOnly Property Lines As ObservableCollection(Of VatReturnLineRow)

        ' ─── Status & Feedback ──────────────────────────────────────────────────────

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
                Return Lines.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Private _statusMessage As String
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Private _whatThisMeansText As String = "Select a period and click Generate to compute a VAT return."
        Public Property WhatThisMeansText As String
            Get
                Return _whatThisMeansText
            End Get
            Set(value As String)
                SetProperty(_whatThisMeansText, value)
            End Set
        End Property

        ' ─── Commands ────────────────────────────────────────────────────────────────

        Public ReadOnly Property GenerateCommand As AsyncRelayCommand
        Public ReadOnly Property FileCommand As AsyncRelayCommand
        Public ReadOnly Property AmendCommand As AsyncRelayCommand
        Public ReadOnly Property ExportCsvCommand As AsyncRelayCommand
        Public ReadOnly Property ExportPdfCommand As AsyncRelayCommand

        Private Async Function GenerateAsync() As Task
            IsError = False
            IsBusy = True
            StatusMessage = String.Empty
            Dim capturedError As String = Nothing
            Try
                Dim vatReturn As VatReturn
                Select Case _selectedFormType
                    Case VatReturnFormType.Form2550M
                        vatReturn = Await _reportingService.GenerateMonthlyVatReturnAsync(_selectedYear, _selectedPeriod)
                    Case VatReturnFormType.Form2550Q
                        vatReturn = Await _reportingService.GenerateQuarterlyVatReturnAsync(_selectedYear, _selectedPeriod)
                    Case Else
                        vatReturn = Await _reportingService.GenerateNonVatPercentageTaxAsync(_selectedYear, _selectedPeriod)
                End Select
                PopulateFromReturn(vatReturn)
                IsError = False
            Catch ex As VatReturnLockedException
                capturedError = $"This return is filed with BIR. Use Amend to correct it. ({ex.Message})"
            Catch ex As Exception
                capturedError = $"Error: {ex.Message}"
            Finally
                IsBusy = False
            End Try
            If capturedError IsNot Nothing Then
                StatusMessage = capturedError
                ErrorMessage = capturedError
                IsError = True
            End If
        End Function

        Private Async Function FileAsync() As Task
            If Not _currentReturnId.HasValue Then Return
            IsBusy = True
            Dim capturedError As String = Nothing
            Try
                Await _reportingService.FileReturnAsync(_currentReturnId.Value, _session.CurrentUsername)
                FilingStatus = VatFilingStatus.Filed
                StatusMessage = "Return filed successfully with BIR."
                UpdateWhatThisMeans()
            Catch ex As Exception
                capturedError = $"Error filing return: {ex.Message}"
            Finally
                IsBusy = False
            End Try
            If capturedError IsNot Nothing Then
                StatusMessage = capturedError
            End If
        End Function

        Private Async Function AmendAsync() As Task
            If Not _currentReturnId.HasValue Then Return
            IsBusy = True
            Dim capturedError As String = Nothing
            Try
                Dim amended = Await _reportingService.AmendReturnAsync(_currentReturnId.Value)
                PopulateFromReturn(amended)
                StatusMessage = "Amendment created. Review the figures and file when ready."
            Catch ex As Exception
                capturedError = $"Error amending return: {ex.Message}"
            Finally
                IsBusy = False
            End Try
            If capturedError IsNot Nothing Then
                StatusMessage = capturedError
            End If
        End Function

        Private Async Function ExportCsvAsync() As Task
            If Not _currentReturnId.HasValue Then Return
            IsBusy = True
            Dim capturedError As String = Nothing
            Try
                Dim stream = Await _exporter.ExportCsvAsync(_currentReturnId.Value)
                Dim fileName = $"VATReturn_{_selectedFormType}_{_selectedYear}_P{_selectedPeriod}.csv"
                RaiseEvent ExportReady(Me, New ExportReadyEventArgs With {.FileName = fileName, .Data = stream, .Filter = "CSV files (*.csv)|*.csv"})
            Catch ex As Exception
                capturedError = $"Export error: {ex.Message}"
            Finally
                IsBusy = False
            End Try
            If capturedError IsNot Nothing Then
                StatusMessage = capturedError
            End If
        End Function

        Private Async Function ExportPdfAsync() As Task
            If Not _currentReturnId.HasValue Then Return
            IsBusy = True
            Dim capturedError As String = Nothing
            Try
                Dim stream = Await _exporter.ExportPdfAsync(_currentReturnId.Value)
                Dim fileName = $"VATReturn_{_selectedFormType}_{_selectedYear}_P{_selectedPeriod}.pdf.txt"
                RaiseEvent ExportReady(Me, New ExportReadyEventArgs With {.FileName = fileName, .Data = stream, .Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"})
            Catch ex As Exception
                capturedError = $"Export error: {ex.Message}"
            Finally
                IsBusy = False
            End Try
            If capturedError IsNot Nothing Then
                StatusMessage = capturedError
            End If
        End Function

        Private Function CanFile() As Boolean
            Return _currentReturnId.HasValue AndAlso _filingStatus = VatFilingStatus.Generated
        End Function

        Private Function CanAmend() As Boolean
            Return _currentReturnId.HasValue AndAlso _filingStatus = VatFilingStatus.Filed
        End Function

        Private Function CanExport() As Boolean
            Return _currentReturnId.HasValue
        End Function

        ' ─── Helpers ────────────────────────────────────────────────────────────────

        Private Sub PopulateFromReturn(vatReturn As VatReturn)
            _currentReturnId = vatReturn.Id
            FilingStatus = vatReturn.FilingStatus
            TotalVatableSales = vatReturn.TotalVatableSales
            TotalVatExemptSales = vatReturn.TotalVatExemptSales
            TotalZeroRatedSales = vatReturn.TotalZeroRatedSales
            TotalOutputVat = vatReturn.TotalOutputVat
            TotalVatablePurchases = vatReturn.TotalVatablePurchases
            TotalInputVat = vatReturn.TotalInputVat
            VatPayable = vatReturn.VatPayable

            Lines.Clear()
            If vatReturn.Lines IsNot Nothing Then
                For Each line In vatReturn.Lines
                    Lines.Add(New VatReturnLineRow With {
                        .SourceDescription = $"{line.SourceModule} — {line.SourceTable} #{line.SourceRowId}",
                        .TransactionDate = line.TransactionDate,
                        .VatableAmount = line.VatableAmount,
                        .VatExemptAmount = line.VatExemptAmount,
                        .ZeroRatedAmount = line.ZeroRatedAmount,
                        .OutputVat = line.OutputVat,
                        .InputVat = line.InputVat,
                        .TreatmentDisplay = line.Treatment.ToString()
                    })
                Next
            End If

            UpdateWhatThisMeans()
            StatusMessage = $"Return generated: {If(vatReturn.Lines?.Count, 0)} line(s) included."
        End Sub

        Private Sub UpdateWhatThisMeans()
            If Not _currentReturnId.HasValue Then
                WhatThisMeansText = "Select a period and click Generate to compute a VAT return."
                Return
            End If

            Dim periodDesc = If(IsMonthly,
                $"Month {_selectedPeriod}, {_selectedYear}",
                $"Q{_selectedPeriod} {_selectedYear}")

            Dim deadlineText As String
            If _selectedFormType = VatReturnFormType.Form2550M Then
                Dim followingMonth = New DateTime(_selectedYear, _selectedPeriod, 1).AddMonths(1)
                deadlineText = $"{followingMonth:MMMM} 20, {followingMonth.Year} (manual) / {followingMonth:MMMM} 25, {followingMonth.Year} (eFPS)"
            Else
                Dim quarterEndMonth = _selectedPeriod * 3
                Dim filingYear = _selectedYear
                If quarterEndMonth = 12 Then
                    filingYear += 1
                    quarterEndMonth = 1
                Else
                    quarterEndMonth += 1
                End If
                deadlineText = New DateTime(filingYear, quarterEndMonth, 25).ToString("MMMM d, yyyy")
            End If

            Dim vatDesc As String
            If _vatPayable > 0 Then
                vatDesc = $"You owe ₱{_vatPayable:N2} to BIR for {periodDesc}."
            ElseIf _vatPayable < 0 Then
                vatDesc = $"You have a VAT input credit of ₱{Math.Abs(_vatPayable):N2} for {periodDesc}. This will be carried forward to the next period."
            Else
                vatDesc = $"No VAT due for {periodDesc}."
            End If

            WhatThisMeansText = $"{vatDesc} Filing deadline: {deadlineText}."
        End Sub

    End Class

    ''' <summary>Carries export stream and file dialog parameters from ViewModel to View.</summary>
    Public Class ExportReadyEventArgs
        Public Property FileName As String
        Public Property Data As Stream
        Public Property Filter As String
    End Class

End Namespace
