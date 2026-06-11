Imports MerchSys.Accounting.ViewModels

Namespace Services.Reporting

    ''' <summary>
    ''' Generates an A4 portrait PDF of the Income Statement (P&amp;L) report using QuestPDF,
    ''' returning both the PDF bytes and per-page preview images.
    ''' </summary>
    Public Interface IIncomeStatementPdfExporter

        ''' <summary>
        ''' Builds the PDF from the current ViewModel state and returns a <see cref="ReportPdfResult"/>
        ''' containing the PDF bytes and page images for preview.
        ''' </summary>
        Function GenerateAsync(vm As IncomeStatementViewModel) As Task(Of ReportPdfResult)

    End Interface

End Namespace
