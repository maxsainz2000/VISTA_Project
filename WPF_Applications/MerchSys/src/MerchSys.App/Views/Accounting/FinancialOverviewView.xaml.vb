Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels

Namespace Views.Accounting

    ''' <summary>
    ''' Primary Accounting screen — Financial Overview KPI Dashboard.
    ''' DataContext is resolved from DI via constructor injection.
    ''' Auto-refresh is managed by the ViewModel timer (every 5 minutes).
    ''' </summary>
    Partial Class FinancialOverviewView
        Inherits UserControl

        Public Sub New(viewModel As FinancialOverviewViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
