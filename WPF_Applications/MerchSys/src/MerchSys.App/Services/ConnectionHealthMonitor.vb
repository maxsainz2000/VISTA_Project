Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging
Imports MySqlConnector
Imports System.Threading

Namespace Services

    ''' <summary>
    ''' Concrete connection health monitor using MySqlConnector SELECT 1 probes.
    ''' Runs on a background task; transitions are reported to the UI thread via WPF Dispatcher.
    ''' </summary>
    Public Class ConnectionHealthMonitor
        Implements IConnectionHealthMonitor
        Implements IDisposable

        Private ReadOnly _connStr As String
        Private ReadOnly _logger As ILogger(Of ConnectionHealthMonitor)
        Private ReadOnly _intervalSeconds As Integer
        Private ReadOnly _backoffSeconds As Integer()
        Private ReadOnly _maxRetries As Integer
        Private ReadOnly _retrySignal As New SemaphoreSlim(0, 1)

        Private _cts As CancellationTokenSource
        Private _currentState As ConnectionState = ConnectionState.Offline

        Public Event StateChanged As EventHandler(Of ConnectionStateChangedEventArgs) Implements IConnectionHealthMonitor.StateChanged

        Public ReadOnly Property CurrentState As ConnectionState Implements IConnectionHealthMonitor.CurrentState
            Get
                Return _currentState
            End Get
        End Property

        Public Sub New(connStr As String, config As IConfiguration,
                       logger As ILogger(Of ConnectionHealthMonitor))
            _connStr = connStr
            _logger = logger
            _intervalSeconds = config.GetValue(Of Integer)("Connection:HealthCheckIntervalSeconds", 15)
            _maxRetries = config.GetValue(Of Integer)("Connection:MaxRetries", 5)
            Dim backoff = config.GetSection("Connection:RetryBackoffSeconds").Get(Of Integer())()
            _backoffSeconds = If(backoff IsNot Nothing AndAlso backoff.Length > 0,
                                 backoff,
                                 New Integer() {2, 4, 8, 16, 32})
        End Sub

        Public Sub Start() Implements IConnectionHealthMonitor.Start
            If _cts IsNot Nothing Then Return
            _cts = New CancellationTokenSource()
            Task.Run(AddressOf RunLoopAsync)
        End Sub

        Public Sub [Stop]() Implements IConnectionHealthMonitor.[Stop]
            _cts?.Cancel()
            _cts = Nothing
        End Sub

        Public Function RetryNowAsync() As Task Implements IConnectionHealthMonitor.RetryNowAsync
            If _currentState = ConnectionState.Online Then Return Task.CompletedTask
            Try
                _retrySignal.Release()
            Catch ex As SemaphoreFullException
                ' Signal already pending — safe to ignore
            End Try
            Return Task.CompletedTask
        End Function

        Private Async Function RunLoopAsync() As Task
            Dim ct = _cts.Token

            Dim initialSuccess = Await PingAsync()
            If initialSuccess Then
                TransitionTo(ConnectionState.Online)
            Else
                _logger.LogWarning("Initial health check failed — transitioning to Reconnecting.")
                TransitionTo(ConnectionState.Reconnecting)
                Await RunReconnectLoopAsync(ct)
                If ct.IsCancellationRequested Then Return
                If _currentState <> ConnectionState.Online Then Return
            End If

            Do While Not ct.IsCancellationRequested
                Dim cancelled = False
                Try
                    Await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), ct)
                Catch ex As OperationCanceledException
                    cancelled = True
                End Try
                If cancelled Then Return

                Dim success = Await PingAsync()
                If Not success Then
                    _logger.LogWarning("Health check failed — transitioning to Reconnecting.")
                    TransitionTo(ConnectionState.Reconnecting)
                    Await RunReconnectLoopAsync(ct)
                    If ct.IsCancellationRequested Then Return
                    If _currentState <> ConnectionState.Online Then Return
                End If
            Loop
        End Function

        Private Async Function RunReconnectLoopAsync(ct As CancellationToken) As Task
            For retryIndex As Integer = 0 To _maxRetries - 1
                If ct.IsCancellationRequested Then Return

                Dim delay = If(retryIndex < _backoffSeconds.Length,
                               _backoffSeconds(retryIndex),
                               _backoffSeconds(_backoffSeconds.Length - 1))

                Dim cancelled = False
                Try
                    Await _retrySignal.WaitAsync(TimeSpan.FromSeconds(delay), ct)
                Catch ex As OperationCanceledException
                    cancelled = True
                End Try
                If cancelled Then Return

                _logger.LogWarning("Reconnect attempt {Attempt}/{MaxRetries}...",
                                   retryIndex + 1, _maxRetries)
                If Await PingAsync() Then
                    _logger.LogInformation("Reconnect attempt {Attempt} succeeded — connection Online.",
                                           retryIndex + 1)
                    TransitionTo(ConnectionState.Online)
                    Return
                End If
            Next

            _logger.LogError("All {MaxRetries} reconnect retries exhausted — transitioning to Offline.",
                             _maxRetries)
            TransitionTo(ConnectionState.Offline)

            ' Wait for manual RetryNow signal
            Do While Not ct.IsCancellationRequested
                Dim cancelled = False
                Try
                    Await _retrySignal.WaitAsync(ct)
                Catch ex As OperationCanceledException
                    cancelled = True
                End Try
                If cancelled Then Return

                _logger.LogWarning("Manual retry probe in progress...")
                If Await PingAsync() Then
                    _logger.LogInformation("Manual retry succeeded — connection Online.")
                    TransitionTo(ConnectionState.Online)
                    Return
                End If
                _logger.LogWarning("Manual retry probe failed.")
            Loop
        End Function

        Private Sub TransitionTo(newState As ConnectionState)
            If _currentState = newState Then Return
            _currentState = newState
            _logger.LogInformation("Connection state → {State}.", newState)

            Dim dispatcher = System.Windows.Application.Current?.Dispatcher
            If dispatcher Is Nothing OrElse dispatcher.HasShutdownStarted Then Return

            If dispatcher.CheckAccess() Then
                RaiseEvent StateChanged(Me, New ConnectionStateChangedEventArgs(newState))
            Else
                dispatcher.Invoke(Sub()
                                      RaiseEvent StateChanged(Me, New ConnectionStateChangedEventArgs(newState))
                                  End Sub)
            End If
        End Sub

        Private Async Function PingAsync() As Task(Of Boolean)
            Try
                Using conn As New MySqlConnection(_connStr)
                    Await conn.OpenAsync()
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT 1"
                        cmd.CommandTimeout = 3
                        Await cmd.ExecuteScalarAsync()
                    End Using
                    Return True
                End Using
            Catch
                Return False
            End Try
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
            _cts?.Dispose()
            _retrySignal.Dispose()
        End Sub

    End Class

End Namespace
