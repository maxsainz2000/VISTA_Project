Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.App.ViewModels
Imports MerchSys.App.Views.Accounting
Imports MerchSys.App.Views.Inventory
Imports MerchSys.App.Views.POS
Imports MerchSys.App.Views.Purchasing

Namespace Views

    ''' <summary>
    ''' Owner KPI Dashboard — read-only landing page for the Owner role.
    ''' DataContext is resolved from DI via constructor injection.
    ''' The Unloaded handler disposes the ViewModel's DispatcherTimer to stop auto-refresh.
    ''' Drill-down events from the ViewModel are handled here by resolving the target
    ''' NavigationItem from MainWindowViewModel and executing NavigateCommand (same pattern
    ''' as FinancialOverviewView.OnNavigateToVatReturnRequested).
    ''' </summary>
    Partial Class OwnerDashboardView
        Inherits UserControl

        Public Sub New(viewModel As OwnerDashboardViewModel)
            InitializeComponent()
            DataContext = viewModel
            AddHandler viewModel.NavigateToPurchasingRequested, AddressOf OnNavigateToPurchasingRequested
            AddHandler viewModel.NavigateToInventoryRequested, AddressOf OnNavigateToInventoryRequested
            AddHandler viewModel.NavigateToSalesRequested, AddressOf OnNavigateToSalesRequested
            AddHandler viewModel.NavigateToAccountingRequested, AddressOf OnNavigateToAccountingRequested
        End Sub

        Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, OwnerDashboardViewModel)
            If vm IsNot Nothing Then vm.Dispose()
        End Sub

        Private Sub OnNavigateToPurchasingRequested(sender As Object, e As EventArgs)
            NavigateTo(GetType(PurchasingDashboardView))
        End Sub

        Private Sub OnNavigateToInventoryRequested(sender As Object, e As EventArgs)
            NavigateTo(GetType(StockDashboardView))
        End Sub

        Private Sub OnNavigateToSalesRequested(sender As Object, e As EventArgs)
            NavigateTo(GetType(SalesSummaryView))
        End Sub

        Private Sub OnNavigateToAccountingRequested(sender As Object, e As EventArgs)
            NavigateTo(GetType(FinancialOverviewView))
        End Sub

        ''' <summary>
        ''' Resolves the MainWindowViewModel from the open window set and executes
        ''' NavigateCommand for the NavigationItem whose ViewType matches <paramref name="targetViewType"/>.
        ''' Navigation is silently suppressed when the item is not found (e.g. role-gated view).
        ''' Precedent: FinancialOverviewView.OnNavigateToVatReturnRequested.
        ''' </summary>
        Private Shared Sub NavigateTo(targetViewType As Type)
            Dim mainVm As MainWindowViewModel = Nothing
            For Each win As Window In Application.Current.Windows
                Dim vm = TryCast(win.DataContext, MainWindowViewModel)
                If vm IsNot Nothing Then
                    mainVm = vm
                    Exit For
                End If
            Next
            If mainVm Is Nothing Then Return
            Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = targetViewType)
            Dim navItem = pair.Item
            If navItem IsNot Nothing AndAlso mainVm.NavigateCommand.CanExecute(navItem) Then
                mainVm.NavigateCommand.Execute(navItem)
            End If
        End Sub

    End Class

End Namespace
