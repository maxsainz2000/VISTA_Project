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
            sb.AppendLine("""Section"",""Amount""")
            sb.AppendLine($"""Net Sales"",""{vm.NetSalesDisplay}""")
            sb.AppendLine($"""Less: Cost of Goods Sold"",""{vm.COGSDisplay}""")
            sb.AppendLine($"""Gross Profit"",""{vm.GrossProfitDisplay}""")
            sb.AppendLine($"""Gross Margin %"",""{vm.GrossMarginPercent:N1}%""")
            sb.AppendLine($"""Less: Operating Expenses"",""{vm.OperatingExpensesDisplay}""")
            sb.AppendLine($"""Less: Shrinkage Loss"",""{vm.ShrinkageLossDisplay}""")
            sb.AppendLine($"""Net Income"",""{vm.NetIncomeDisplay}""")
            sb.AppendLine($"""Net Margin %"",""{vm.NetMarginPercent:N1}%""")

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
            Const W As Integer = 56
            Dim sep = New String("="c, W)
            Dim sb As New StringBuilder()
            sb.AppendLine("INCOME STATEMENT")
            sb.AppendLine(vm.PeriodDescription)
            sb.AppendLine($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}")
            sb.AppendLine(sep)
            sb.AppendLine()
            sb.AppendLine(PadLine("Net Sales", vm.NetSalesDisplay, W))
            sb.AppendLine(PadLine("Less: Cost of Goods Sold", vm.COGSDisplay, W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine(PadLine("Gross Profit", vm.GrossProfitDisplay, W))
            sb.AppendLine(PadLine("  Gross Margin", $"{vm.GrossMarginPercent:N1}%", W))
            sb.AppendLine()
            sb.AppendLine(PadLine("Less: Operating Expenses", vm.OperatingExpensesDisplay, W))
            sb.AppendLine(PadLine("Less: Shrinkage Loss", vm.ShrinkageLossDisplay, W))
            sb.AppendLine(New String("-"c, W))
            sb.AppendLine(PadLine("NET INCOME", vm.NetIncomeDisplay, W))
            sb.AppendLine(PadLine("  Net Margin", $"{vm.NetMarginPercent:N1}%", W))

            If vm.ProductMargins.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine(sep)
                sb.AppendLine("PRODUCT BREAKDOWN")
                sb.AppendLine(sep)
                Const Hdr = "Product                   Revenue      COGS   Gross Profit   Margin  Units"
                sb.AppendLine(Hdr)
                sb.AppendLine(New String("-"c, W))
                For Each m In vm.ProductMargins
                    Dim nm = If(m.ProductName.Length > 24, m.ProductName.Substring(0, 24), m.ProductName)
                    sb.AppendLine($"{nm,-26}₱{m.Revenue,8:N2}  ₱{m.COGS,8:N2}   ₱{m.GrossProfit,9:N2}  {m.GrossMarginPercent,5:N1}%  {m.UnitsSold,4}")
                Next
            End If

            sb.AppendLine()
            sb.AppendLine(sep)
            sb.AppendLine("Villon Farm Supply — VISTA Income Statement")
            sb.AppendLine($"Exported {DateTime.Now:MM/dd/yyyy HH:mm}")
            Return sb.ToString()
        End Function

        Private Shared Function PadLine(label As String, amount As String, width As Integer) As String
            Dim remaining = width - label.Length - amount.Length
            Return label & New String(" "c, Math.Max(1, remaining)) & amount
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
