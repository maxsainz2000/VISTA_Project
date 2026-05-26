Imports System.IO
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.Win32
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    Partial Class TamperAuditReportView
        Inherits UserControl

        Public Sub New(viewModel As TamperAuditReportViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Async Sub ExportCsv_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, TamperAuditReportViewModel)
            If vm Is Nothing Then Return

            Dim suggested = ResolveSuggestedPath(vm.Options.DefaultDirectory, vm.Options.CsvFileNamePattern)
            Dim dialog As New SaveFileDialog() With {
                .Filter = "CSV files (*.csv)|*.csv",
                .FileName = Path.GetFileName(suggested),
                .InitialDirectory = Path.GetDirectoryName(suggested),
                .OverwritePrompt = True
            }

            If dialog.ShowDialog() = True Then
                Try
                    Await vm.ExportCsvCommand.ExecuteAsync(dialog.FileName)
                Catch ex As Exception
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub ExportPdf_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = DirectCast(DataContext, TamperAuditReportViewModel)
            If vm Is Nothing Then Return

            Dim suggested = ResolveSuggestedPath(vm.Options.DefaultDirectory, vm.Options.PdfFileNamePattern)
            Dim dialog As New SaveFileDialog() With {
                .Filter = "PDF files (*.pdf)|*.pdf",
                .FileName = Path.GetFileName(suggested),
                .InitialDirectory = Path.GetDirectoryName(suggested),
                .OverwritePrompt = True
            }

            If dialog.ShowDialog() = True Then
                Try
                    Await vm.ExportPdfCommand.ExecuteAsync(dialog.FileName)
                Catch ex As Exception
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Function ResolveSuggestedPath(defaultDirectory As String, pattern As String) As String
            Dim expandedDir = Environment.ExpandEnvironmentVariables(defaultDirectory)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm")
            Dim fileName = pattern.Replace("{YYYYMMDD-HHmm}", timestamp)
            Return Path.Combine(expandedDir, fileName)
        End Function

    End Class

End Namespace
