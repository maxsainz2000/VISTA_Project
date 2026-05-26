Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.Extensions.Options
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.Services.Reporting

Namespace ViewModels

    ''' <summary>
    ''' Display row for the Tamper Audit Report DataGrid.
    ''' Wraps <see cref="TamperAuditEntry"/> with presentation-ready properties,
    ''' including hash truncation for inline display (full value exposed for tooltip).
    ''' </summary>
    Public Class TamperAuditEntryDto

        Public ReadOnly Property DetectedAt As DateTime
        Public ReadOnly Property ReceiptId As Long
        Public ReadOnly Property ReceiptNumber As String
        Public ReadOnly Property ExpectedHash As String
        Public ReadOnly Property ActualHash As String
        Public ReadOnly Property ExpectedHashShort As String
        Public ReadOnly Property ActualHashShort As String
        Public ReadOnly Property Severity As String
        Public ReadOnly Property Details As String

        Public Sub New(entry As TamperAuditEntry)
            DetectedAt = entry.DetectedAt
            ReceiptId = entry.ReceiptId
            ReceiptNumber = If(entry.ReceiptNumber, String.Empty)
            ExpectedHash = If(entry.ExpectedValue, String.Empty)
            ActualHash = If(entry.ActualValue, String.Empty)
            ExpectedHashShort = If(ExpectedHash.Length > 12, ExpectedHash.Substring(0, 12) & "…", ExpectedHash)
            ActualHashShort = If(ActualHash.Length > 12, ActualHash.Substring(0, 12) & "…", ActualHash)
            Severity = "Critical"
            Details = If(entry.TamperKind, String.Empty)
        End Sub

    End Class

    ''' <summary>
    ''' ViewModel for the Tamper Audit Report view.
    ''' Queries <see cref="ITamperAuditQueryService"/> for tamper incidents in a configurable
    ''' date range (default: last 30 days). Read-only — no mutations are performed.
    ''' Supports CSV and PDF report exporting.
    ''' </summary>
    Public Class TamperAuditReportViewModel
        Inherits ObservableObject

        Private ReadOnly _queryService As ITamperAuditQueryService
        Private ReadOnly _exporter As ITamperReportExporter
        Private ReadOnly _options As TamperReportExportOptions

        Private _entries As ObservableCollection(Of TamperAuditEntryDto)
        Public Property Entries As ObservableCollection(Of TamperAuditEntryDto)
            Get
                Return _entries
            End Get
            Private Set(value As ObservableCollection(Of TamperAuditEntryDto))
                SetProperty(_entries, value)
                OnPropertyChanged(NameOf(HasNoEntries))
                OnPropertyChanged(NameOf(HasEntries))
                ExportCsvCommand?.NotifyCanExecuteChanged()
                ExportPdfCommand?.NotifyCanExecuteChanged()
            End Set
        End Property

        Private _dateFrom As DateTime
        Public Property DateFrom As DateTime
            Get
                Return _dateFrom
            End Get
            Set(value As DateTime)
                SetProperty(_dateFrom, value)
            End Set
        End Property

        Private _dateTo As DateTime
        Public Property DateTo As DateTime
            Get
                Return _dateTo
            End Get
            Set(value As DateTime)
                SetProperty(_dateTo, value)
            End Set
        End Property

        Private _isLoading As Boolean
        Public Property IsLoading As Boolean
            Get
                Return _isLoading
            End Get
            Private Set(value As Boolean)
                SetProperty(_isLoading, value)
                ExportCsvCommand?.NotifyCanExecuteChanged()
                ExportPdfCommand?.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property HasNoEntries As Boolean
            Get
                Return _entries IsNot Nothing AndAlso _entries.Count = 0 AndAlso Not IsLoading
            End Get
        End Property

        Public ReadOnly Property HasEntries As Boolean
            Get
                Return _entries IsNot Nothing AndAlso _entries.Count > 0
            End Get
        End Property

        Public ReadOnly Property LoadCommand As AsyncRelayCommand
        Public ReadOnly Property ExportCsvCommand As AsyncRelayCommand(Of String)
        Public ReadOnly Property ExportPdfCommand As AsyncRelayCommand(Of String)

        Public ReadOnly Property Options As TamperReportExportOptions
            Get
                Return _options
            End Get
        End Property

        Public Sub New(queryService As ITamperAuditQueryService, exporter As ITamperReportExporter, options As IOptions(Of TamperReportExportOptions))
            _queryService = queryService
            _exporter = exporter
            _options = options.Value

            Dim today = DateTime.Today
            _dateFrom = today.AddDays(-30)
            _dateTo = today
            _entries = Nothing

            LoadCommand = New AsyncRelayCommand(AddressOf LoadAsync)
            ExportCsvCommand = New AsyncRelayCommand(Of String)(AddressOf ExportCsvAsync, AddressOf CanExport)
            ExportPdfCommand = New AsyncRelayCommand(Of String)(AddressOf ExportPdfAsync, AddressOf CanExport)
        End Sub

        Private Function CanExport(targetPath As String) As Boolean
            Return Not IsLoading AndAlso _entries IsNot Nothing
        End Function

        Public Async Function LoadAsync() As Task
            IsLoading = True
            OnPropertyChanged(NameOf(HasNoEntries))
            OnPropertyChanged(NameOf(HasEntries))

            Dim fromUtc = DateFrom.Date
            Dim toUtc = DateTo.Date.AddDays(1).AddTicks(-1)

            Dim results = Await _queryService.GetIncidentsAsync(fromUtc, toUtc)
            Entries = New ObservableCollection(Of TamperAuditEntryDto)(
                results.Select(Function(e) New TamperAuditEntryDto(e)))

            IsLoading = False
            OnPropertyChanged(NameOf(HasNoEntries))
            OnPropertyChanged(NameOf(HasEntries))
        End Function

        Public Async Function ExportCsvAsync(targetPath As String) As Task
            If String.IsNullOrWhiteSpace(targetPath) Then Return        ' user cancelled
            Dim fromUtc = DateFrom.Date
            Dim toUtc = DateTo.Date.AddDays(1).AddTicks(-1)
            Dim entries = Await _queryService.GetIncidentsAsync(fromUtc, toUtc)
            Await _exporter.ExportAsync(entries, DateFrom, DateTo, TamperReportFormat.Csv, targetPath, CancellationToken.None)
        End Function

        Public Async Function ExportPdfAsync(targetPath As String) As Task
            If String.IsNullOrWhiteSpace(targetPath) Then Return        ' user cancelled
            Dim fromUtc = DateFrom.Date
            Dim toUtc = DateTo.Date.AddDays(1).AddTicks(-1)
            Dim entries = Await _queryService.GetIncidentsAsync(fromUtc, toUtc)
            Await _exporter.ExportAsync(entries, DateFrom, DateTo, TamperReportFormat.Pdf, targetPath, CancellationToken.None)
        End Function

    End Class

End Namespace
