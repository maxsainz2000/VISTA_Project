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
        ' Navigation is driven by HandleLoginSucceeded in Application.xaml.vb after RefreshNavigation();
        ' calling it here would race with that path on first login.
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        _placementService.SavePlacement(Me)
    End Sub

    Private Sub MainWindow_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        If _viewModel IsNot Nothing Then
            ' 1. If Shortcuts Overlay is open, Esc dismisses it
            If _viewModel.IsShortcutsOverlayOpen Then
                If e.Key = Key.Escape Then
                    _viewModel.ShortcutsOverlay.CloseCommand.Execute(Nothing)
                    e.Handled = True
                    Exit Sub
                End If
            End If

            ' 2. If Command Palette is open, handle its navigation/close keys
            If _viewModel.IsCommandPaletteOpen Then
                Select Case e.Key
                    Case Key.Escape
                        _viewModel.CommandPalette.CloseCommand.Execute(Nothing)
                        e.Handled = True
                        Exit Sub
                    Case Key.Up
                        _viewModel.CommandPalette.MoveSelectionUp()
                        If CommandPaletteControl IsNot Nothing Then
                            CommandPaletteControl.ScrollSelectedIndexIntoView()
                        End If
                        e.Handled = True
                        Exit Sub
                    Case Key.Down
                        _viewModel.CommandPalette.MoveSelectionDown()
                        If CommandPaletteControl IsNot Nothing Then
                            CommandPaletteControl.ScrollSelectedIndexIntoView()
                        End If
                        e.Handled = True
                        Exit Sub
                    Case Key.Enter
                        _viewModel.CommandPalette.ExecuteSelectedCommand.Execute(Nothing)
                        e.Handled = True
                        Exit Sub
                End Select
            End If

            ' 3. Handle toggling the Shortcuts Overlay via Ctrl+/ or ?
            If e.Key = Key.OemQuestion Then
                Dim hasControl = (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control
                Dim hasShift = (Keyboard.Modifiers And ModifierKeys.Shift) = ModifierKeys.Shift
                Dim hasAltOrWin = (Keyboard.Modifiers And (ModifierKeys.Alt Or ModifierKeys.Windows)) <> 0

                If Not hasAltOrWin Then
                    If hasControl AndAlso Not hasShift Then
                        ' Ctrl+/ triggers unconditionally
                        _viewModel.ShortcutsOverlay.ToggleCommand.Execute(Nothing)
                        e.Handled = True
                        Exit Sub
                    ElseIf hasShift AndAlso Not hasControl Then
                        ' ? (Shift+/) triggers only if focused control is not a text entry field
                        Dim focused = Keyboard.FocusedElement
                        Dim isTextInput = False
                        If focused IsNot Nothing Then
                            If TypeOf focused Is System.Windows.Controls.Primitives.TextBoxBase OrElse
                               TypeOf focused Is System.Windows.Controls.PasswordBox Then
                                isTextInput = True
                            End If
                        End If

                        If Not isTextInput Then
                            _viewModel.ShortcutsOverlay.ToggleCommand.Execute(Nothing)
                            e.Handled = True
                            Exit Sub
                        End If
                    End If
                End If
            End If
        End If
    End Sub

End Class
