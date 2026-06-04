Imports System.Windows.Controls
Imports MerchSys.POS.Entities
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    ''' <summary>
    ''' Credit Management screen — account list, payment recording, and history panel.
    ''' DataContext is set via constructor injection resolved from the DI host.
    ''' </summary>
    Partial Class CreditManagementView
        Inherits UserControl

        Public Sub New(viewModel As CreditManagementViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        ''' <summary>
        ''' Fires SelectAccountCommand when the user selects a row in the accounts DataGrid,
        ''' loading that account's payment and credit-transaction history into the right panel.
        ''' </summary>
        Private Sub AccountsDataGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
            Handles AccountsDataGrid.SelectionChanged

            Dim vm = TryCast(DataContext, CreditManagementViewModel)
            If vm Is Nothing Then Return

            Dim account = TryCast(AccountsDataGrid.SelectedItem, CreditAccount)
            If account IsNot Nothing Then
                vm.SelectAccountCommand.Execute(account)
            End If
        End Sub

        ''' <summary>
        ''' Triggers SearchCommand when the user presses Enter in the search box.
        ''' </summary>
        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, CreditManagementViewModel)
                vm?.SearchCommand.Execute(Nothing)
            End If
        End Sub

        Private Sub AddAccountFormOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           NewAccountNameTextBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub PaymentDialogOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           PaymentAmountTextBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, CreditManagementViewModel)
            If vm IsNot Nothing Then
                If vm.IsPaymentDialogVisible Then
                    If e.Key = System.Windows.Input.Key.Escape Then
                        If vm.CancelPaymentCommand.CanExecute(Nothing) Then
                            vm.CancelPaymentCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    ElseIf e.Key = System.Windows.Input.Key.Enter Then
                        If vm.RecordPaymentCommand.CanExecute(Nothing) Then
                            vm.RecordPaymentCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    End If
                ElseIf vm.IsAddAccountVisible Then
                    If e.Key = System.Windows.Input.Key.Escape Then
                        If vm.CancelAddAccountCommand.CanExecute(Nothing) Then
                            vm.CancelAddAccountCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    ElseIf e.Key = System.Windows.Input.Key.Enter Then
                        If vm.ConfirmAddAccountCommand.CanExecute(Nothing) Then
                            vm.ConfirmAddAccountCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
