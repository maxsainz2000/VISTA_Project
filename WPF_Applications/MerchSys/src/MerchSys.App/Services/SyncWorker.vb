Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace Services

    Public Class SyncWorker
        Inherits BackgroundService

        Private ReadOnly _probe As ISyncProbe
        Private ReadOnly _scopeFactory As IServiceScopeFactory
        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _settings As IOptionsMonitor(Of SyncSettings)
        Private ReadOnly _logger As ILogger(Of SyncWorker)

        Private _currentStatus As SyncStatus = SyncStatus.Offline

        Public Sub New(probe As ISyncProbe,
                       scopeFactory As IServiceScopeFactory,
                       notifications As INotificationService,
                       settings As IOptionsMonitor(Of SyncSettings),
                       logger As ILogger(Of SyncWorker))
            _probe = probe
            _scopeFactory = scopeFactory
            _notifications = notifications
            _settings = settings
            _logger = logger
        End Sub

        Protected Overrides Async Function ExecuteAsync(stoppingToken As CancellationToken) As Task
            _logger.LogInformation("SyncWorker started")

            While Not stoppingToken.IsCancellationRequested
                Try
                    Await Task.Delay(
                        TimeSpan.FromSeconds(_settings.CurrentValue.ProbeIntervalSeconds),
                        stoppingToken)
                Catch ex As OperationCanceledException
                    Exit While
                End Try

                If stoppingToken.IsCancellationRequested Then Exit While

                Dim previousStatus = _currentStatus
                SetStatus(SyncStatus.Probing)

                Try
                    Dim result = Await _probe.ProbeAsync()

                    If result.IsHealthy Then
                        If previousStatus = SyncStatus.Offline OrElse
                           previousStatus = SyncStatus.Probing Then
                            SetStatus(SyncStatus.Syncing)
                            Using scope = _scopeFactory.CreateScope()
                                Dim orchestrator = scope.ServiceProvider.
                                    GetRequiredService(Of SyncOrchestrator)()
                                Await orchestrator.RunAsync(stoppingToken)
                            End Using
                        End If
                        SetStatus(SyncStatus.Online)
                    Else
                        SetStatus(SyncStatus.Offline)
                    End If

                Catch ex As Exception When Not TypeOf ex Is OperationCanceledException
                    _logger.LogError(ex, "SyncWorker probe cycle failed")
                    SetStatus(SyncStatus.[Error])
                End Try
            End While

            _logger.LogInformation("SyncWorker stopped")
        End Function

        Private Sub SetStatus(status As SyncStatus)
            _currentStatus = status
            _notifications.NotifySyncStatusChanged(status)
        End Sub

    End Class

End Namespace
