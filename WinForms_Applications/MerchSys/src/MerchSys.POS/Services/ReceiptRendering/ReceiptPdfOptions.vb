Namespace Services.ReceiptRendering

    ''' <summary>
    ''' Strongly-typed options for the PDF receipt renderer.
    ''' </summary>
    Public Class ReceiptPdfOptions
        Public Property OutputDirectory As String = "%LOCALAPPDATA%\MerchSys\Receipts"
        Public Property FileNamePattern As String = "OR-{ReceiptNumber}-{YYYYMMDD}.pdf"
        Public Property FontFamily As String = "Consolas"
        Public Property FontSizePt As Double = 9.0
    End Class

End Namespace
