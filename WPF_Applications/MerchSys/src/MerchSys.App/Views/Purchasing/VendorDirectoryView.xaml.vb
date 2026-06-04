Imports System.Windows.Controls
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' Vendor Directory screen — lists all vendors with search, hosts an inline editor
    ''' panel for create/edit, and shows a purchase history detail panel for the selected vendor.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' </summary>
    Partial Class VendorDirectoryView
        Inherits UserControl

        Public Sub New(viewModel As VendorListViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub VendorGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
            Handles VendorGrid.SelectionChanged

            Dim vm = TryCast(DataContext, VendorListViewModel)
            If vm Is Nothing Then Return
            vm.SelectedVendor = TryCast(VendorGrid.SelectedItem, Vendor)
        End Sub

        Private Sub VendorGrid_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) _
            Handles VendorGrid.MouseDoubleClick

            Dim vm = TryCast(DataContext, VendorListViewModel)
            If vm Is Nothing Then Return
            If vm.SelectedVendor IsNot Nothing AndAlso vm.EditVendorCommand.CanExecute(Nothing) Then
                Dispatcher.InvokeAsync(Async Function()
                                           Await vm.EditVendorCommand.ExecuteAsync(Nothing)
                                       End Function)
            End If
        End Sub

        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Escape Then
                Dim vm = TryCast(DataContext, VendorListViewModel)
                If vm IsNot Nothing Then vm.SearchText = String.Empty
            End If
        End Sub

        Private Sub EditorPanelOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           VendorNameTextBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, VendorListViewModel)
            If vm IsNot Nothing AndAlso vm.IsEditorOpen Then
                If e.Key = System.Windows.Input.Key.Escape Then
                    If vm.CancelEditorCommand.CanExecute(Nothing) Then
                        vm.CancelEditorCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                ElseIf e.Key = System.Windows.Input.Key.Enter Then
                    If vm.SaveVendorCommand.CanExecute(Nothing) Then
                        vm.SaveVendorCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                End If
            End If
        End Sub

    End Class

End Namespace
