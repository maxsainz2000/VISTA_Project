Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    Partial Class VendorCatalogView
        Inherits UserControl

        Public Sub New(viewModel As VendorCatalogViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub OpenSearchDialog(sender As Object, e As System.Windows.RoutedEventArgs)
            Dim vm = TryCast(DataContext, VendorCatalogViewModel)
            If vm IsNot Nothing Then
                vm.IsSearchDialogOpen = True
                Dispatcher.BeginInvoke(Sub()
                                           ModalSearchBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub CloseSearchDialog(sender As Object, e As System.Windows.RoutedEventArgs)
            Dim vm = TryCast(DataContext, VendorCatalogViewModel)
            If vm IsNot Nothing Then
                vm.IsSearchDialogOpen = False
            End If
        End Sub

        Private Sub ModalSearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, VendorCatalogViewModel)
                If vm IsNot Nothing Then
                    If vm.SearchProductsCommand.CanExecute(Nothing) Then
                        vm.SearchProductsCommand.Execute(Nothing)
                    End If
                End If
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, VendorCatalogViewModel)
            If vm IsNot Nothing AndAlso vm.IsSearchDialogOpen Then
                If e.Key = System.Windows.Input.Key.Escape Then
                    vm.IsSearchDialogOpen = False
                    e.Handled = True
                ElseIf e.Key = System.Windows.Input.Key.Enter Then
                    Dim focusedTxt = TryCast(System.Windows.Input.Keyboard.FocusedElement, TextBox)
                    If focusedTxt Is Nothing OrElse focusedTxt.Name <> "ModalSearchBox" Then
                        If vm.AddProductCommand.CanExecute(Nothing) Then
                            vm.AddProductCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
