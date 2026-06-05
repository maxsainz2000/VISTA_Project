Imports System.Windows.Controls
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    Partial Class DailySummaryView
        Inherits UserControl

        Public Sub New(viewModel As DailySummaryViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        ' Auto-load data when the view is first shown.
        Private Sub DailySummaryView_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) _
            Handles Me.Loaded

            Dim vm = TryCast(DataContext, DailySummaryViewModel)
            If vm Is Nothing Then Return

            Dispatcher.InvokeAsync(Async Function()
                                       Await vm.LoadCommand.ExecuteAsync(Nothing)
                                   End Function)
        End Sub

        Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
            Dim vm = TryCast(DataContext, DailySummaryViewModel)
            If vm IsNot Nothing Then
                If e.Key = System.Windows.Input.Key.Enter Then
                    If vm.LoadCommand.CanExecute(Nothing) Then
                        vm.LoadCommand.Execute(Nothing)
                    End If
                    e.Handled = True
                End If
            End If
        End Sub

    End Class

End Namespace
