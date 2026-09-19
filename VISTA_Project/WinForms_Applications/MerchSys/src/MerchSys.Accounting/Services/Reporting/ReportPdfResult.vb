Namespace Services.Reporting

    ''' <summary>
    ''' Carries the generated PDF bytes and per-page PNG preview images between the
    ''' Accounting exporter services and the shell <c>ReportPreviewWindow</c>.
    ''' </summary>
    Public Class ReportPdfResult

        ''' <summary>The complete PDF document as a byte array.</summary>
        Public Property PdfBytes As Byte()

        ''' <summary>One PNG image per page, suitable for WPF <c>BitmapImage</c> binding.</summary>
        Public Property PageImages As IReadOnlyList(Of Byte())

        ''' <summary>Suggested file name shown in the SaveFileDialog (e.g. IncomeStatement-20260610-2335.pdf).</summary>
        Public Property SuggestedFileName As String

    End Class

End Namespace
