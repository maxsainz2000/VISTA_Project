Imports Microsoft.Extensions.Logging
Imports System.Windows.Input
Imports System.Windows.Threading

Namespace Services

    ''' <summary>
    ''' WPF singleton implementation of IIdleMonitor (OWASP DA2 — session inactivity detection).
    ''' Hooks InputManager.PreProcessInput to detect keyboard and pointer activity.
    ''' Clamps IdleTimeoutMinutes to the DA2-compliant range [15, 30].
    ''' </summary>
    Public Class WpfIdleMonitor
        Implements IIdleMonitor
        Implements IDisposable

        Private ReadOnly _options As IdleMonitorOptions
        Private ReadOnly _logger As ILogger(Of WpfIdleMonitor)
        Private ReadOnly _timer As DispatcherTimer
        Private ReadOnly _timeoutSeconds As Integer

        Private _lastActivityUtc As DateTime
        Private _warningRaised As Boolean
        Private _expiredRaised As Boolean
        Private _isRunning As Boolean
        Private _hooked As Boolean

        Public Sub New(options As IdleMonitorOptions, logger As ILogger(Of WpfIdleMonitor))
            _options = options
            _logger = logger

            Dim clamped = Math.Max(15, Math.Min(30, _options.IdleTimeoutMinutes))
            If clamped <> _options.IdleTimeoutMinutes Then
                _logger.LogWarning(
                    "Session:IdleTimeoutMinutes={Configured} is outside the DA2-compliant range [15, 30]. Clamped to {Clamped}.",
                    _options.IdleTimeoutMinutes, clamped)
            End If
            _timeoutSeconds = clamped * 60

            _timer = New DispatcherTimer() With {.Interval = TimeSpan.FromSeconds(1)}
            AddHandler _timer.Tick, AddressOf OnTimerTick
        End Sub

        Public ReadOnly Property IsRunning As Boolean Implements IIdleMonitor.IsRunning
            Get
                Return _isRunning
            End Get
        End Property

        Public ReadOnly Property IdleSeconds As Integer Implements IIdleMonitor.IdleSeconds
            Get
                If Not _isRunning Then Return 0
                Return CInt(Math.Floor((DateTime.UtcNow - _lastActivityUtc).TotalSeconds))
            End Get
        End Property

        Public ReadOnly Property RemainingSeconds As Integer Implements IIdleMonitor.RemainingSeconds
            Get
                If Not _isRunning Then Return 0
                Return Math.Max(0, _timeoutSeconds - IdleSeconds)
            End Get
        End Property

        Public Sub Start() Implements IIdleMonitor.Start
            If _isRunning Then Return
            _lastActivityUtc = DateTime.UtcNow
            _warningRaised = False
            _expiredRaised = False
            _isRunning = True
            If Not _hooked Then
                AddHandler InputManager.Current.PreProcessInput, AddressOf OnPreProcessInput
                _hooked = True
            End If
            _timer.Start()
        End Sub

        Public Sub [Stop]() Implements IIdleMonitor.[Stop]
            If Not _isRunning Then Return
            _isRunning = False
            _timer.Stop()
            If _hooked Then
                RemoveHandler InputManager.Current.PreProcessInput, AddressOf OnPreProcessInput
                _hooked = False
            End If
            _warningRaised = False
            _expiredRaised = False
        End Sub

        Public Sub RecordActivity() Implements IIdleMonitor.RecordActivity
            _lastActivityUtc = DateTime.UtcNow
            _warningRaised = False
            _expiredRaised = False
        End Sub

        Public Event WarningShown As EventHandler(Of IdleMonitorWarningEventArgs) Implements IIdleMonitor.WarningShown
        Public Event SessionExpired As EventHandler Implements IIdleMonitor.SessionExpired

        Private Sub OnPreProcessInput(sender As Object, e As PreProcessInputEventArgs)
            Dim inputArgs = e.StagingItem.Input
            If TypeOf inputArgs Is KeyEventArgs OrElse
               TypeOf inputArgs Is MouseEventArgs OrElse
               TypeOf inputArgs Is TouchEventArgs Then
                _lastActivityUtc = DateTime.UtcNow
            End If
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs)
            Dim remaining = RemainingSeconds

            If remaining = 0 AndAlso Not _expiredRaised Then
                _expiredRaised = True
                [Stop]()
                RaiseEvent SessionExpired(Me, EventArgs.Empty)
            ElseIf remaining <= _options.WarningLeadSeconds AndAlso remaining > 0 AndAlso Not _warningRaised Then
                _warningRaised = True
                RaiseEvent WarningShown(Me, New IdleMonitorWarningEventArgs() With {.RemainingSeconds = remaining})
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
        End Sub

    End Class

#If DEBUG Then

    ''' <summary>
    ''' No-op idle monitor for DEBUG builds when VISTA_DISABLE_IDLE_TIMEOUT=1.
    ''' Start/Stop/RecordActivity do nothing; events are never raised.
    ''' Must not ship to release builds — guarded by #If DEBUG.
    ''' </summary>
    Friend Class NoOpIdleMonitor
        Implements IIdleMonitor

        Public ReadOnly Property IsRunning As Boolean Implements IIdleMonitor.IsRunning
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IdleSeconds As Integer Implements IIdleMonitor.IdleSeconds
            Get
                Return 0
            End Get
        End Property

        Public ReadOnly Property RemainingSeconds As Integer Implements IIdleMonitor.RemainingSeconds
            Get
                Return 0
            End Get
        End Property

        Public Sub Start() Implements IIdleMonitor.Start
        End Sub

        Public Sub [Stop]() Implements IIdleMonitor.[Stop]
        End Sub

        Public Sub RecordActivity() Implements IIdleMonitor.RecordActivity
        End Sub

        Public Event WarningShown As EventHandler(Of IdleMonitorWarningEventArgs) Implements IIdleMonitor.WarningShown
        Public Event SessionExpired As EventHandler Implements IIdleMonitor.SessionExpired

    End Class

#End If

End Namespace
