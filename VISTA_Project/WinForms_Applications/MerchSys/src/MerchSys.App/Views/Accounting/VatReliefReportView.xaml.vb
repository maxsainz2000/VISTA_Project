Imports System.IO
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Accounting.Services.Reporting
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.App.Views.Shell
Imports Microsoft.Win32

Namespace Views.Accounting

    Partial Class VatReliefReportView
        Inherits UserControl

        Private ReadOnly _pdfExporter As IVatReliefPdfExporter

        Public Sub New(viewModel As VatReliefReportViewModel, pdfExporter As IVatReliefPdfExporter)
            InitializeComponent()
            DataContext = viewModel
            _pdfExporter = pdfExporter
        End Sub

        ' ── Export CSV ────────────────────────────────────────────────────────────

        Private Sub ExportCsv_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, VatReliefReportViewModel)
            Dim csv = BuildVatReliefCsv(vm)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim suggested = $"VatReliefReport-{timestamp}.csv"

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
            Dim vm = DirectCast(DataContext, VatReliefReportViewModel)
            Try
                Dim result = Await _pdfExporter.GenerateAsync(vm)
                Dim monthLabel = If(vm.Summary IsNot Nothing,
                                    New DateTime(vm.SelectedYear, vm.SelectedMonth, 1).ToString("MMM yyyy"),
                                    $"{vm.SelectedYear}-{vm.SelectedMonth:D2}")
                Dim preview As New ReportPreviewWindow(
                    $"VAT Relief Report — {monthLabel}",
                    result)
                preview.Owner = Window.GetWindow(Me)
                preview.ShowDialog()
            Catch ex As Exception
                MessageBox.Show($"PDF generation failed: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        ' ── Builders ──────────────────────────────────────────────────────────────

        Private Shared Function BuildVatReliefCsv(vm As VatReliefReportViewModel) As String
            Dim s = vm.Summary
            Dim monthLabel = If(s IsNot Nothing,
                                New DateTime(s.Year, s.Month, 1).ToString("MMMM yyyy"),
                                $"{vm.SelectedYear}/{vm.SelectedMonth:D2}")
            Dim sb As New StringBuilder()
            sb.AppendLine($"""VAT Relief Report"",""{monthLabel}""")
            sb.AppendLine($"""Generated"",""{DateTime.Now:MM/dd/yyyy HH:mm}""")
            sb.AppendLine()

            ' Current month summary
            sb.AppendLine("""Category"",""Description"",""Amount (PHP)""")
            If s IsNot Nothing Then
                sb.AppendLine($"""SALES"",""VATable Sales"",""{s.VatableSales:N2}""")
                sb.AppendLine($"""SALES"",""VAT-Exempt Sales"",""{s.VatExemptSales:N2}""")
                sb.AppendLine($"""SALES"",""Zero-Rated Sales"",""{s.ZeroRatedSales:N2}""")
                sb.AppendLine($"""SALES"",""Output VAT"",""{s.OutputVat:N2}""")
                sb.AppendLine($"""SALES"",""Total Gross Sales"",""{s.TotalGrossSales:N2}""")
                sb.AppendLine($"""PURCHASES"",""VATable Purchases"",""{s.VatablePurchases:N2}""")
                sb.AppendLine($"""PURCHASES"",""VAT-Exempt Purchases"",""{s.VatExemptPurchases:N2}""")
                sb.AppendLine($"""PURCHASES"",""Zero-Rated Purchases"",""{s.ZeroRatedPurchases:N2}""")
                sb.AppendLine($"""PURCHASES"",""Input VAT"",""{s.InputVat:N2}""")
                sb.AppendLine($"""PURCHASES"",""Total Gross Purchases"",""{s.TotalGrossPurchases:N2}""")
                sb.AppendLine($"""NET"",""Net VAT Payable"",""{s.NetVatPayable:N2}""")
            End If

            ' Trailing months
            If vm.TrailingMonths.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("""TRAILING 12 MONTHS""")
                sb.AppendLine("""Year"",""Month"",""Output VAT"",""Input VAT"",""Net VAT Payable""")
                For Each t In vm.TrailingMonths
                    sb.AppendLine($"""{t.Year}"",""{t.Month}"",""{t.OutputVat:N2}"",""{t.InputVat:N2}"",""{t.NetVatPayable:N2}""")
                Next
            End If

            Return sb.ToString()
        End Function

        Private Shared Function PadLine(label As String, amount As String, width As Integer) As String
            Dim remaining = width - label.Length - amount.Length
            Return label & New String(" "c, Math.Max(1, remaining)) & amount
        End Function

    End Class

End Namespace
