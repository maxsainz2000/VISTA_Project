Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.App.ViewModels

Namespace Views

    ''' <summary>
    ''' Owner KPI Dashboard — read-only landing page for the Owner role.
    ''' DataContext is resolved from DI via constructor injection.
    ''' The Unloaded handler disposes the ViewModel's DispatcherTimer to stop auto-refresh.
    ''' </summary>
    Partial Class OwnerDashboardView
        Inherits UserControl

        Public Sub New(viewModel As OwnerDashboardViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, OwnerDashboardViewModel)
            If vm IsNot Nothing Then vm.Dispose()
        End Sub

    End Class

End Namespace
