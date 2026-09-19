Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' AP Ledger screen — displays all vendor invoices with outstanding balances,
    ''' overdue highlighting, and inline payment recording dialog.
    ''' DataContext is resolved from DI via constructor injection.
    ''' </summary>
    Partial Class APLedgerView
        Inherits UserControl

        Public Sub New(viewModel As APLedgerViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub PaymentDialogOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           PaymentAmountInputTextBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            Dim vm = TryCast(DataContext, APLedgerViewModel)
            If vm IsNot Nothing AndAlso vm.IsPaymentDialogOpen Then
                If e.Key = Key.Escape Then
                    If vm.CancelPaymentCommand.CanExecute(Nothing) Then
                        vm.CancelPaymentCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                ElseIf e.Key = Key.Enter Then
                    If vm.ConfirmPaymentCommand.CanExecute(Nothing) Then
                        vm.ConfirmPaymentCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                End If
            End If
        End Sub

    End Class

End Namespace
