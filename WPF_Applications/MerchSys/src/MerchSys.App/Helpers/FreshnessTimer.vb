Imports System.Windows.Threading

Namespace Helpers

    ''' <summary>
    ''' Provides a single shared DispatcherTimer ticking source (approx. every 30 seconds)
    ''' to allow multiple data freshness indicators to age in lockstep without spawning
    ''' multiple individual timers.
    ''' </summary>
    Public Class FreshnessTimer

        Private Shared ReadOnly _timer As DispatcherTimer
        Public Shared Event Tick As EventHandler

        Shared Sub New()
            _timer = New DispatcherTimer(DispatcherPriority.Background)
            _timer.Interval = TimeSpan.FromSeconds(30)
            AddHandler _timer.Tick, AddressOf OnTimerTick
            _timer.Start()
        End Sub

        Private Shared Sub OnTimerTick(sender As Object, e As EventArgs)
            RaiseEvent Tick(Nothing, EventArgs.Empty)
        End Sub

    End Class

End Namespace
