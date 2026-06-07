Imports System.IO
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.App.Views.Shell
Imports Microsoft.Win32

Namespace Views.Accounting

    Partial Class VatReliefReportView
        Inherits UserControl

        Public Sub New(viewModel As VatReliefReportViewModel)
            InitializeComponent()
            DataContext = viewModel
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

        ' ── Export PDF (plain-text report with print preview) ─────────────────────

        Private Sub ExportPdf_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, VatReliefReportViewModel)
            Dim content = BuildVatReliefReport(vm)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim monthLabel = If(vm.Summary IsNot Nothing,
                                New DateTime(vm.SelectedYear, vm.SelectedMonth, 1).ToString("MMM yyyy"),
                                $"{vm.SelectedYear}-{vm.SelectedMonth:D2}")

            Dim preview As New ReportPreviewWindow(
                $"VAT Relief Report — {monthLabel}",
                content,
                $"VatReliefReport-{timestamp}.txt")
            preview.Owner = Window.GetWindow(Me)
            preview.ShowDialog()
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

        Private Shared Function BuildVatReliefReport(vm As VatReliefReportViewModel) As String
            Dim s = vm.Summary
            Dim monthLabel = If(s IsNot Nothing,
                                New DateTime(s.Year, s.Month, 1).ToString("MMMM yyyy"),
                                $"{vm.SelectedYear}/{vm.SelectedMonth:D2}")
            Const W As Integer = 60
            Dim sep = New String("="c, W)
            Dim sb As New StringBuilder()
            sb.AppendLine("VAT RELIEF REPORT")
            sb.AppendLine(monthLabel)
            sb.AppendLine($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}")
            sb.AppendLine(sep)
            sb.AppendLine()

            If s IsNot Nothing Then
                sb.AppendLine("SALES SUMMARY")
                sb.AppendLine(New String("-"c, W))
                sb.AppendLine(PadLine("VATable Sales:", $"₱{s.VatableSales:N2}", W))
                sb.AppendLine(PadLine("VAT-Exempt Sales:", $"₱{s.VatExemptSales:N2}", W))
                sb.AppendLine(PadLine("Zero-Rated Sales:", $"₱{s.ZeroRatedSales:N2}", W))
                sb.AppendLine(PadLine("Output VAT:", $"₱{s.OutputVat:N2}", W))
                sb.AppendLine(PadLine("Total Gross Sales:", $"₱{s.TotalGrossSales:N2}", W))
                sb.AppendLine()
                sb.AppendLine("PURCHASES SUMMARY")
                sb.AppendLine(New String("-"c, W))
                sb.AppendLine(PadLine("VATable Purchases:", $"₱{s.VatablePurchases:N2}", W))
                sb.AppendLine(PadLine("VAT-Exempt Purchases:", $"₱{s.VatExemptPurchases:N2}", W))
                sb.AppendLine(PadLine("Zero-Rated Purchases:", $"₱{s.ZeroRatedPurchases:N2}", W))
                sb.AppendLine(PadLine("Input VAT:", $"₱{s.InputVat:N2}", W))
                sb.AppendLine(PadLine("Total Gross Purchases:", $"₱{s.TotalGrossPurchases:N2}", W))
                sb.AppendLine()
                sb.AppendLine(sep)
                sb.AppendLine(PadLine("NET VAT PAYABLE:", $"₱{s.NetVatPayable:N2}", W))
                sb.AppendLine(sep)
                sb.AppendLine()

                If Not String.IsNullOrEmpty(vm.WhatThisMeans) Then
                    sb.AppendLine("INTERPRETATION")
                    sb.AppendLine(vm.WhatThisMeans)
                    sb.AppendLine()
                End If
            End If

            If vm.TrailingMonths.Count > 0 Then
                sb.AppendLine(sep)
                sb.AppendLine("TRAILING 12-MONTH TREND")
                sb.AppendLine(sep)
                sb.AppendLine($"{"Month",-14}{"Output VAT",14}{"Input VAT",14}{"Net Payable",14}")
                sb.AppendLine(New String("-"c, W))
                For Each t In vm.TrailingMonths
                    Dim mLabel = New DateTime(t.Year, t.Month, 1).ToString("MMM yyyy")
                    sb.AppendLine($"{mLabel,-14}₱{t.OutputVat,12:N2}  ₱{t.InputVat,12:N2}  ₱{t.NetVatPayable,12:N2}")
                Next
                sb.AppendLine()
            End If

            sb.AppendLine(sep)
            sb.AppendLine("Villon Farm Supply — VISTA VAT Relief Report")
            sb.AppendLine($"Exported {DateTime.Now:MM/dd/yyyy HH:mm}")
            Return sb.ToString()
        End Function

        Private Shared Function PadLine(label As String, amount As String, width As Integer) As String
            Dim remaining = width - label.Length - amount.Length
            Return label & New String(" "c, Math.Max(1, remaining)) & amount
        End Function

    End Class

End Namespace
