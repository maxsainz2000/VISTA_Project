Imports MerchSys.Accounting.ViewModels
Imports MerchSys.SharedKernel.Queries
Imports MediatR
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services.Reporting

    ''' <summary>
    ''' QuestPDF implementation of <see cref="IVatReliefPdfExporter"/>.
    ''' Renders an A4 portrait VAT Relief Report with Sales/Purchases summaries,
    ''' trailing 12-month trend table, interpretation text, and paginated footer.
    ''' </summary>
    Public Class VatReliefPdfExporter
        Implements IVatReliefPdfExporter

        Private Const FontFamily As String = "Consolas"
        Private Const FontSize As Single = 9.0F

        Private ReadOnly _mediator As IMediator

        Public Sub New(mediator As IMediator)
            _mediator = mediator
        End Sub

        Public Async Function GenerateAsync(vm As VatReliefReportViewModel) As Task(Of ReportPdfResult) _
            Implements IVatReliefPdfExporter.GenerateAsync

            ' Resolve business identity for the header
            Dim businessName As String = ""
            Dim businessAddress As String = ""
            Dim businessTin As String = ""

            Try
                Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())
                If vatConfig IsNot Nothing Then
                    businessName = If(vatConfig.BusinessName, String.Empty)
                    businessAddress = If(vatConfig.BusinessAddress, String.Empty)
                    businessTin = If(vatConfig.BusinessTIN, String.Empty)
                End If
            Catch
                ' Modular isolation fallback
            End Try

            Dim s = vm.Summary
            Dim monthLabel = If(s IsNot Nothing,
                                New DateTime(vm.SelectedYear, vm.SelectedMonth, 1).ToString("MMMM yyyy"),
                                $"{vm.SelectedYear}-{vm.SelectedMonth:D2}")

            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim suggestedFileName = $"VatReliefReport-{timestamp}.pdf"

            Dim doc = Document.Create(
                Sub(container)
                    container.Page(
                        Sub(pg)
                            pg.Size(PageSizes.A4)
                            pg.Margin(30)
                            pg.DefaultTextStyle(Function(st) st.FontFamily(FontFamily).FontSize(FontSize))

                            ' ── Header ────────────────────────────────────────────
                            pg.Header().Column(
                                Sub(col)
                                    If Not String.IsNullOrWhiteSpace(businessName) Then
                                        col.Item().Text(businessName).Bold().FontSize(12)
                                    End If
                                    If Not String.IsNullOrWhiteSpace(businessAddress) Then
                                        col.Item().Text(businessAddress).FontSize(8)
                                    End If
                                    If Not String.IsNullOrWhiteSpace(businessTin) Then
                                        col.Item().Text($"TIN: {businessTin}").FontSize(8)
                                    End If
                                    col.Item().PaddingTop(10).Text("VAT RELIEF REPORT").Bold().FontSize(14)
                                    col.Item().Text(monthLabel).FontSize(10)
                                    col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Local)").FontSize(8)
                                    col.Item().PaddingBottom(10)
                                End Sub)

                            ' ── Content ───────────────────────────────────────────
                            pg.Content().PaddingTop(5).Column(
                                Sub(col)
                                    If s IsNot Nothing Then
                                        ' Sales Summary section
                                        col.Item().Text("SALES SUMMARY").Bold().FontSize(11)
                                        col.Item().PaddingTop(5).Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(5)
                                                        cols.RelativeColumn(3)
                                                    End Sub)

                                                tbl.Header(
                                                    Sub(h)
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Description").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Amount (₱)").Bold()
                                                    End Sub)

                                                AddSummaryRow(tbl, "VATable Sales", $"₱{s.VatableSales:N2}", False)
                                                AddSummaryRow(tbl, "VAT-Exempt Sales", $"₱{s.VatExemptSales:N2}", False)
                                                AddSummaryRow(tbl, "Zero-Rated Sales", $"₱{s.ZeroRatedSales:N2}", False)
                                                AddSummaryRow(tbl, "Output VAT", $"₱{s.OutputVat:N2}", True)
                                                AddSummaryRow(tbl, "Total Gross Sales", $"₱{s.TotalGrossSales:N2}", True)
                                            End Sub)

                                        ' Purchases Summary section
                                        col.Item().PaddingTop(15).Text("PURCHASES SUMMARY").Bold().FontSize(11)
                                        col.Item().PaddingTop(5).Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(5)
                                                        cols.RelativeColumn(3)
                                                    End Sub)

                                                tbl.Header(
                                                    Sub(h)
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Description").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Amount (₱)").Bold()
                                                    End Sub)

                                                AddSummaryRow(tbl, "VATable Purchases", $"₱{s.VatablePurchases:N2}", False)
                                                AddSummaryRow(tbl, "VAT-Exempt Purchases", $"₱{s.VatExemptPurchases:N2}", False)
                                                AddSummaryRow(tbl, "Zero-Rated Purchases", $"₱{s.ZeroRatedPurchases:N2}", False)
                                                AddSummaryRow(tbl, "Input VAT", $"₱{s.InputVat:N2}", True)
                                                AddSummaryRow(tbl, "Total Gross Purchases", $"₱{s.TotalGrossPurchases:N2}", True)
                                            End Sub)

                                        ' Net VAT Payable
                                        col.Item().PaddingTop(15).Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(5)
                                                        cols.RelativeColumn(3)
                                                    End Sub)
                                                tbl.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("NET VAT PAYABLE").Bold().FontSize(10)
                                                tbl.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{s.NetVatPayable:N2}").Bold().FontSize(10)
                                            End Sub)

                                        ' Interpretation
                                        If Not String.IsNullOrEmpty(vm.WhatThisMeans) Then
                                            col.Item().PaddingTop(12).Text("INTERPRETATION").Bold().FontSize(10)
                                            col.Item().PaddingTop(4).Text(vm.WhatThisMeans).FontSize(9)
                                        End If
                                    End If

                                    ' Trailing 12 Months
                                    If vm.TrailingMonths.Count > 0 Then
                                        col.Item().PaddingTop(20).Text("TRAILING 12-MONTH TREND").Bold().FontSize(11)
                                        col.Item().PaddingTop(5).Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(2)  ' Month
                                                        cols.RelativeColumn(2.5) ' Output VAT
                                                        cols.RelativeColumn(2.5) ' Input VAT
                                                        cols.RelativeColumn(2.5) ' Net Payable
                                                    End Sub)

                                                tbl.Header(
                                                    Sub(h)
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Month").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Output VAT").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Input VAT").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Net Payable").Bold()
                                                    End Sub)

                                                For Each t In vm.TrailingMonths
                                                    Dim mLabel = New DateTime(t.Year, t.Month, 1).ToString("MMM yyyy")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(mLabel)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{t.OutputVat:N2}")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{t.InputVat:N2}")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{t.NetVatPayable:N2}")
                                                Next
                                            End Sub)
                                    End If
                                End Sub)

                            ' ── Footer ────────────────────────────────────────────
                            pg.Footer().AlignCenter().PaddingTop(10).Text(
                                Sub(txt)
                                    txt.Span("Villon Farm Supply — VISTA VAT Relief Report — page ")
                                    txt.CurrentPageNumber()
                                    txt.Span(" of ")
                                    txt.TotalPages()
                                End Sub)
                        End Sub)
                End Sub)

            ' Generate PDF bytes and page images on a background thread
            Dim result As New ReportPdfResult With {.SuggestedFileName = suggestedFileName}
            Await Task.Run(
                Sub()
                    result.PdfBytes = doc.GeneratePdf()
                    Dim images As New List(Of Byte())
                    For Each pageImage In doc.GenerateImages(New ImageGenerationSettings With {.RasterDpi = 150})
                        images.Add(pageImage)
                    Next
                    result.PageImages = images
                End Sub)

            Return result
        End Function

        Private Shared Sub AddSummaryRow(tbl As TableDescriptor, label As String, amount As String, bold As Boolean)
            If bold Then
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label).Bold()
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(amount).Bold()
            Else
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label)
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(amount)
            End If
        End Sub

    End Class

End Namespace
