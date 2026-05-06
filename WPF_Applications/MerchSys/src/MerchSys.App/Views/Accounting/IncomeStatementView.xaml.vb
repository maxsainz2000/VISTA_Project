Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

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

    End Class

End Namespace
