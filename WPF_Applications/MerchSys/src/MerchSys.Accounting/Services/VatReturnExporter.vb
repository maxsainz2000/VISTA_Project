Imports System.IO
Imports System.Reflection
Imports System.Text
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Enums
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging

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

            Dim templateText = LoadTemplate(vatReturn.FormType)
            Dim rendered = ApplyTemplate(templateText, vatReturn)
            Dim bytes = Encoding.UTF8.GetBytes(rendered)
            Return New MemoryStream(bytes)
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
