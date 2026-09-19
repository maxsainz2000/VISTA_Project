Imports System.IO
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Accounting.Services.Reporting
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.App.Views.Shell
Imports Microsoft.Win32

Namespace Views.Accounting

    ''' <summary>
    ''' Income Statement (P&amp;L) view — Monthly, Quarterly, and Annual formats.
    ''' DataContext resolved from DI via constructor injection.
    ''' </summary>
    Partial Class IncomeStatementView
        Inherits UserControl

        Private ReadOnly _pdfExporter As IIncomeStatementPdfExporter

        Public Sub New(viewModel As IncomeStatementViewModel, pdfExporter As IIncomeStatementPdfExporter)
            InitializeComponent()
            DataContext = viewModel
            _pdfExporter = pdfExporter
        End Sub

        ' ── Export CSV ────────────────────────────────────────────────────────────

        Private Sub ExportCsv_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, IncomeStatementViewModel)
            Dim csv = BuildIncomeStatementCsv(vm)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim suggested = $"IncomeStatement-{timestamp}.csv"

            Dim dlg As New SaveFileDialog() With {
                .Filter = "CSV files (*.csv)|*.csv",
                .FileName = suggested,
                .OverwritePrompt = True
            }

            If dlg.ShowDialog() = True Then
                Dim saveErr As String = String.Empty
                Try
                    File.WriteAllText(dlg.FileName, csv, Encoding.UTF8)
                Catch ex As Exception
                    saveErr = ex.Message
                End Try
                If Not String.IsNullOrEmpty(saveErr) Then
                    MessageBox.Show($"Export failed: {saveErr}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End If
        End Sub

        ' ── Export PDF (A4 PDF with preview) ──────────────────────────────────────

        Private Async Sub ExportPdf_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, IncomeStatementViewModel)
            Try
                Dim result = Await _pdfExporter.GenerateAsync(vm)
                Dim preview As New ReportPreviewWindow(
                    $"Income Statement — {vm.PeriodDescription}",
                    result)
                preview.Owner = Window.GetWindow(Me)
                preview.ShowDialog()
            Catch ex As Exception
                MessageBox.Show($"PDF generation failed: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        ' ── Builders ──────────────────────────────────────────────────────────────

        Private Shared Function BuildIncomeStatementCsv(vm As IncomeStatementViewModel) As String
            Dim sb As New StringBuilder()
            sb.AppendLine($"""Income Statement"",""{EscapeCsv(vm.PeriodDescription)}""")
            sb.AppendLine($"""Generated"",""{DateTime.Now:MM/dd/yyyy HH:mm}""")
            sb.AppendLine()
            sb.AppendLine($"""Section"",""Current"",""Prior ({EscapeCsv(vm.PrevPeriodLabel)})"",""Change""")
            sb.AppendLine($"""Net Sales"",""{vm.NetSalesDisplay}"",""{vm.PrevNetSalesDisplay}"",""{If(vm.ShowNetSalesDelta, FormatDeltaPercent(vm.NetSalesDelta), "")}""")
            sb.AppendLine($"""Less: Cost of Goods Sold"",""{vm.COGSDisplay}"",""{vm.PrevCOGSDisplay}"",""{If(vm.ShowCOGSDelta, FormatDeltaPercent(vm.COGSDelta), "")}""")
            sb.AppendLine($"""Gross Profit"",""{vm.GrossProfitDisplay}"",""{vm.PrevGrossProfitDisplay}"",""{If(vm.ShowGrossProfitDelta, FormatDeltaPercent(vm.GrossProfitDelta), "")}""")
            sb.AppendLine($"""Gross Margin"",""{vm.GrossMarginPercent:N1}%"",""{vm.PrevGrossMarginPercentDisplay}"",""""")
            sb.AppendLine($"""Other Operating Expenses"",""{vm.OtherOperatingExpensesDisplay}"",""{vm.PrevOtherOperatingExpensesDisplay}"",""{If(vm.ShowOtherOperatingExpensesDelta, FormatDeltaPercent(vm.OtherOperatingExpensesDelta), "")}""")
            sb.AppendLine($"""Shrinkage Loss"",""{vm.ShrinkageLossDisplay}"",""{vm.PrevShrinkageLossDisplay}"",""{If(vm.ShowShrinkageLossDelta, FormatDeltaPercent(vm.ShrinkageLossDelta), "")}""")
            sb.AppendLine($"""Total Operating Expenses"",""{vm.OperatingExpensesDisplay}"",""{vm.PrevOperatingExpensesDisplay}"",""{If(vm.ShowOperatingExpensesDelta, FormatDeltaPercent(vm.OperatingExpensesDelta), "")}""")
            sb.AppendLine($"""Net Income"",""{vm.NetIncomeDisplay}"",""{vm.PrevNetIncomeDisplay}"",""{If(vm.ShowNetIncomeDelta, FormatDeltaPercent(vm.NetIncomeDelta), "")}""")
            sb.AppendLine($"""Net Margin"",""{vm.NetMarginPercent:N1}%"",""{vm.PrevNetMarginPercentDisplay}"",""""")

            If vm.ProductMargins.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("""Product"",""Revenue"",""COGS"",""Gross Profit"",""Gross Margin %"",""Units Sold""")
                For Each m In vm.ProductMargins
                    sb.AppendLine($"""{EscapeCsv(m.ProductName)}"",""₱{m.Revenue:N2}"",""₱{m.COGS:N2}"",""₱{m.GrossProfit:N2}"",""{m.GrossMarginPercent:N1}%"",""{m.UnitsSold}""")
                Next
            End If

            Return sb.ToString()
        End Function

        Private Shared Function FormatDeltaPercent(delta As Double) As String
            If delta > 0.001 Then
                Return $"+{delta:N1}%"
            ElseIf delta < -0.001 Then
                Return $"{delta:N1}%"
            Else
                Return "0.0%"
            End If
        End Function

        ''' <summary>
        ''' Escapes a value for embedding inside a double-quoted CSV field by doubling any
        ''' internal quotes (RFC 4180). Guards against SKU names like 2" PVC pipe breaking columns.
        ''' </summary>
        Private Shared Function EscapeCsv(value As String) As String
            If value Is Nothing Then Return String.Empty
            Return value.Replace("""", """""")
        End Function

    End Class

End Namespace
