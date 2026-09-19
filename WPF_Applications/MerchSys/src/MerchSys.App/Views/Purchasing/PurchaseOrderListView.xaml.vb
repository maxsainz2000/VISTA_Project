Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' PO Management screen — lists all purchase orders with status filtering
    ''' and hosts the inline editor panel for creating and editing Draft POs.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' </summary>
    Partial Class PurchaseOrderListView
        Inherits UserControl

        Public Sub New(viewModel As PurchaseOrderListViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub POGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
            Handles POGrid.SelectionChanged

            Dim vm = TryCast(DataContext, PurchaseOrderListViewModel)
            If vm Is Nothing Then Return
            vm.SelectedOrder = TryCast(POGrid.SelectedItem, PORowItem)
        End Sub

        Private Sub POGrid_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) _
            Handles POGrid.MouseDoubleClick

            Dim vm = TryCast(DataContext, PurchaseOrderListViewModel)
            If vm Is Nothing Then Return
            If vm.SelectedOrder IsNot Nothing AndAlso vm.EditPOCommand IsNot Nothing AndAlso vm.EditPOCommand.CanExecute(Nothing) Then
                Dispatcher.InvokeAsync(Async Function()
                                           Await vm.EditPOCommand.ExecuteAsync(Nothing)
                                       End Function)
            End If
        End Sub

        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Escape Then
                Dim vm = TryCast(DataContext, PurchaseOrderListViewModel)
                If vm IsNot Nothing Then vm.SearchText = String.Empty
            End If
        End Sub

        Private Sub EditorPanelOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           VendorComboBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, PurchaseOrderListViewModel)
            If vm IsNot Nothing AndAlso vm.IsEditorOpen Then
                If e.Key = System.Windows.Input.Key.Escape Then
                    If vm.CancelEditorCommand IsNot Nothing AndAlso vm.CancelEditorCommand.CanExecute(Nothing) Then
                        vm.CancelEditorCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                ElseIf e.Key = System.Windows.Input.Key.Enter Then
                    If vm.SaveDraftCommand IsNot Nothing AndAlso vm.SaveDraftCommand.CanExecute(Nothing) Then
                        vm.SaveDraftCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                End If
            End If
        End Sub

    End Class

End Namespace
