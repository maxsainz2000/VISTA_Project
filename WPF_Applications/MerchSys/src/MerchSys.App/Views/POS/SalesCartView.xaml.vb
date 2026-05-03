Imports System.Windows.Controls
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    ''' <summary>
    ''' Primary POS transaction screen.
    ''' DataContext is set via constructor injection — resolved from DI by the host.
    ''' </summary>
    Partial Class SalesCartView
        Inherits UserControl

        Public Sub New(viewModel As SalesCartViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        ' ── DataGrid row-edit events ──────────────────────────────────────────────

        ''' <summary>
        ''' Fires the UpdateQuantityCommand when the user commits an edit on a cart row.
        ''' This propagates Quantity or DiscountAmount changes back to CartService.
        ''' </summary>
        Private Sub CartDataGrid_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs) _
            Handles CartDataGrid.CellEditEnding

            If e.EditAction = DataGridEditAction.Commit Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                If vm Is Nothing Then Return

                Dim line = TryCast(e.Row.Item, CartLineItem)
                If line Is Nothing Then Return

                ' Commit happens before the binding updates the source in some scenarios;
                ' dispatch to after the current edit cycle completes.
                Dispatcher.InvokeAsync(Async Function()
                                           Await vm.UpdateQuantityCommand.ExecuteAsync(line)
                                       End Function)
            End If
        End Sub

        ' ── Product list double-click → add to cart ───────────────────────────────

        ''' <summary>
        ''' Double-clicking a product in the search results list adds it to the cart.
        ''' </summary>
        Private Sub ProductList_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) _
            Handles ProductList.MouseDoubleClick

            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm Is Nothing Then Return

            Dim product = TryCast(ProductList.SelectedItem, ProductSearchItem)
            If product Is Nothing Then Return

            Dim addTask = vm.AddToCartCommand.ExecuteAsync(product)
        End Sub

        ' ── Product search on Enter key ───────────────────────────────────────────

        Private Sub ProductSearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles ProductSearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                vm?.SearchProductCommand.Execute(Nothing)
            End If
        End Sub

    End Class

End Namespace
