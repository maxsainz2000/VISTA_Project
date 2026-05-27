Imports System.Windows.Controls
Imports MerchSys.Inventory.ViewModels
Imports Microsoft.Extensions.DependencyInjection

Namespace Views.Inventory

    ''' <summary>
    ''' Product Management screen — Manager-only CRUD for products and categories.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' </summary>
    Partial Class ProductManagementView
        Inherits UserControl

        Private ReadOnly _serviceProvider As IServiceProvider

        Public Sub New(viewModel As ProductManagementViewModel, serviceProvider As IServiceProvider)
            InitializeComponent()
            DataContext = viewModel
            _serviceProvider = serviceProvider
        End Sub

        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Escape Then
                Dim vm = TryCast(DataContext, ProductManagementViewModel)
                If vm IsNot Nothing Then vm.SearchText = String.Empty
            End If
        End Sub

        Private Sub ViewPriceHistory_Click(sender As Object, e As System.Windows.RoutedEventArgs)
            Dim vm = TryCast(DataContext, ProductManagementViewModel)
            If vm IsNot Nothing AndAlso vm.SelectedProduct IsNot Nothing Then
                Dim view = _serviceProvider.GetRequiredService(Of ProductPriceHistoryView)()
                Dim historyVm = TryCast(view.DataContext, ProductPriceHistoryViewModel)
                If historyVm IsNot Nothing Then
                    historyVm.ProductId = vm.SelectedProduct.ProductId
                    historyVm.ProductName = vm.SelectedProduct.Name
                    Dim task = historyVm.LoadHistoryAsync()
                End If
                view.Owner = System.Windows.Window.GetWindow(Me)
                view.ShowDialog()
            End If
        End Sub

    End Class

End Namespace
