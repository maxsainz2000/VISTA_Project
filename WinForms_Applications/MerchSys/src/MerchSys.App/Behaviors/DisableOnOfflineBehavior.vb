Imports System.Runtime.CompilerServices
Imports MerchSys.App.Services

Namespace Behaviors

    ''' <summary>
    ''' Attached behavior that disables any FrameworkElement when the central DB connection
    ''' is not Online. Apply to all mutating buttons (Save PO, Issue OR, Add Vendor, etc.).
    '''
    ''' Usage in XAML:
    '''   xmlns:behaviors="clr-namespace:MerchSys.App.Behaviors"
    '''   behaviors:DisableOnOfflineBehavior.IsDisabledWhenOffline="True"
    ''' </summary>
    Public NotInheritable Class DisableOnOfflineBehavior

        ' ── Per-element state ────────────────────────────────────────────────────

        Private NotInheritable Class DisableHandler
            Private ReadOnly _element As FrameworkElement
            Private ReadOnly _monitor As IConnectionHealthMonitor

            Public Sub New(el As FrameworkElement, mon As IConnectionHealthMonitor)
                _element = el
                _monitor = mon
            End Sub

            Public Sub OnStateChanged(sender As Object, e As ConnectionStateChangedEventArgs)
                ApplyState(e.NewState)
            End Sub

            Public Sub Attach()
                AddHandler _monitor.StateChanged, AddressOf OnStateChanged
                ApplyState(_monitor.CurrentState)
            End Sub

            Public Sub Detach()
                RemoveHandler _monitor.StateChanged, AddressOf OnStateChanged
                _element.ClearValue(UIElement.IsEnabledProperty)
            End Sub

            Private Sub ApplyState(state As ConnectionState)
                If state = ConnectionState.Online Then
                    _element.ClearValue(UIElement.IsEnabledProperty)
                Else
                    _element.IsEnabled = False
                End If
            End Sub
        End Class

        ' Keyed by element; allows GC when element is collected
        Private Shared ReadOnly _handlers As New ConditionalWeakTable(Of FrameworkElement, DisableHandler)()

        ' ── Attached property ────────────────────────────────────────────────────

        Public Shared ReadOnly IsDisabledWhenOfflineProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "IsDisabledWhenOffline",
                GetType(Boolean),
                GetType(DisableOnOfflineBehavior),
                New PropertyMetadata(False, AddressOf OnPropertyChanged))

        Public Shared Function GetIsDisabledWhenOffline(obj As DependencyObject) As Boolean
            Return CBool(obj.GetValue(IsDisabledWhenOfflineProperty))
        End Function

        Public Shared Sub SetIsDisabledWhenOffline(obj As DependencyObject, value As Boolean)
            obj.SetValue(IsDisabledWhenOfflineProperty, value)
        End Sub

        ' ── Property change handler ───────────────────────────────────────────────

        Private Shared Sub OnPropertyChanged(d As DependencyObject,
                                             e As DependencyPropertyChangedEventArgs)
            Dim element = TryCast(d, FrameworkElement)
            If element Is Nothing Then Return

            If CBool(e.NewValue) Then
                AddHandler element.Loaded, AddressOf OnLoaded
                AddHandler element.Unloaded, AddressOf OnUnloaded
                If element.IsLoaded Then ConnectElement(element)
            Else
                RemoveHandler element.Loaded, AddressOf OnLoaded
                RemoveHandler element.Unloaded, AddressOf OnUnloaded
                DetachElement(element)
            End If
        End Sub

        Private Shared Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            ConnectElement(TryCast(sender, FrameworkElement))
        End Sub

        Private Shared Sub OnUnloaded(sender As Object, e As RoutedEventArgs)
            DetachElement(TryCast(sender, FrameworkElement))
        End Sub

        Private Shared Sub ConnectElement(element As FrameworkElement)
            If element Is Nothing Then Return
            Dim monitor = ConnectionHealthMonitorLocator.Current
            If monitor Is Nothing Then Return

            Dim existing As DisableHandler = Nothing
            If _handlers.TryGetValue(element, existing) Then Return

            Dim handler = New DisableHandler(element, monitor)
            _handlers.Add(element, handler)
            handler.Attach()
        End Sub

        Private Shared Sub DetachElement(element As FrameworkElement)
            If element Is Nothing Then Return
            Dim handler As DisableHandler = Nothing
            If _handlers.TryGetValue(element, handler) Then
                handler.Detach()
                _handlers.Remove(element)
            End If
        End Sub

    End Class

End Namespace
