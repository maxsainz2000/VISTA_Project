Imports System.IO
Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    Partial Class VatReturnView
        Inherits UserControl

        Private ReadOnly _viewModel As VatReturnViewModel

        Public Sub New(viewModel As VatReturnViewModel)
            InitializeComponent()
            _viewModel = viewModel
            DataContext = viewModel
            AddHandler viewModel.ExportReady, AddressOf OnExportReady
        End Sub

        Private Sub OnExportReady(sender As Object, e As ExportReadyEventArgs)
            Dim dlg As New Microsoft.Win32.SaveFileDialog With {
                .FileName = e.FileName,
                .Filter = e.Filter,
                .Title = "Save VAT Return Export"
            }

            If dlg.ShowDialog() = True Then
                Using fileStream = dlg.OpenFile()
                    e.Data.CopyTo(fileStream)
                End Using
            End If
            e.Data.Dispose()
        End Sub

    End Class

End Namespace
