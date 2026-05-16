Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    Partial Class TamperAuditReportView
        Inherits UserControl

        Public Sub New(viewModel As TamperAuditReportViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
