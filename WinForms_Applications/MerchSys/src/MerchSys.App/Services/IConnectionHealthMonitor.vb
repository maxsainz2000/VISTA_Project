Namespace Services

    ''' <summary>Connection states for the central MariaDB instance.</summary>
    Public Enum ConnectionState
        ''' <summary>Last health-check succeeded. Mutation commands are enabled.</summary>
        Online
        ''' <summary>At least one check failed; backoff retries are in progress. Mutation commands disabled.</summary>
        Reconnecting
        ''' <summary>All retries exhausted. Mutation commands disabled. Manual retry available.</summary>
        Offline
    End Enum

    ''' <summary>Event arguments carrying the new connection state after a transition.</summary>
    Public Class ConnectionStateChangedEventArgs
        Inherits EventArgs

        Public ReadOnly Property NewState As ConnectionState

        Public Sub New(state As ConnectionState)
            NewState = state
        End Sub

    End Class

    ''' <summary>
    ''' Monitors live connectivity to the central MariaDB instance via periodic SELECT 1 probes.
    ''' State machine: Online → Reconnecting (first probe failure) → Offline (retries exhausted).
    ''' A successful probe at any stage returns the monitor to Online.
    ''' Events are raised on the WPF UI thread.
    ''' </summary>
    Public Interface IConnectionHealthMonitor

        ''' <summary>Current connection state.</summary>
        ReadOnly Property CurrentState As ConnectionState

        ''' <summary>
        ''' Fires on the WPF UI thread whenever CurrentState changes.
        ''' Subscribe to update UI elements and mutation-command enabled states.
        ''' </summary>
        Event StateChanged As EventHandler(Of ConnectionStateChangedEventArgs)

        ''' <summary>Starts the periodic health-check loop on a background task.</summary>
        Sub Start()

        ''' <summary>Cancels the health-check loop. Safe to call at application exit.</summary>
        Sub [Stop]()

        ''' <summary>
        ''' Forces an immediate probe regardless of the normal schedule or backoff delay.
        ''' No-op when the state is already Online. Intended for the manual Retry button.
        ''' </summary>
        Function RetryNowAsync() As Task

    End Interface

End Namespace
