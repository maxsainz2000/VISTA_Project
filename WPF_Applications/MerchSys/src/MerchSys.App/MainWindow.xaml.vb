Imports System.Windows.Input
Imports MerchSys.App.Services
Imports MerchSys.App.ViewModels
Imports MerchSys.App.Views.Shell

Class MainWindow

    Private ReadOnly _viewModel As MainWindowViewModel
    Private ReadOnly _placementService As WindowPlacementService
    Private _pendingMaximize As Boolean = False

    Public Sub New(viewModel As MainWindowViewModel,
                   activityRail As ActivityRail,
                   moduleDetailPanel As ModuleDetailPanel,
                   placementService As WindowPlacementService)
        InitializeComponent()
        _viewModel = viewModel
        _placementService = placementService
        DataContext = viewModel
        ActivityRailSlot.Content = activityRail
        ModuleDetailSlot.Content = moduleDetailPanel

        ' Restore saved placement or fall back to first-run defaults (CenterScreen / Maximized).
        ' Bounds are set before Show() so WindowStartupLocation takes effect correctly.
        ' WindowState.Maximized is deferred to Loaded so RestoreBounds reflects the normal
        ' bounds set here (enabling un-maximize to a sensible size).
        Dim placement = _placementService.LoadPlacement()
        If placement IsNot Nothing Then
            WindowStartupLocation = WindowStartupLocation.Manual
            Left = placement.Left
            Top = placement.Top
            Width = placement.Width
            Height = placement.Height
            _pendingMaximize = placement.IsMaximized
        Else
            ' First run or off-screen fallback
            WindowStartupLocation = WindowStartupLocation.CenterScreen
            _pendingMaximize = True
        End If
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Apply Maximized state here so the normal bounds set in the constructor become
        ' RestoreBounds — un-maximizing will return to those exact dimensions.
        If _pendingMaximize Then
            WindowState = WindowState.Maximized
        End If
        _viewModel.NavigateToDefault()
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        _placementService.SavePlacement(Me)
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
