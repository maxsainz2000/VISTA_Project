Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Accounting.ViewModels
Imports MerchSys.App.ViewModels

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
            AddHandler viewModel.NavigateToVatReturnRequested, AddressOf OnNavigateToVatReturnRequested
        End Sub

        ''' <summary>
        ''' Handles <see cref="FinancialOverviewViewModel.NavigateToVatReturnRequested"/>, raised when the
        ''' user clicks the VAT Payable tile.  Uses the type-based <see cref="MainWindowViewModel.NavigateCommand"/>
        ''' pattern established by INT-02 and INT-13: resolves a <see cref="MerchSys.App.Models.NavigationItem"/>
        ''' from <see cref="MainWindowViewModel.NavigationGroups"/> by <c>ViewType</c> and passes it to
        ''' <c>NavigateCommand</c>.  Navigation is silently suppressed when the current role is Owner
        ''' (the VAT Return nav entry is not added for Owner in <c>BuildAccountingNavItems</c>).
        ''' Precedent: <see cref="MainWindowViewModel.NavigateToDefault"/> — identical lookup idiom.
        ''' </summary>
        Private Sub OnNavigateToVatReturnRequested(sender As Object, e As EventArgs)
            Dim mainWindow = TryCast(Application.Current.MainWindow?.DataContext, MainWindowViewModel)
            If mainWindow Is Nothing Then Return
            Dim item = mainWindow.NavigationGroups _
                .SelectMany(Function(g) g.Items) _
                .FirstOrDefault(Function(i) i.ViewType = GetType(VatReturnView))
            If item IsNot Nothing AndAlso mainWindow.NavigateCommand.CanExecute(item) Then
                mainWindow.NavigateCommand.Execute(item)
            End If
        End Sub

    End Class

End Namespace
