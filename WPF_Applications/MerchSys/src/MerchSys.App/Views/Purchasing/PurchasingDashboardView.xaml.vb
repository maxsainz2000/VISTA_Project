Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels
Imports MerchSys.App.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' Code-behind for the Purchasing Dashboard View.
    ''' Injects the PurchasingDashboardViewModel and handles navigation requests.
    ''' </summary>
    Partial Class PurchasingDashboardView
        Inherits UserControl

        Public Sub New(viewModel As PurchasingDashboardViewModel)
            InitializeComponent()
            DataContext = viewModel
            AddHandler viewModel.NavigateToViewRequested, AddressOf OnNavigateToViewRequested
        End Sub

        Private Sub OnNavigateToViewRequested(sender As Object, viewName As String)
            Dim targetType As Type = Nothing
            Select Case viewName
                Case "APLedgerView"
                    targetType = GetType(APLedgerView)
                Case "PurchaseOrderListView"
                    targetType = GetType(PurchaseOrderListView)
                Case "ReorderSuggestionsView"
                    targetType = GetType(ReorderSuggestionsView)
                Case "VendorDirectoryView"
                    targetType = GetType(VendorDirectoryView)
            End Select

            If targetType Is Nothing Then Return

            Dim mainVm As MainWindowViewModel = Nothing
            For Each win As Window In Application.Current.Windows
                Dim vm = TryCast(win.DataContext, MainWindowViewModel)
                If vm IsNot Nothing Then
                    mainVm = vm
                    Exit For
                End If
            Next
            If mainVm Is Nothing Then Return

            Dim item = mainVm.NavigationGroups _
                .SelectMany(Function(g) g.Items) _
                .FirstOrDefault(Function(i) i.ViewType = targetType)
            If item IsNot Nothing AndAlso mainVm.NavigateCommand.CanExecute(item) Then
                mainVm.NavigateCommand.Execute(item)
            End If
        End Sub

    End Class

End Namespace
