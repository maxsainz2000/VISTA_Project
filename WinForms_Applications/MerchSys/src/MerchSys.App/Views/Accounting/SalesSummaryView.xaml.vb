Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    Partial Class SalesSummaryView
        Inherits UserControl

        Public Sub New(viewModel As SalesSummaryViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
