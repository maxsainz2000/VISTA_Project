Namespace Services

    ''' <summary>
    ''' Configuration for the idle monitor. OWASP DA2 specifies a 15–30 min inactivity timeout.
    ''' </summary>
    Public Class IdleMonitorOptions
        ''' <summary>Idle duration before the session is force-logged-out. DA2 spec: 15–30 min.</summary>
        Public Property IdleTimeoutMinutes As Integer = 20

        ''' <summary>Seconds of remaining idle before the warning dialog is shown. Default: 60.</summary>
        Public Property WarningLeadSeconds As Integer = 60
    End Class

    Public Class IdleMonitorWarningEventArgs
        Inherits EventArgs
        Public Property RemainingSeconds As Integer
    End Class

    ''' <summary>
    ''' Contract for the WPF idle/inactivity monitor (OWASP DA2).
    ''' Implementations track keyboard and pointer activity and raise events when the
    ''' configured 15–30 minute inactivity threshold is approached or exceeded.
    ''' </summary>
    Public Interface IIdleMonitor

        ''' <summary>True once Start has been called and Stop has not.</summary>
        ReadOnly Property IsRunning As Boolean

        ''' <summary>Whole seconds since the last recorded user activity.</summary>
        ReadOnly Property IdleSeconds As Integer

        ''' <summary>Whole seconds remaining before forced logout (0 if monitor is stopped).</summary>
        ReadOnly Property RemainingSeconds As Integer

        Sub Start()
        Sub [Stop]()

        ''' <summary>
        ''' Manually reset the idle clock. Called by the WPF input hook on any keyboard or
        ''' pointer event, and also by the warning dialog's "Stay signed in" button.
        ''' </summary>
        Sub RecordActivity()

        ''' <summary>
        ''' Raised once per warning window when RemainingSeconds first drops to
        ''' WarningLeadSeconds. Re-raised only after a subsequent RecordActivity.
        ''' </summary>
        Event WarningShown As EventHandler(Of IdleMonitorWarningEventArgs)

        ''' <summary>
        ''' Raised once when the idle threshold is reached. Subscriber must perform the logout.
        ''' </summary>
        Event SessionExpired As EventHandler

    End Interface

End Namespace
