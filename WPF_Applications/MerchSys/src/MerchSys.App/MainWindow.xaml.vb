Imports MerchSys.App.ViewModels
Imports MerchSys.App.Views.Shell

Class MainWindow

    Private ReadOnly _viewModel As MainWindowViewModel

    Public Sub New(viewModel As MainWindowViewModel,
                   activityRail As ActivityRail,
                   moduleDetailPanel As ModuleDetailPanel)
        InitializeComponent()
        _viewModel = viewModel
        DataContext = viewModel
        ' Slot the DI-resolved shell components into their named ContentControl placeholders.
        ActivityRailSlot.Content = activityRail
        ModuleDetailSlot.Content = moduleDetailPanel
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        _viewModel.NavigateToDefault()
    End Sub

End Class
