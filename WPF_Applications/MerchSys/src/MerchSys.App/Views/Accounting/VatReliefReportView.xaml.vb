Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    Partial Class VatReliefReportView
        Inherits UserControl

        Public Sub New(viewModel As VatReliefReportViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
