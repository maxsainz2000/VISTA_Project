Imports MerchSys.App.ViewModels

Class MainWindow

    Private ReadOnly _viewModel As MainWindowViewModel

    Public Sub New(viewModel As MainWindowViewModel)
        InitializeComponent()
        _viewModel = viewModel
        DataContext = viewModel
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        _viewModel.NavigateToDefault()
    End Sub

End Class
