Imports System.Windows.Input
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

    Private Sub MainWindow_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        If _viewModel IsNot Nothing AndAlso _viewModel.IsCommandPaletteOpen Then
            Select Case e.Key
                Case Key.Escape
                    _viewModel.CommandPalette.CloseCommand.Execute(Nothing)
                    e.Handled = True
                Case Key.Up
                    _viewModel.CommandPalette.MoveSelectionUp()
                    If CommandPaletteControl IsNot Nothing Then
                        CommandPaletteControl.ScrollSelectedIndexIntoView()
                    End If
                    e.Handled = True
                Case Key.Down
                    _viewModel.CommandPalette.MoveSelectionDown()
                    If CommandPaletteControl IsNot Nothing Then
                        CommandPaletteControl.ScrollSelectedIndexIntoView()
                    End If
                    e.Handled = True
                Case Key.Enter
                    _viewModel.CommandPalette.ExecuteSelectedCommand.Execute(Nothing)
                    e.Handled = True
            End Select
        End If
    End Sub

End Class
