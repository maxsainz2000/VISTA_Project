Imports System.Windows.Controls
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    ''' <summary>
    ''' Transaction History screen — browse, filter, and manage past POS transactions.
    ''' DataContext is injected via DI. Row selection triggers detail panel load via ViewModel command.
    ''' </summary>
    Partial Class TransactionHistoryView
        Inherits UserControl

        Public Sub New(viewModel As TransactionHistoryViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        ' ── DataGrid row selection ────────────────────────────────────────────────

        ''' <summary>
        ''' Fires SelectTransactionCommand when the user clicks a row in the transaction grid.
        ''' </summary>
        Private Sub TransactionGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
            Handles TransactionGrid.SelectionChanged

            Dim vm = TryCast(DataContext, TransactionHistoryViewModel)
            If vm Is Nothing Then Return

            Dim item = TryCast(TransactionGrid.SelectedItem, TransactionSummaryItem)
            If item Is Nothing Then Return

            Dispatcher.InvokeAsync(Async Function()
                                       Await vm.SelectTransactionCommand.ExecuteAsync(item)
                                   End Function)
        End Sub

        ' ── TX number search on Enter ─────────────────────────────────────────────

        Private Sub TxNumberBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles TxNumberBox.KeyDown

            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, TransactionHistoryViewModel)
                vm?.SearchCommand.Execute(Nothing)
            End If
        End Sub

        ' ── Return quantity: restrict to digits ───────────────────────────────────

        Private Sub ReturnQtyBox_PreviewTextInput(sender As Object, e As System.Windows.Input.TextCompositionEventArgs) _
            Handles ReturnQtyBox.PreviewTextInput

            e.Handled = Not e.Text.All(Function(c) Char.IsDigit(c))
        End Sub

        Private Sub ReturnDialogOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           ReturnLineComboBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, TransactionHistoryViewModel)
            If vm IsNot Nothing Then
                If vm.IsReturnDialogVisible Then
                    If e.Key = System.Windows.Input.Key.Escape Then
                        If vm.CancelReturnCommand.CanExecute(Nothing) Then
                            vm.CancelReturnCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    ElseIf e.Key = System.Windows.Input.Key.Enter Then
                        If vm.ProcessReturnCommand.CanExecute(Nothing) Then
                            vm.ProcessReturnCommand.Execute(Nothing)
                        End If
                        e.Handled = True
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
