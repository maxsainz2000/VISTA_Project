Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.Inventory.ViewModels

Namespace Views.Inventory

    ''' <summary>
    ''' Main Inventory screen — real-time stock dashboard.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' Row selection fires SelectProductCommand to load the product detail panel.
    ''' </summary>
    Partial Class StockDashboardView
        Inherits UserControl

        Public Sub New(viewModel As StockDashboardViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub ProductGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
            Handles ProductGrid.SelectionChanged

            Dim vm = TryCast(DataContext, StockDashboardViewModel)
            If vm Is Nothing Then Return

            Dim row = TryCast(ProductGrid.SelectedItem, ProductRowItem)
            If row Is Nothing Then Return

            Dispatcher.InvokeAsync(Async Function()
                                       Await vm.SelectProductCommand.ExecuteAsync(row)
                                   End Function)
        End Sub

        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Escape Then
                Dim vm = TryCast(DataContext, StockDashboardViewModel)
                If vm IsNot Nothing Then vm.SearchText = String.Empty
            End If
        End Sub

        Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            Dim vm = TryCast(DataContext, IDisposable)
            If vm IsNot Nothing Then vm.Dispose()
        End Sub

    End Class

End Namespace
