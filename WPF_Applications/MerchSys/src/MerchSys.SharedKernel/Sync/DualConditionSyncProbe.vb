Imports System.Diagnostics
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options

Namespace Sync

    ''' <summary>
    ''' Implements the dual-condition connectivity rule from concepts/offline-first-sync.md:
    ''' Condition A — network adapter must report available; Condition B — central server
    ''' must accept a TCP connection within the configured timeout.
    ''' </summary>
    Public Class DualConditionSyncProbe
        Implements ISyncProbe

        Private ReadOnly _settings As IOptionsMonitor(Of SyncSettings)
        Private ReadOnly _logger As ILogger(Of DualConditionSyncProbe)

        Public Sub New(settings As IOptionsMonitor(Of SyncSettings),
                       logger As ILogger(Of DualConditionSyncProbe))
            _settings = settings
            _logger = logger
        End Sub

        ''' <summary>
        ''' Evaluates both connectivity conditions and returns a result within ProbeTimeoutMs.
        ''' Logs only the exception type on failure, not the stack trace.
        ''' </summary>
        Public Async Function ProbeAsync() As Task(Of SyncProbeResult) Implements ISyncProbe.ProbeAsync
            Dim result = New SyncProbeResult() With {
                .ProbedAt = DateTime.UtcNow,
                .NetworkAvailable = NetworkInterface.GetIsNetworkAvailable()
            }

            If Not result.NetworkAvailable Then
                Return result
            End If

            Dim cfg = _settings.CurrentValue
            Dim sw = Stopwatch.StartNew()

            Try
                Using cts = New CancellationTokenSource(cfg.ProbeTimeoutMs)
                    Using client As New TcpClient()
                        Await client.ConnectAsync(cfg.CentralServerHost, cfg.CentralServerPort, cts.Token)
                        sw.Stop()
                        result.ServerReachable = True
                        result.LatencyMs = CInt(sw.ElapsedMilliseconds)
                    End Using
                End Using
            Catch ex As OperationCanceledException
                _logger.LogWarning("Sync probe timed out after {Ms}ms ({Type})",
                                   cfg.ProbeTimeoutMs, ex.GetType().Name)
                result.[Error] = ex.GetType().Name
            Catch ex As Exception
                _logger.LogWarning("Sync probe failed ({Type})", ex.GetType().Name)
                result.[Error] = ex.GetType().Name
            End Try

            Return result
        End Function

    End Class

End Namespace
