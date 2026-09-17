Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports MediatR
Imports Microsoft.Extensions.Options
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Queries
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services.Reporting

    ''' <summary>
    ''' Implementation of <see cref="ITamperReportExporter"/> to export tamper audit entries
    ''' to RFC 4180 compliant CSV files with UTF-8 BOM, or A4 portrait PDF files using QuestPDF.
    ''' </summary>
    Public Class TamperReportExporter
        Implements ITamperReportExporter

        Private Const FallbackFontFamily As String = "Consolas"
        Private Const FallbackFontSize As Double = 9.0

        Private ReadOnly _mediator As IMediator
        Private ReadOnly _options As TamperReportExportOptions

        Public Sub New(mediator As IMediator, options As IOptions(Of TamperReportExportOptions))
            _mediator = mediator
            _options = options.Value
        End Sub

        ''' <summary>
        ''' Exports the list of tamper incidents to the specified file path in the selected format.
        ''' </summary>
        Public Async Function ExportAsync(
            incidents As IReadOnlyList(Of TamperAuditEntry),
            dateFrom As DateTime,
            dateTo As DateTime,
            format As TamperReportFormat,
            targetPath As String,
            cancellationToken As CancellationToken
        ) As Task Implements ITamperReportExporter.ExportAsync

            If String.IsNullOrWhiteSpace(targetPath) Then
                Throw New ArgumentException("Target path cannot be null or empty.", NameOf(targetPath))
            End If

            ' 1. Pre-flight guards: No silent overwrites to preserve tamper evidence
            If File.Exists(targetPath) Then
                Throw New InvalidOperationException($"The target export file already exists at '{targetPath}'. Silent overwrites are disabled to preserve integrity.")
            End If

            ' Create directory if it doesn't exist
            Dim dir = Path.GetDirectoryName(targetPath)
            If Not String.IsNullOrWhiteSpace(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            ' 2. Dispatch based on format
            Select Case format
                Case TamperReportFormat.Csv
                    Await WriteCsvAsync(incidents, targetPath, cancellationToken)
                Case TamperReportFormat.Pdf
                    Await WritePdfAsync(incidents, dateFrom, dateTo, targetPath, cancellationToken)
                Case Else
                    Throw New NotSupportedException($"Export format '{format}' is not supported.")
            End Select
        End Function

        ''' <summary>
        ''' Writes the incidents to a CSV file in accordance with RFC 4180.
        ''' Uses UTF-8 with BOM so Excel automatically recognizes PHP Peso glyphs and other characters.
        ''' </summary>
        Private Function WriteCsvAsync(
            incidents As IReadOnlyList(Of TamperAuditEntry),
            targetPath As String,
            cancellationToken As CancellationToken
        ) As Task
            ' UTF-8 with BOM
            Dim utf8WithBom As New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)

            Using stream As New FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                Using writer As New StreamWriter(stream, utf8WithBom)
                    ' Header Row (14 columns, matching the BIR evidence shape)
                    writer.WriteLine("DetectedAtUtc,DetectedAtLocal,ReceiptId,ReceiptNumber,TamperKind,Severity,DetectedByService,ExpectedHash,ActualHash,MachineName,OperatingUser,AdditionalContextJson,CreatedAtUtc,CreatedBy")

                    For Each entry In incidents
                        cancellationToken.ThrowIfCancellationRequested()

                        Dim detectedAtUtc = entry.DetectedAt.ToString("o")
                        Dim detectedAtLocal = entry.DetectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                        Dim receiptId = entry.ReceiptId.ToString()
                        Dim receiptNumber = If(entry.ReceiptNumber, String.Empty)
                        Dim tamperKind = If(entry.TamperKind, String.Empty)
                        Dim severity = "Critical"
                        Dim detectedByService = If(entry.DetectedByService, String.Empty)
                        Dim expectedHash = If(entry.ExpectedValue, String.Empty)
                        Dim actualHash = If(entry.ActualValue, String.Empty)
                        Dim machineName = If(entry.MachineName, String.Empty)
                        Dim operatingUser = If(entry.OperatingUser, String.Empty)
                        Dim additionalContextJson = If(entry.AdditionalContextJson, String.Empty)
                        Dim createdAtUtc = entry.CreatedAt.ToString("o")
                        Dim createdBy = If(entry.CreatedBy, String.Empty)

                        Dim row = String.Join(",", {
                            EscapeCsvField(detectedAtUtc),
                            EscapeCsvField(detectedAtLocal),
                            EscapeCsvField(receiptId),
                            EscapeCsvField(receiptNumber),
                            EscapeCsvField(tamperKind),
                            EscapeCsvField(severity),
                            EscapeCsvField(detectedByService),
                            EscapeCsvField(expectedHash),
                            EscapeCsvField(actualHash),
                            EscapeCsvField(machineName),
                            EscapeCsvField(operatingUser),
                            EscapeCsvField(additionalContextJson),
                            EscapeCsvField(createdAtUtc),
                            EscapeCsvField(createdBy)
                        })
                        writer.WriteLine(row)
                    Next
                End Using
            End Using

            Return Task.CompletedTask
        End Function

        ''' <summary>
        ''' Escapes a field according to RFC 4180: wraps in double quotes and duplicates internal quotes.
        ''' </summary>
        Private Function EscapeCsvField(value As String) As String
            If value Is Nothing Then Return """"""
            Return $"""{value.Replace("""", """""")}"""
        End Function

        ''' <summary>
        ''' Generates an A4 portrait PDF document of tamper incidents using QuestPDF.
        ''' Business name, address, and TIN are queried from VAT configuration via MediatR if available.
        ''' </summary>
        Private Async Function WritePdfAsync(
            incidents As IReadOnlyList(Of TamperAuditEntry),
            dateFrom As DateTime,
            dateTo As DateTime,
            targetPath As String,
            cancellationToken As CancellationToken
        ) As Task

            ' Query business registration data via MediatR if available
            Dim businessName As String = ""
            Dim businessAddress As String = ""
            Dim businessTin As String = ""

            Try
                Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery(), cancellationToken)
                If vatConfig IsNot Nothing Then
                    businessName = If(vatConfig.BusinessName, String.Empty)
                    businessAddress = If(vatConfig.BusinessAddress, String.Empty)
                    businessTin = If(vatConfig.BusinessTIN, String.Empty)
                End If
            Catch ex As Exception
                ' Modular isolation fallback: write empty headers if POS registration is unavailable
            End Try

            Dim fontFamily = If(String.IsNullOrWhiteSpace(_options.PdfFontFamily), FallbackFontFamily, _options.PdfFontFamily)
            Dim fontSize As Single = CSng(If(_options.PdfFontSizePt <= 0, FallbackFontSize, _options.PdfFontSizePt))

            ' Construct the A4 PDF Document
            ' Using defensive naming conventions (pg, colHeader, colContent, tbl, txtFooter) to avoid type collisions (BC30980)
            Dim doc = Document.Create(
                Sub(container)
                    container.Page(
                        Sub(pg)
                            pg.Size(PageSizes.A4)
                            pg.Margin(30)
                            pg.DefaultTextStyle(Function(s) s.FontFamily(fontFamily).FontSize(fontSize))

                            ' Header Block: Business identifiers + Report title
                            pg.Header().Column(
                                Sub(colHeader)
                                    If Not String.IsNullOrWhiteSpace(businessName) Then
                                        colHeader.Item().Text(businessName).Bold().FontSize(12)
                                    End If
                                    If Not String.IsNullOrWhiteSpace(businessAddress) Then
                                        colHeader.Item().Text(businessAddress).FontSize(8)
                                    End If
                                    If Not String.IsNullOrWhiteSpace(businessTin) Then
                                        colHeader.Item().Text($"TIN: {businessTin}").FontSize(8)
                                    End If

                                    colHeader.Item().PaddingTop(10).Text("TAMPER AUDIT REPORT").Bold().FontSize(14)
                                    colHeader.Item().Text($"Date Range: From {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}").FontSize(9)
                                    colHeader.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Local)").FontSize(8)
                                    colHeader.Item().PaddingBottom(10)
                                End Sub)

                            ' Tabular body with incident details
                            pg.Content().PaddingTop(5).Column(
                                Sub(colContent)
                                    If incidents.Count = 0 Then
                                        colContent.Item().AlignCenter().PaddingTop(20).Text("No tamper incidents detected in the selected period.").Italic()
                                    Else
                                        colContent.Item().Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(3.2) ' Detected At (Local)
                                                        cols.RelativeColumn(2.2) ' Receipt #
                                                        cols.RelativeColumn(2.8) ' Tamper Kind
                                                        cols.RelativeColumn(1.8) ' Severity
                                                        cols.RelativeColumn(4.0) ' Expected Hash (64-char wraps)
                                                        cols.RelativeColumn(4.0) ' Actual Hash (64-char wraps)
                                                        cols.RelativeColumn(2.0) ' Machine
                                                        cols.RelativeColumn(2.0) ' Operator
                                                    End Sub)

                                                ' Table Header Row
                                                tbl.Header(
                                                    Sub(tblHeader)
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Detected (Local)").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Receipt #").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Kind").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Severity").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Expected Hash").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Actual Hash").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Machine").Bold()
                                                        tblHeader.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Operator").Bold()
                                                    End Sub)

                                                ' Data Rows
                                                For Each entry In incidents
                                                    Dim detLocal = entry.DetectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                                                    Dim rNum = If(entry.ReceiptNumber, String.Empty)
                                                    Dim tKind = If(entry.TamperKind, String.Empty)
                                                    Dim severity = "Critical"
                                                    Dim expHash = If(entry.ExpectedValue, String.Empty)
                                                    Dim actHash = If(entry.ActualValue, String.Empty)
                                                    Dim mach = If(entry.MachineName, String.Empty)
                                                    Dim opUser = If(entry.OperatingUser, String.Empty)

                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(detLocal)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(rNum)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tKind)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(severity).FontColor(Colors.Red.Medium)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(expHash)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(actHash)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(mach)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(opUser)
                                                Next
                                            End Sub)
                                    End If
                                End Sub)

                            ' Footer block per page
                            pg.Footer().AlignCenter().PaddingTop(10).Text(
                                Sub(txtFooter)
                                    txtFooter.Span("Generated by MerchSys POS — page ")
                                    txtFooter.CurrentPageNumber()
                                    txtFooter.Span(" of ")
                                    txtFooter.TotalPages()
                                End Sub)
                        End Sub)
                End Sub)

            ' Generate PDF on local disk asynchronously
            Await Task.Run(Sub() doc.GeneratePdf(targetPath), cancellationToken)
        End Function

    End Class

End Namespace
