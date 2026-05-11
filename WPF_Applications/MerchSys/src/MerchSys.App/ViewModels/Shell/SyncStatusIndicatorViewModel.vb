Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Sync

Namespace ViewModels.Shell

    Public Enum IndicatorSeverity
        Healthy = 0
        Idle = 1
        Warning = 2
        Critical = 3
    End Enum

    ''' <summary>
    ''' Presents the current sync connectivity state as a compact status-bar indicator.
    ''' Subscribes to INotificationService.SyncStatusChanged and derives DisplayLabel,
    ''' Severity, and LastSyncDisplay from each transition.
    ''' The 10-minute "sync delayed" warning threshold is a starting heuristic from INFRA-10;
    ''' if it needs to be configurable, a future plan should promote it to appsettings.json.
    ''' </summary>
    Public Class SyncStatusIndicatorViewModel
        Inherits ObservableObject
        Implements IDisposable

        Private ReadOnly _notifications As INotificationService
        Private ReadOnly _timer As DispatcherTimer
        Private _lastKnownStatus As SyncStatus
        Private _disposed As Boolean

        Private _displayLabel As String = "Offline"
        Public Property DisplayLabel As String
            Get
                Return _displayLabel
            End Get
            Private Set(value As String)
                SetProperty(_displayLabel, value)
            End Set
        End Property

        Private _severity As IndicatorSeverity = IndicatorSeverity.Idle
        Public Property Severity As IndicatorSeverity
            Get
                Return _severity
            End Get
            Private Set(value As IndicatorSeverity)
                SetProperty(_severity, value)
            End Set
        End Property

        Private _lastSyncDisplay As String = "never"
        Public Property LastSyncDisplay As String
            Get
                Return _lastSyncDisplay
            End Get
            Private Set(value As String)
                SetProperty(_lastSyncDisplay, value)
            End Set
        End Property

        Private _tooltipText As String = "Sync status: Offline"
        Public Property TooltipText As String
            Get
                Return _tooltipText
            End Get
            Private Set(value As String)
                SetProperty(_tooltipText, value)
            End Set
        End Property

        Public Sub New(notifications As INotificationService)
            _notifications = notifications
            _lastKnownStatus = notifications.CurrentSyncStatus

            AddHandler _notifications.SyncStatusChanged, AddressOf OnSyncStatusChanged
            Recompute(_lastKnownStatus, _notifications.LastSuccessfulPushAt)

            ' 30 s keeps the relative timestamp ticking between events without being noisy
            _timer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(30)}
            AddHandler _timer.Tick, AddressOf OnTimerTick
            _timer.Start()
        End Sub

        Private Sub OnSyncStatusChanged(sender As Object, newStatus As SyncStatus)
            _lastKnownStatus = newStatus
            Recompute(newStatus, _notifications.LastSuccessfulPushAt)
        End Sub

        Private Sub OnTimerTick(sender As Object, e As EventArgs)
            Recompute(_lastKnownStatus, _notifications.LastSuccessfulPushAt)
        End Sub

        Private Sub Recompute(status As SyncStatus, lastPush As Nullable(Of DateTimeOffset))
            Dim delayed = lastPush.HasValue AndAlso
                          (DateTimeOffset.Now - lastPush.Value).TotalMinutes > 10

            Select Case status
                Case SyncStatus.Online
                    If delayed Then
                        DisplayLabel = "Online (sync delayed)"
                        Severity = IndicatorSeverity.Warning
                    Else
                        DisplayLabel = "Online"
                        Severity = IndicatorSeverity.Healthy
                    End If
                Case SyncStatus.Syncing
                    DisplayLabel = "Syncing…"
                    Severity = IndicatorSeverity.Idle
                Case SyncStatus.Probing
                    DisplayLabel = "Syncing…"
                    Severity = IndicatorSeverity.Idle
                Case SyncStatus.Offline
                    DisplayLabel = "Offline"
                    Severity = IndicatorSeverity.Idle
                Case SyncStatus.Error
                    DisplayLabel = "Sync error"
                    Severity = IndicatorSeverity.Critical
                Case Else
                    DisplayLabel = "Offline"
                    Severity = IndicatorSeverity.Idle
            End Select

            LastSyncDisplay = FormatRelative(lastPush)
            TooltipText = BuildTooltip(status, lastPush)
        End Sub

        Private Shared Function FormatRelative(ts As Nullable(Of DateTimeOffset)) As String
            If Not ts.HasValue Then Return "never"
            Dim elapsed = DateTimeOffset.Now - ts.Value
            If elapsed.TotalSeconds < 60 Then Return "just now"
            If elapsed.TotalMinutes < 60 Then Return $"{CInt(Math.Floor(elapsed.TotalMinutes))} min ago"
            If elapsed.TotalHours < 24 Then Return $"{CInt(Math.Floor(elapsed.TotalHours))} hr ago"
            If elapsed.TotalDays < 2 Then Return "yesterday"
            Return $"{CInt(Math.Floor(elapsed.TotalDays))} days ago"
        End Function

        Private Shared Function BuildTooltip(status As SyncStatus, lastPush As Nullable(Of DateTimeOffset)) As String
            Dim lastLine = If(lastPush.HasValue,
                              $"Last successful push: {lastPush.Value:yyyy-MM-dd HH:mm}",
                              "Last successful push: never")
            Return $"Sync status: {status}{Environment.NewLine}{lastLine}"
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposed Then Return
            _disposed = True
            _timer.Stop()
            RemoveHandler _timer.Tick, AddressOf OnTimerTick
            RemoveHandler _notifications.SyncStatusChanged, AddressOf OnSyncStatusChanged
        End Sub

    End Class

End Namespace
