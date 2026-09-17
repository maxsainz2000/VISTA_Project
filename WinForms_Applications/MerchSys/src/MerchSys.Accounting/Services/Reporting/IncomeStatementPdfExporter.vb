Imports MerchSys.Accounting.ViewModels
Imports MerchSys.SharedKernel.Queries
Imports MediatR
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services.Reporting

    ''' <summary>
    ''' QuestPDF implementation of <see cref="IIncomeStatementPdfExporter"/>.
    ''' Renders an A4 portrait Income Statement with business header, comparative P&amp;L table,
    ''' per-product breakdown, and paginated footer.  Follows the same visual conventions
    ''' as <see cref="TamperReportExporter"/>.
    ''' </summary>
    Public Class IncomeStatementPdfExporter
        Implements IIncomeStatementPdfExporter

        Private Const FontFamily As String = "Consolas"
        Private Const FontSize As Single = 9.0F

        Private ReadOnly _mediator As IMediator

        Public Sub New(mediator As IMediator)
            _mediator = mediator
        End Sub

        Public Async Function GenerateAsync(vm As IncomeStatementViewModel) As Task(Of ReportPdfResult) _
            Implements IIncomeStatementPdfExporter.GenerateAsync

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

            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim suggestedFileName = $"IncomeStatement-{timestamp}.pdf"

            Dim doc = Document.Create(
                Sub(container)
                    container.Page(
                        Sub(pg)
                            pg.Size(PageSizes.A4)
                            pg.Margin(30)
                            pg.DefaultTextStyle(Function(s) s.FontFamily(FontFamily).FontSize(FontSize))

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
                                    col.Item().PaddingTop(10).Text("INCOME STATEMENT").Bold().FontSize(14)
                                    col.Item().Text(vm.PeriodDescription).FontSize(10)
                                    col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (Local)").FontSize(8)
                                    col.Item().PaddingBottom(10)
                                End Sub)

                            ' ── Content ───────────────────────────────────────────
                            pg.Content().PaddingTop(5).Column(
                                Sub(col)
                                    ' Comparative P&L table
                                    col.Item().Table(
                                        Sub(tbl)
                                            tbl.ColumnsDefinition(
                                                Sub(cols)
                                                    cols.RelativeColumn(5)  ' Line Item
                                                    cols.RelativeColumn(3)  ' Current
                                                    cols.RelativeColumn(3)  ' Prior
                                                    cols.RelativeColumn(2)  ' Change
                                                End Sub)

                                            ' Header row
                                            tbl.Header(
                                                Sub(h)
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Line Item").Bold()
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Current").Bold()
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text($"Prior ({vm.PrevPeriodLabel})").Bold()
                                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Change").Bold()
                                                End Sub)

                                            ' Data rows
                                            AddRow(tbl, "Net Sales", vm.NetSalesDisplay, vm.PrevNetSalesDisplay, If(vm.ShowNetSalesDelta, FormatDelta(vm.NetSalesDelta), ""), False)
                                            AddRow(tbl, "Less: Cost of Goods Sold", vm.COGSDisplay, vm.PrevCOGSDisplay, If(vm.ShowCOGSDelta, FormatDelta(vm.COGSDelta), ""), False)

                                            ' Separator
                                            AddSeparator(tbl)

                                            AddRow(tbl, "Gross Profit", vm.GrossProfitDisplay, vm.PrevGrossProfitDisplay, If(vm.ShowGrossProfitDelta, FormatDelta(vm.GrossProfitDelta), ""), True)
                                            AddRow(tbl, "  Gross Margin", $"{vm.GrossMarginPercent:N1}%", vm.PrevGrossMarginPercentDisplay, "", False)

                                            ' Spacing row
                                            AddEmptyRow(tbl)

                                            AddRow(tbl, "Other Operating Expenses", vm.OtherOperatingExpensesDisplay, vm.PrevOtherOperatingExpensesDisplay, If(vm.ShowOtherOperatingExpensesDelta, FormatDelta(vm.OtherOperatingExpensesDelta), ""), False)
                                            AddRow(tbl, "Shrinkage Loss", vm.ShrinkageLossDisplay, vm.PrevShrinkageLossDisplay, If(vm.ShowShrinkageLossDelta, FormatDelta(vm.ShrinkageLossDelta), ""), False)

                                            AddSeparator(tbl)

                                            AddRow(tbl, "Total Operating Expenses", vm.OperatingExpensesDisplay, vm.PrevOperatingExpensesDisplay, If(vm.ShowOperatingExpensesDelta, FormatDelta(vm.OperatingExpensesDelta), ""), True)

                                            AddSeparator(tbl)

                                            AddRow(tbl, "NET INCOME", vm.NetIncomeDisplay, vm.PrevNetIncomeDisplay, If(vm.ShowNetIncomeDelta, FormatDelta(vm.NetIncomeDelta), ""), True)
                                            AddRow(tbl, "  Net Margin", $"{vm.NetMarginPercent:N1}%", vm.PrevNetMarginPercentDisplay, "", False)
                                        End Sub)

                                    ' Product Breakdown
                                    If vm.ProductMargins.Count > 0 Then
                                        col.Item().PaddingTop(20).Text("PRODUCT BREAKDOWN").Bold().FontSize(11)
                                        col.Item().PaddingTop(5).Table(
                                            Sub(tbl)
                                                tbl.ColumnsDefinition(
                                                    Sub(cols)
                                                        cols.RelativeColumn(4)  ' Product
                                                        cols.RelativeColumn(2)  ' Revenue
                                                        cols.RelativeColumn(2)  ' COGS
                                                        cols.RelativeColumn(2)  ' Gross Profit
                                                        cols.RelativeColumn(1.5) ' Margin %
                                                        cols.RelativeColumn(1)  ' Units
                                                    End Sub)

                                                tbl.Header(
                                                    Sub(h)
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Product").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Revenue").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("COGS").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Gross Profit").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Margin %").Bold()
                                                        h.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Units").Bold()
                                                    End Sub)

                                                For Each m In vm.ProductMargins
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(m.ProductName)
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{m.Revenue:N2}")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{m.COGS:N2}")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"₱{m.GrossProfit:N2}")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{m.GrossMarginPercent:N1}%")
                                                    tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(m.UnitsSold.ToString())
                                                Next
                                            End Sub)
                                    End If
                                End Sub)

                            ' ── Footer ────────────────────────────────────────────
                            pg.Footer().AlignCenter().PaddingTop(10).Text(
                                Sub(txt)
                                    txt.Span("Villon Farm Supply — VISTA Income Statement — page ")
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

        ' ── Helpers ─────────────────────────────────────────────────────────────

        Private Shared Sub AddRow(tbl As TableDescriptor, label As String, current As String, prior As String, delta As String, bold As Boolean)
            Dim borderSpec = Sub(cell As IContainer)
                                 ' no-op thin border on bottom
                             End Sub

            If bold Then
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label).Bold()
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(current).Bold()
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(prior).Bold()
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(delta).Bold()
            Else
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label)
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(current)
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(prior)
                tbl.Cell().BorderBottom(0.5, QuestPDF.Infrastructure.Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(delta)
            End If
        End Sub

        Private Shared Sub AddSeparator(tbl As TableDescriptor)
            tbl.Cell().ColumnSpan(4).PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1)
        End Sub

        Private Shared Sub AddEmptyRow(tbl As TableDescriptor)
            tbl.Cell().ColumnSpan(4).PaddingVertical(4)
        End Sub

        Private Shared Function FormatDelta(value As Double) As String
            If value > 0.001 Then Return $"+{value:N1}%"
            If value < -0.001 Then Return $"{value:N1}%"
            Return "0.0%"
        End Function

    End Class

End Namespace
