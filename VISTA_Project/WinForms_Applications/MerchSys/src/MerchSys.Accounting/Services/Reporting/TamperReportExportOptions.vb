Namespace Services.Reporting

    ''' <summary>
    ''' Strongly-typed configuration options for the Tamper Incident Report exports.
    ''' Bound from Accounting:TamperReport:Export in appsettings.json.
    ''' </summary>
    Public Class TamperReportExportOptions
        Public Property DefaultDirectory As String = "%LOCALAPPDATA%\MerchSys\TamperReports"
        Public Property CsvFileNamePattern As String = "TamperReport-{YYYYMMDD-HHmm}.csv"
        Public Property PdfFileNamePattern As String = "TamperReport-{YYYYMMDD-HHmm}.pdf"
        Public Property PdfFontFamily As String = "Consolas"
        Public Property PdfFontSizePt As Double = 9.0
    End Class

End Namespace
