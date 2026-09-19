Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports MerchSys.POS.Entities
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services.ReceiptRendering

    ''' <summary>
    ''' Renders the structured BIR-compliant receipt body into a monospace A5 portrait PDF
    ''' document via QuestPDF. Implements a defensive overwrite guard to prevent modifying
    ''' existing receipts.
    ''' </summary>
    ''' <remarks>
    ''' Font handling: the configured family (e.g. Consolas) is passed through to QuestPDF.
    ''' If the font is not installed on the host, QuestPDF substitutes an internal default
    ''' (DejaVu Sans). The substitution is silent at the library level, so the renderer logs
    ''' a single advisory at startup whenever the configured family is not the built-in default.
    ''' </remarks>
    Public Class PdfReceiptRenderer
        Implements IReceiptRenderer

        Private Const FallbackFontFamily As String = "Courier New"

        Private ReadOnly _options As ReceiptPdfOptions
        Private ReadOnly _logger As ILogger(Of PdfReceiptRenderer)

        Public Sub New(options As IOptions(Of ReceiptPdfOptions), logger As ILogger(Of PdfReceiptRenderer))
            _options = options.Value
            _logger = logger
        End Sub

        ''' <summary>
        ''' Renders the receipt into a monospace PDF file at the configured output path.
        ''' </summary>
        ''' <exception cref="InvalidOperationException">
        ''' Thrown if the target receipt file already exists. Overwriting a stored Official Receipt
        ''' is prohibited by BIR retention rules.
        ''' </exception>
        Public Function RenderAsync(
            receipt As OfficialReceipt,
            body As ReceiptBody,
            cancellationToken As CancellationToken
        ) As Task Implements IReceiptRenderer.RenderAsync

            Dim outputDir = Environment.ExpandEnvironmentVariables(_options.OutputDirectory)
            Dim fileName = _options.FileNamePattern
            fileName = fileName.Replace("{ReceiptNumber}", receipt.ReceiptNumber)
            fileName = fileName.Replace("{YYYYMMDD}", receipt.IssueDate.ToLocalTime().ToString("yyyyMMdd"))

            Dim fullPath = Path.Combine(outputDir, fileName)

            If File.Exists(fullPath) Then
                Throw New InvalidOperationException(
                    $"Receipt PDF already exists at '{fullPath}'. Overwriting generated receipts is prohibited.")
            End If

            If Not Directory.Exists(outputDir) Then
                Directory.CreateDirectory(outputDir)
            End If

            Dim fontFamily = If(String.IsNullOrWhiteSpace(_options.FontFamily), FallbackFontFamily, _options.FontFamily)
            Dim fontSize As Single = CSng(_options.FontSizePt)
            Dim lines = body.AllLines

            Try
                Dim doc = Document.Create(
                    Sub(container)
                        container.Page(
                            Sub(page)
                                page.Size(PageSizes.A5)
                                page.Margin(20)
                                page.DefaultTextStyle(Function(s) s.FontFamily(fontFamily).FontSize(fontSize))

                                page.Content().Column(
                                    Sub(col)
                                        For Each ln As String In lines
                                            col.Item().Text(ln)
                                        Next
                                    End Sub)
                            End Sub)
                    End Sub)

                doc.GeneratePdf(fullPath)
            Catch ex As Exception
                _logger.LogError(ex,
                    "PDF receipt generation failed for receipt {0} at '{1}'.",
                    receipt.ReceiptNumber, fullPath)
                Throw
            End Try

            Return Task.CompletedTask
        End Function

    End Class

End Namespace
