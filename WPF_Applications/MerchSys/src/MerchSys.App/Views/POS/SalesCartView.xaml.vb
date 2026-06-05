Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Linq
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

        ' ── Event Handlers & Focus Management ─────────────────────────────────────

        Private Sub OnTransactionCompleted(sender As Object, e As EventArgs)
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub CartLines_CollectionChanged(sender As Object, e As Specialized.NotifyCollectionChangedEventArgs)
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub SalesCartView_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm IsNot Nothing Then
                ' Ensure clean registration to avoid duplicates
                RemoveHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                RemoveHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
                AddHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                AddHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
            End If
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub SalesCartView_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm IsNot Nothing Then
                RemoveHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                RemoveHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
            End If
        End Sub

        ' ── Keyboard Shortcuts (Ctrl+Q, Ctrl+D, Ctrl+T) ─────────────────────────

        Private Sub SalesCartView_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
            ' Cart-scoped hotkeys that do not bubble to the window
            If (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
                If e.Key = Key.Q Then
                    FocusCartGridCell("Qty")
                    e.Handled = True
                ElseIf e.Key = Key.D Then
                    FocusCartGridCell("Discount")
                    e.Handled = True
                ElseIf e.Key = Key.T Then
                    AmountTenderedTextBox.Focus()
                    AmountTenderedTextBox.SelectAll()
                    e.Handled = True
                End If
            End If
        End Sub

        Private Sub FocusCartGridCell(columnName As String)
            If CartDataGrid.SelectedItem Is Nothing AndAlso CartDataGrid.Items.Count > 0 Then
                CartDataGrid.SelectedIndex = 0
            End If
            If CartDataGrid.SelectedItem IsNot Nothing Then
                Dim selectedItem = CartDataGrid.SelectedItem
                CartDataGrid.Focus()
                Dim column = CartDataGrid.Columns.FirstOrDefault(Function(c) c.Header.ToString() = columnName)
                If column IsNot Nothing Then
                    CartDataGrid.CurrentCell = New DataGridCellInfo(selectedItem, column)
                    CartDataGrid.BeginEdit()
                End If
            End If
        End Sub

        ' Toolbar click handlers
        Private Sub SetQtyButton_Click(sender As Object, e As RoutedEventArgs)
            FocusCartGridCell("Qty")
        End Sub

        Private Sub ApplyDiscountButton_Click(sender As Object, e As RoutedEventArgs)
            FocusCartGridCell("Discount")
        End Sub

        ' ── DataGrid row-edit events ──────────────────────────────────────────────

        ''' <summary>
        ''' Routes committed cell edits to the correct service call:
        '''   Qty     → UpdateQuantityCommand  → CartService.UpdateLineQuantityAsync
        '''   Discount → ApplyDiscountCommand   → CartService.ApplyLineDiscountAsync
        ''' </summary>
        Private Sub CartDataGrid_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs) _
            Handles CartDataGrid.CellEditEnding

            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm Is Nothing Then Return

            Dim line = TryCast(e.Row.Item, CartLineItem)
            If line Is Nothing Then Return

            If e.EditAction = DataGridEditAction.Commit Then
                Dim columnHeader = TryCast(e.Column.Header, String)

                ' Dispatch after the DataGrid finishes its commit so bindings
                ' have pushed the new value to the CartLineItem source property.
                If String.Equals(columnHeader, "Discount", StringComparison.OrdinalIgnoreCase) Then
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.ApplyDiscountCommand.ExecuteAsync(line)
                                               ProductSearchBox.Focus()
                                           End Function)
                Else
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.UpdateQuantityCommand.ExecuteAsync(line)
                                               ProductSearchBox.Focus()
                                           End Function)
                End If
            Else
                ' If canceled, just return focus to the scan field
                Dispatcher.InvokeAsync(Sub()
                                           ProductSearchBox.Focus()
                                       End Sub)
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

        ' ── ProductList KeyDown (Enter to add) ───────────────────────────────────

        Private Sub ProductList_KeyDown(sender As Object, e As KeyEventArgs) Handles ProductList.KeyDown
            If e.Key = Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                If vm Is Nothing Then Return

                Dim product = TryCast(ProductList.SelectedItem, ProductSearchItem)
                If product Is Nothing Then Return

                Dim addTask = vm.AddToCartCommand.ExecuteAsync(product)
                e.Handled = True
            End If
        End Sub

        ' ── Product search/add on Enter key ───────────────────────────────────────

        Private Sub ProductSearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles ProductSearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                If vm IsNot Nothing Then
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.SearchAndAddProductCommand.ExecuteAsync(Nothing)
                                               ProductSearchBox.Focus()
                                           End Function)
                    e.Handled = True
                End If
            End If
        End Sub

        ' ── Credit Customer Search KeyDown (Enter to search) ──────────────────────

        Private Sub CreditCustomerSearch_KeyDown(sender As Object, e As KeyEventArgs) Handles CreditCustomerSearch.KeyDown
            If e.Key = Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                vm?.SearchCreditCustomerCommand.Execute(Nothing)
                e.Handled = True
            End If
        End Sub

    End Class

End Namespace
