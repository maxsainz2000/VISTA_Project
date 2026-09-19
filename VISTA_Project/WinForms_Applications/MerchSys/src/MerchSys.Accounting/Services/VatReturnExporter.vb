Imports System.IO
Imports System.Reflection
Imports System.Text
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Enums
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services

    ''' <summary>
    ''' Produces CSV and plain-text PDF exports of a <c>VatReturn</c> in BIR cell-for-cell layout.
    ''' CSV line numbers: Form 2550M lines 1–23, Form 2550Q lines 1–28, Form 2551Q lines 1–14.
    ''' PDF is rendered as a human-readable <c>.pdf.txt</c> substitute; no external PDF library
    ''' is required.  Template files are read from embedded resources in the Accounting assembly
    ''' (<c>MerchSys.Accounting.Reports.Templates.*</c>).
    ''' </summary>
    Public Interface IVatReturnExporter

        ''' <summary>Returns a <see cref="Stream"/> containing the CSV export for the specified return.</summary>
        Function ExportCsvAsync(returnId As Integer) As Task(Of Stream)

        ''' <summary>
        ''' Returns a <see cref="Stream"/> containing a human-readable plain-text PDF substitute
        ''' based on the BIR form template.  The caller should save this with a <c>.pdf.txt</c>
        ''' extension and note the limitation to the user.
        ''' </summary>
        Function ExportPdfAsync(returnId As Integer) As Task(Of Stream)

    End Interface

    ''' <summary>
    ''' Implementation of <see cref="IVatReturnExporter"/>.
    ''' Reads <see cref="MerchSys.Accounting.Data.AccountingDbContext"/> for return data and
    ''' applies <c>{{Placeholder}}</c> substitution over the embedded template text.
    ''' </summary>
    Public Class VatReturnExporter
        Implements IVatReturnExporter

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of VatReturnExporter)
        Private ReadOnly _assembly As Assembly = GetType(VatReturnExporter).Assembly

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of VatReturnExporter))
            _db = db
            _logger = logger
        End Sub

        Public Async Function ExportCsvAsync(returnId As Integer) As Task(Of Stream) _
            Implements IVatReturnExporter.ExportCsvAsync

            Dim vatReturn = Await _db.VatReturns.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = returnId)

            If vatReturn Is Nothing Then
                Throw New InvalidOperationException($"VAT return #{returnId} not found.")
            End If

            Dim csv = BuildCsv(vatReturn)
            Dim bytes = Encoding.UTF8.GetBytes(csv)
            Return New MemoryStream(bytes)
        End Function

        Public Async Function ExportPdfAsync(returnId As Integer) As Task(Of Stream) _
            Implements IVatReturnExporter.ExportPdfAsync

            Dim vatReturn = Await _db.VatReturns.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = returnId)

            If vatReturn Is Nothing Then
                Throw New InvalidOperationException($"VAT return #{returnId} not found.")
            End If

            Dim csvLines = BuildCsvLineItems(vatReturn)
            Dim periodDesc = PeriodDescription(vatReturn)
            Dim deadline = FilingDeadlineText(vatReturn)
            Dim whatMeans = WhatThisMeansText(vatReturn)

            Dim doc = Document.Create(
                Sub(container)
                    container.Page(
                        Sub(pg)
                            pg.Size(PageSizes.A4)
                            pg.Margin(30)
                            pg.DefaultTextStyle(Function(s) s.FontFamily("Consolas").FontSize(9))

                            pg.Header().Column(
                                Sub(col)
                                    col.Item().Text("Villon Farm Supply").Bold().FontSize(12)
                                    col.Item().PaddingTop(10).Text($"BIR {vatReturn.FormType} — VAT RETURN").Bold().FontSize(14)
                                    col.Item().Text(periodDesc).FontSize(10)
                                    col.Item().Text($"Filing Status: {vatReturn.FilingStatus}").FontSize(9)
                                    col.Item().Text($"Generated: {vatReturn.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8)
                                    col.Item().PaddingBottom(10)
                                End Sub)

                            pg.Content().PaddingTop(5).Column(
                                Sub(col)
                                    col.Item().Table(
                                        Sub(tbl)
                                            tbl.ColumnsDefinition(
                                                Sub(cols)
                                                    cols.RelativeColumn(1)   ' Line No
                                                    cols.RelativeColumn(6)   ' Description
                                                    cols.RelativeColumn(2.5) ' Amount
                                                End Sub)

                                            tbl.Header(
                                                Sub(h)
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Line").Bold()
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Description").Bold()
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Amount (₱)").Bold()
                                                End Sub)

                                            For Each item In csvLines
                                                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.LineNo)
                                                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Description)
                                                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(item.Amount)
                                            Next
                                        End Sub)

                                    ' Filing deadline
                                    col.Item().PaddingTop(15).Text($"Filing Deadline: {deadline}").FontSize(9)

                                    ' What this means
                                    col.Item().PaddingTop(10).Text("INTERPRETATION").Bold().FontSize(10)
                                    col.Item().PaddingTop(4).Text(whatMeans).FontSize(9)
                                End Sub)

                            pg.Footer().AlignCenter().PaddingTop(10).Text(
                                Sub(txt)
                                    txt.Span("Villon Farm Supply — VISTA VAT Return — page ")
                                    txt.CurrentPageNumber()
                                    txt.Span(" of ")
                                    txt.TotalPages()
                                End Sub)
                        End Sub)
                End Sub)

            Dim ms As New MemoryStream()
            Await Task.Run(Sub() doc.GeneratePdf(ms))
            ms.Position = 0
            Return ms
        End Function

        ' ─── CSV Builder ────────────────────────────────────────────────────────────

        Private Shared Function BuildCsv(vatReturn As Entities.VatReturn) As String
            Dim sb As New StringBuilder()
            sb.AppendLine("Line No.,Description,Amount (PHP)")

            Dim totalGross = vatReturn.TotalVatableSales + vatReturn.TotalVatExemptSales + vatReturn.TotalZeroRatedSales

            Select Case vatReturn.FormType
                Case VatReturnFormType.Form2550M
                    sb.AppendLine($"1,""Taxable Sales/Receipts (Net of VAT)"",{vatReturn.TotalVatableSales:F2}")
                    sb.AppendLine($"2,""Zero-Rated Sales/Receipts"",{vatReturn.TotalZeroRatedSales:F2}")
                    sb.AppendLine($"3,""Exempt Sales/Receipts"",{vatReturn.TotalVatExemptSales:F2}")
                    sb.AppendLine($"4,""Total Gross Sales/Receipts"",{totalGross:F2}")
                    sb.AppendLine($"5,""Output Tax (12% of Line 1)"",{vatReturn.TotalOutputVat:F2}")
                    sb.AppendLine("6,""Adjustments to Output Tax"",0.00")
                    sb.AppendLine($"7,""Total Output Tax"",{vatReturn.TotalOutputVat:F2}")
                    sb.AppendLine("8,""Input Tax Carried Over from Previous Period"",0.00")
                    sb.AppendLine($"9,""Input Tax on Domestic Purchases"",{vatReturn.TotalInputVat:F2}")
                    sb.AppendLine($"10,""Total Allowable Input Tax"",{vatReturn.TotalInputVat:F2}")
                    sb.AppendLine($"11,""VAT Payable/(Creditable)"",{vatReturn.VatPayable:F2}")
                    sb.AppendLine("12,""Surcharge"",0.00")
                    sb.AppendLine("13,""Interest"",0.00")
                    sb.AppendLine("14,""Compromise"",0.00")
                    For i = 15 To 22
                        sb.AppendLine($"{i},""N/A"",0.00")
                    Next
                    sb.AppendLine($"23,""Total Amount Due"",{vatReturn.VatPayable:F2}")

                Case VatReturnFormType.Form2550Q
                    sb.AppendLine($"1,""Taxable Sales Q1 Month 1"",{vatReturn.TotalVatableSales:F2}")
                    sb.AppendLine("2,""Taxable Sales Q1 Month 2"",0.00")
                    sb.AppendLine("3,""Taxable Sales Q1 Month 3"",0.00")
                    sb.AppendLine($"4,""Total Taxable Sales for the Quarter"",{vatReturn.TotalVatableSales:F2}")
                    sb.AppendLine($"5,""Zero-Rated Sales"",{vatReturn.TotalZeroRatedSales:F2}")
                    sb.AppendLine($"6,""Exempt Sales"",{vatReturn.TotalVatExemptSales:F2}")
                    sb.AppendLine($"7,""Total Gross Sales/Receipts"",{totalGross:F2}")
                    sb.AppendLine($"8,""Output Tax (12% of Line 4)"",{vatReturn.TotalOutputVat:F2}")
                    sb.AppendLine("9,""Adjustments to Output Tax"",0.00")
                    sb.AppendLine($"10,""Total Output Tax"",{vatReturn.TotalOutputVat:F2}")
                    sb.AppendLine("11,""Input Tax Carried Over from Previous Quarter"",0.00")
                    sb.AppendLine($"12,""Input Tax on Domestic Purchases"",{vatReturn.TotalInputVat:F2}")
                    sb.AppendLine($"13,""Total Allowable Input Tax"",{vatReturn.TotalInputVat:F2}")
                    sb.AppendLine($"14,""VAT Payable/(Creditable)"",{vatReturn.VatPayable:F2}")
                    sb.AppendLine("15,""Less: VAT Paid in 1st Month"",0.00")
                    sb.AppendLine("16,""Less: VAT Paid in 2nd Month"",0.00")
                    sb.AppendLine($"17,""Net VAT Payable for the Quarter"",{vatReturn.VatPayable:F2}")
                    sb.AppendLine("18,""Surcharge"",0.00")
                    sb.AppendLine("19,""Interest"",0.00")
                    sb.AppendLine("20,""Compromise"",0.00")
                    For i = 21 To 27
                        sb.AppendLine($"{i},""N/A"",0.00")
                    Next
                    sb.AppendLine($"28,""Total Amount Due"",{vatReturn.VatPayable:F2}")

                Case VatReturnFormType.Form2551Q
                    sb.AppendLine($"1,""Gross Taxable Receipts Month 1"",{vatReturn.TotalVatableSales:F2}")
                    sb.AppendLine("2,""Gross Taxable Receipts Month 2"",0.00")
                    sb.AppendLine("3,""Gross Taxable Receipts Month 3"",0.00")
                    sb.AppendLine($"4,""Total Gross Taxable Receipts"",{vatReturn.TotalVatableSales:F2}")
                    sb.AppendLine("5,""Rate of Tax (3%)"",0.03")
                    sb.AppendLine($"6,""Tax Due (Line 4 × Line 5)"",{vatReturn.VatPayable:F2}")
                    sb.AppendLine("7,""Less: Tax Paid in Previous Months"",0.00")
                    sb.AppendLine($"8,""Tax Still Due"",{vatReturn.VatPayable:F2}")
                    sb.AppendLine("9,""Surcharge"",0.00")
                    sb.AppendLine("10,""Interest"",0.00")
                    sb.AppendLine("11,""Compromise"",0.00")
                    sb.AppendLine("12,""N/A"",0.00")
                    sb.AppendLine("13,""N/A"",0.00")
                    sb.AppendLine($"14,""Total Amount Due"",{vatReturn.VatPayable:F2}")
            End Select

            Return sb.ToString()
        End Function

        ' ─── PDF Line Item Builder ──────────────────────────────────────────────────

        Private Class VatReturnLineItem
            Public Property LineNo As String
            Public Property Description As String
            Public Property Amount As String
        End Class

        Private Shared Function BuildCsvLineItems(vatReturn As Entities.VatReturn) As List(Of VatReturnLineItem)
            Dim items As New List(Of VatReturnLineItem)()
            Dim totalGross = vatReturn.TotalVatableSales + vatReturn.TotalVatExemptSales + vatReturn.TotalZeroRatedSales

            Select Case vatReturn.FormType
                Case VatReturnFormType.Form2550M
                    items.Add(New VatReturnLineItem With {.LineNo = "1", .Description = "Taxable Sales/Receipts (Net of VAT)", .Amount = vatReturn.TotalVatableSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "2", .Description = "Zero-Rated Sales/Receipts", .Amount = vatReturn.TotalZeroRatedSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "3", .Description = "Exempt Sales/Receipts", .Amount = vatReturn.TotalVatExemptSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "4", .Description = "Total Gross Sales/Receipts", .Amount = totalGross.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "5", .Description = "Output Tax (12% of Line 1)", .Amount = vatReturn.TotalOutputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "6", .Description = "Adjustments to Output Tax", .Amount = "0.00"})
                    items.Add(New VatReturnLineItem With {.LineNo = "7", .Description = "Total Output Tax", .Amount = vatReturn.TotalOutputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "8", .Description = "Input Tax Carried Over from Previous Period", .Amount = "0.00"})
                    items.Add(New VatReturnLineItem With {.LineNo = "9", .Description = "Input Tax on Domestic Purchases", .Amount = vatReturn.TotalInputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "10", .Description = "Total Allowable Input Tax", .Amount = vatReturn.TotalInputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "11", .Description = "VAT Payable/(Creditable)", .Amount = vatReturn.VatPayable.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "23", .Description = "Total Amount Due", .Amount = vatReturn.VatPayable.ToString("N2")})

                Case VatReturnFormType.Form2550Q
                    items.Add(New VatReturnLineItem With {.LineNo = "1", .Description = "Taxable Sales Q1 Month 1", .Amount = vatReturn.TotalVatableSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "4", .Description = "Total Taxable Sales for the Quarter", .Amount = vatReturn.TotalVatableSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "5", .Description = "Zero-Rated Sales", .Amount = vatReturn.TotalZeroRatedSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "6", .Description = "Exempt Sales", .Amount = vatReturn.TotalVatExemptSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "7", .Description = "Total Gross Sales/Receipts", .Amount = totalGross.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "8", .Description = "Output Tax (12% of Line 4)", .Amount = vatReturn.TotalOutputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "10", .Description = "Total Output Tax", .Amount = vatReturn.TotalOutputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "12", .Description = "Input Tax on Domestic Purchases", .Amount = vatReturn.TotalInputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "13", .Description = "Total Allowable Input Tax", .Amount = vatReturn.TotalInputVat.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "14", .Description = "VAT Payable/(Creditable)", .Amount = vatReturn.VatPayable.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "17", .Description = "Net VAT Payable for the Quarter", .Amount = vatReturn.VatPayable.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "28", .Description = "Total Amount Due", .Amount = vatReturn.VatPayable.ToString("N2")})

                Case VatReturnFormType.Form2551Q
                    items.Add(New VatReturnLineItem With {.LineNo = "1", .Description = "Gross Taxable Receipts Month 1", .Amount = vatReturn.TotalVatableSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "4", .Description = "Total Gross Taxable Receipts", .Amount = vatReturn.TotalVatableSales.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "5", .Description = "Rate of Tax (3%)", .Amount = "0.03"})
                    items.Add(New VatReturnLineItem With {.LineNo = "6", .Description = "Tax Due (Line 4 × Line 5)", .Amount = vatReturn.VatPayable.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "8", .Description = "Tax Still Due", .Amount = vatReturn.VatPayable.ToString("N2")})
                    items.Add(New VatReturnLineItem With {.LineNo = "14", .Description = "Total Amount Due", .Amount = vatReturn.VatPayable.ToString("N2")})
            End Select

            Return items
        End Function

        ' ─── Template Engine ────────────────────────────────────────────────────────

        Private Function LoadTemplate(formType As VatReturnFormType) As String
            Dim resourceName = $"MerchSys.Accounting.Reports.Templates.{FormTemplateName(formType)}"
            Using stream = _assembly.GetManifestResourceStream(resourceName)
                If stream Is Nothing Then
                    _logger.LogWarning("Embedded template not found: {ResourceName}. Using built-in fallback.", resourceName)
                    Return FallbackTemplate(formType)
                End If
                Using reader As New StreamReader(stream, Encoding.UTF8)
                    Return reader.ReadToEnd()
                End Using
            End Using
        End Function

        Private Shared Function FormTemplateName(formType As VatReturnFormType) As String
            Select Case formType
                Case VatReturnFormType.Form2550M : Return "Form2550M.template"
                Case VatReturnFormType.Form2550Q : Return "Form2550Q.template"
                Case Else : Return "Form2551Q.template"
            End Select
        End Function

        Private Shared Function ApplyTemplate(template As String, vatReturn As Entities.VatReturn) As String
            Dim totalGross = vatReturn.TotalVatableSales + vatReturn.TotalVatExemptSales + vatReturn.TotalZeroRatedSales
            Dim deadline = FilingDeadlineText(vatReturn)

            Return template.
                Replace("{{BusinessName}}", "Villon Farm Supply").
                Replace("{{BusinessTIN}}", "N/A").
                Replace("{{PeriodDescription}}", PeriodDescription(vatReturn)).
                Replace("{{GeneratedAt}}", vatReturn.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss") & " UTC").
                Replace("{{FilingStatus}}", vatReturn.FilingStatus.ToString()).
                Replace("{{TotalVatableSales}}", vatReturn.TotalVatableSales.ToString("N2")).
                Replace("{{TotalVatExemptSales}}", vatReturn.TotalVatExemptSales.ToString("N2")).
                Replace("{{TotalZeroRatedSales}}", vatReturn.TotalZeroRatedSales.ToString("N2")).
                Replace("{{TotalGrossSales}}", totalGross.ToString("N2")).
                Replace("{{TotalOutputVat}}", vatReturn.TotalOutputVat.ToString("N2")).
                Replace("{{TotalVatablePurchases}}", vatReturn.TotalVatablePurchases.ToString("N2")).
                Replace("{{TotalInputVat}}", vatReturn.TotalInputVat.ToString("N2")).
                Replace("{{VatPayable}}", vatReturn.VatPayable.ToString("N2")).
                Replace("{{TaxRate}}", "0.03").
                Replace("{{Month1VatableSales}}", vatReturn.TotalVatableSales.ToString("N2")).
                Replace("{{Month2VatableSales}}", "0.00").
                Replace("{{Month3VatableSales}}", "0.00").
                Replace("{{Month1GrossReceipts}}", vatReturn.TotalVatableSales.ToString("N2")).
                Replace("{{Month2GrossReceipts}}", "0.00").
                Replace("{{Month3GrossReceipts}}", "0.00").
                Replace("{{FilingDeadline}}", deadline).
                Replace("{{WhatThisMeans}}", WhatThisMeansText(vatReturn))
        End Function

        Private Shared Function PeriodDescription(vatReturn As Entities.VatReturn) As String
            If vatReturn.PeriodType = VatReturnPeriodType.Monthly Then
                Return $"Month {vatReturn.Period}, {vatReturn.Year}"
            End If
            Return $"Q{vatReturn.Period} {vatReturn.Year}"
        End Function

        Private Shared Function FilingDeadlineText(vatReturn As Entities.VatReturn) As String
            Select Case vatReturn.FormType
                Case VatReturnFormType.Form2550M
                    Dim followingMonth = New DateTime(vatReturn.Year, vatReturn.Period, 1).AddMonths(1)
                    Return followingMonth.ToString("MMMM") & " 20, " & followingMonth.Year.ToString() & " (manual) / " &
                           followingMonth.ToString("MMMM") & " 25, " & followingMonth.Year.ToString() & " (eFPS)"
                Case Else
                    Dim quarterEndMonth = vatReturn.Period * 3
                    Dim filingYear = vatReturn.Year
                    If quarterEndMonth = 12 Then
                        filingYear += 1
                        quarterEndMonth = 1
                    Else
                        quarterEndMonth += 1
                    End If
                    Return New DateTime(filingYear, quarterEndMonth, 25).ToString("MMMM d, yyyy")
            End Select
        End Function

        Private Shared Function WhatThisMeansText(vatReturn As Entities.VatReturn) As String
            If vatReturn.VatPayable > 0 Then
                Return $"You owe ₱{vatReturn.VatPayable:N2} to BIR for {PeriodDescription(vatReturn)}."
            ElseIf vatReturn.VatPayable < 0 Then
                Return $"You have a VAT input credit of ₱{Math.Abs(vatReturn.VatPayable):N2} for {PeriodDescription(vatReturn)}. This will be carried forward."
            End If
            Return $"No VAT due for {PeriodDescription(vatReturn)}."
        End Function

        Private Shared Function FallbackTemplate(formType As VatReturnFormType) As String
            Return $"BIR {formType} — plain-text export (template file not embedded)." & Environment.NewLine &
                   "{{TotalVatableSales}}, {{TotalOutputVat}}, {{TotalInputVat}}, {{VatPayable}}"
        End Function

    End Class

End Namespace
