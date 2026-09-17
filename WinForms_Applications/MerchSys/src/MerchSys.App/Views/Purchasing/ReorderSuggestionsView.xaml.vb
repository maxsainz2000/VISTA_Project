Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' Reorder Suggestions screen — two-tab layout for reviewing suggestions
    ''' (generate / accept / dismiss) and editing per-product reorder configuration.
    ''' DataContext is resolved from DI via constructor injection.
    ''' </summary>
    Partial Class ReorderSuggestionsView
        Inherits UserControl

        Public Sub New(viewModel As ReorderSuggestionsViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub EditConfigDialogOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CType(e.NewValue, Boolean) Then
                Dispatcher.BeginInvoke(Sub()
                                           MinThresholdTextBox.Focus()
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            End If
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            Dim vm = TryCast(DataContext, ReorderSuggestionsViewModel)
            If vm IsNot Nothing AndAlso vm.IsEditDialogOpen Then
                If e.Key = Key.Escape Then
                    If vm.CancelEditCommand.CanExecute(Nothing) Then
                        vm.CancelEditCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                ElseIf e.Key = Key.Enter Then
                    If vm.SaveConfigCommand.CanExecute(Nothing) Then
                        vm.SaveConfigCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                End If
            End If
        End Sub

    End Class

End Namespace
