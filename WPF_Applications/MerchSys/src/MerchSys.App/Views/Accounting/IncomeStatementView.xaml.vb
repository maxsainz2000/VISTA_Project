Imports System.IO
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.App.Views.Shell
Imports Microsoft.Win32

Namespace Views.Accounting

    ''' <summary>
    ''' Income Statement (P&L) view — Monthly, Quarterly, and Annual formats.
    ''' DataContext resolved from DI via constructor injection.
    ''' </summary>
    Partial Class IncomeStatementView
        Inherits UserControl

        Public Sub New(viewModel As IncomeStatementViewModel)
            InitializeComponent()
            DataContext = viewModel
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

        ' ── Export PDF (plain-text report with print preview) ─────────────────────

        Private Sub ExportPdf_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, IncomeStatementViewModel)
            Dim content = BuildIncomeStatementReport(vm)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")

            Dim preview As New ReportPreviewWindow(
                $"Income Statement — {vm.PeriodDescription}",
                content,
                $"IncomeStatement-{timestamp}.txt")
            preview.Owner = Window.GetWindow(Me)
            preview.ShowDialog()
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

        Private Shared Function BuildIncomeStatementReport(vm As IncomeStatementViewModel) As String
            Const W As Integer = 72
            Dim sep = New String("="c, W)
            Dim sb As New StringBuilder()
            sb.AppendLine("INCOME STATEMENT")
            sb.AppendLine(vm.PeriodDescription)
            sb.AppendLine($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}")
            sb.AppendLine(sep)
            
            Dim priorHeader = $"Prior ({vm.PrevPeriodLabel})"
            sb.AppendLine(FormatReportLine("Section", "Current", priorHeader, "Change", W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine()
            sb.AppendLine(FormatReportLine("Net Sales", vm.NetSalesDisplay, vm.PrevNetSalesDisplay, If(vm.ShowNetSalesDelta, FormatDeltaPercent(vm.NetSalesDelta), ""), W))
            sb.AppendLine(FormatReportLine("Less: Cost of Goods Sold", vm.COGSDisplay, vm.PrevCOGSDisplay, If(vm.ShowCOGSDelta, FormatDeltaPercent(vm.COGSDelta), ""), W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine(FormatReportLine("Gross Profit", vm.GrossProfitDisplay, vm.PrevGrossProfitDisplay, If(vm.ShowGrossProfitDelta, FormatDeltaPercent(vm.GrossProfitDelta), ""), W))
            sb.AppendLine(FormatReportLine("  Gross Margin", $"{vm.GrossMarginPercent:N1}%", vm.PrevGrossMarginPercentDisplay, "", W))
            sb.AppendLine()
            sb.AppendLine(FormatReportLine("Other Operating Expenses", vm.OtherOperatingExpensesDisplay, vm.PrevOtherOperatingExpensesDisplay, If(vm.ShowOtherOperatingExpensesDelta, FormatDeltaPercent(vm.OtherOperatingExpensesDelta), ""), W))
            sb.AppendLine(FormatReportLine("Shrinkage Loss", vm.ShrinkageLossDisplay, vm.PrevShrinkageLossDisplay, If(vm.ShowShrinkageLossDelta, FormatDeltaPercent(vm.ShrinkageLossDelta), ""), W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine(FormatReportLine("Total Operating Expenses", vm.OperatingExpensesDisplay, vm.PrevOperatingExpensesDisplay, If(vm.ShowOperatingExpensesDelta, FormatDeltaPercent(vm.OperatingExpensesDelta), ""), W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine(FormatReportLine("NET INCOME", vm.NetIncomeDisplay, vm.PrevNetIncomeDisplay, If(vm.ShowNetIncomeDelta, FormatDeltaPercent(vm.NetIncomeDelta), ""), W))
            sb.AppendLine(FormatReportLine("  Net Margin", $"{vm.NetMarginPercent:N1}%", vm.PrevNetMarginPercentDisplay, "", W))

            If vm.ProductMargins.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine(sep)
                sb.AppendLine("PRODUCT BREAKDOWN")
                sb.AppendLine(sep)
                sb.AppendLine("Product                     Revenue       COGS   Gross Profit   Margin  Units")
                sb.AppendLine(New String("-"c, W))
                For Each m In vm.ProductMargins
                    Dim nm = If(m.ProductName.Length > 24, m.ProductName.Substring(0, 24), m.ProductName)
                    sb.AppendLine($"{nm,-26}₱{m.Revenue,9:N2}  ₱{m.COGS,9:N2}   ₱{m.GrossProfit,10:N2}  {m.GrossMarginPercent,5:N1}%  {m.UnitsSold,5}")
                Next
            End If

            sb.AppendLine()
            sb.AppendLine(sep)
            sb.AppendLine("Villon Farm Supply — VISTA Income Statement")
            sb.AppendLine($"Exported {DateTime.Now:MM/dd/yyyy HH:mm}")
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

        Private Shared Function FormatReportLine(label As String, current As String, prior As String, delta As String, width As Integer) As String
            Dim labelWidth = width - 40
            Dim lbl = If(label.Length > labelWidth, label.Substring(0, labelWidth), label)
            Return $"{lbl.PadRight(labelWidth)}{current,14}{prior,14}{delta,12}"
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
