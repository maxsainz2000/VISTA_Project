Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' AP Ledger screen — displays all vendor invoices with outstanding balances,
    ''' overdue highlighting, and inline payment recording dialog.
    ''' DataContext is resolved from DI via constructor injection.
    ''' </summary>
    Partial Class APLedgerView
        Inherits UserControl

        Public Sub New(viewModel As APLedgerViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
