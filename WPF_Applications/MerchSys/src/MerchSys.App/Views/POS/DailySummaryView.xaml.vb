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

    End Class

End Namespace
