Imports System.Windows

Namespace Services

    Public Class WindowPlacement
        Public Property Left As Double
        Public Property Top As Double
        Public Property Width As Double
        Public Property Height As Double
        Public Property IsMaximized As Boolean
    End Class

    ''' <summary>
    ''' Saves and restores per-laptop window placement (size, position, maximized state).
    ''' Shared state lives in UiSettingsStore alongside the theme key so both services
    ''' write to the same ui-settings.json without clobbering each other.
    ''' </summary>
    Public Class WindowPlacementService

        Private ReadOnly _store As UiSettingsStore

        Public Sub New(store As UiSettingsStore)
            _store = store
        End Sub

        ''' <summary>
        ''' Returns the saved placement with off-screen bounds clamped to the current virtual
        ''' screen, or Nothing if no placement is saved or the saved position is off-screen.
        ''' Callers treat Nothing as "use first-run defaults (CenterScreen / Maximized)".
        ''' </summary>
        Public Function LoadPlacement() As WindowPlacement
            If Double.IsNaN(_store.WindowLeft) OrElse Double.IsNaN(_store.WindowTop) Then
                Return Nothing  ' no saved placement yet
            End If

            Dim savedLeft = _store.WindowLeft
            Dim savedTop = _store.WindowTop
            Dim savedWidth = Math.Max(_store.WindowWidth, 900.0)   ' respect MinWidth
            Dim savedHeight = Math.Max(_store.WindowHeight, 600.0)  ' respect MinHeight

            ' Validate against the current virtual screen so a disconnected/smaller monitor
            ' cannot leave the window unreachable.  Require at least 100×30 px to be visible.
            Dim vsLeft = SystemParameters.VirtualScreenLeft
            Dim vsTop = SystemParameters.VirtualScreenTop
            Dim vsRight = vsLeft + SystemParameters.VirtualScreenWidth
            Dim vsBottom = vsTop + SystemParameters.VirtualScreenHeight

            Dim windowRight = savedLeft + savedWidth
            Dim windowBottom = savedTop + savedHeight

            Dim visibleH = Math.Min(windowRight, vsRight) - Math.Max(savedLeft, vsLeft)
            Dim visibleV = Math.Min(windowBottom, vsBottom) - Math.Max(savedTop, vsTop)

            If visibleH < 100.0 OrElse visibleV < 30.0 Then
                Return Nothing  ' off-screen — fall back to first-run defaults
            End If

            ' Clamp so title bar and resize handles remain reachable
            Dim clampedLeft = Math.Max(vsLeft, Math.Min(savedLeft, vsRight - 100.0))
            Dim clampedTop = Math.Max(vsTop, Math.Min(savedTop, vsBottom - 30.0))
            Dim clampedWidth = Math.Min(savedWidth, SystemParameters.VirtualScreenWidth)
            Dim clampedHeight = Math.Min(savedHeight, SystemParameters.VirtualScreenHeight)

            Return New WindowPlacement() With {
                .Left = clampedLeft,
                .Top = clampedTop,
                .Width = clampedWidth,
                .Height = clampedHeight,
                .IsMaximized = _store.WindowMaximized
            }
        End Function

        ''' <summary>
        ''' Captures the window's current placement and persists it.  When the window is
        ''' maximized, RestoreBounds is saved (not the screen-filling bounds) so that
        ''' un-maximizing always returns to a sensible normal size.
        ''' </summary>
        Public Sub SavePlacement(window As Window)
            Dim isMax = (window.WindowState = WindowState.Maximized)
            Dim bounds As Rect

            If isMax Then
                bounds = window.RestoreBounds
            Else
                bounds = New Rect(window.Left, window.Top, window.Width, window.Height)
            End If

            _store.WindowLeft = bounds.Left
            _store.WindowTop = bounds.Top
            _store.WindowWidth = bounds.Width
            _store.WindowHeight = bounds.Height
            _store.WindowMaximized = isMax
            _store.Save()
        End Sub

    End Class

End Namespace
