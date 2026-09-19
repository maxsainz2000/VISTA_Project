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
            ' Application.Current.MainWindow is the LoginView (first window shown), not the shell.
            ' Iterate all open windows to find the one backed by MainWindowViewModel.
            Dim mainVm As MainWindowViewModel = Nothing
            For Each win As Window In Application.Current.Windows
                Dim vm = TryCast(win.DataContext, MainWindowViewModel)
                If vm IsNot Nothing Then
                    mainVm = vm
                    Exit For
                End If
            Next
            If mainVm Is Nothing Then Return
            Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = GetType(VatReturnView))
            Dim item = pair.Item
            If item IsNot Nothing AndAlso mainVm.NavigateCommand.CanExecute(item) Then
                mainVm.NavigateCommand.Execute(item)
            End If
        End Sub

        Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            Dim vm = TryCast(DataContext, IDisposable)
            If vm IsNot Nothing Then vm.Dispose()
        End Sub

    End Class

End Namespace
