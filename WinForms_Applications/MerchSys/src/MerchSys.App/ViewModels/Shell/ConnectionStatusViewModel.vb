Imports System.Windows.Media
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.App.Services

Namespace ViewModels.Shell

    ''' <summary>
    ''' Binds the IConnectionHealthMonitor state machine to the ConnectionStatusIndicator badge.
    ''' Exposes display-ready properties so the XAML needs no code-behind logic.
    ''' </summary>
    Public Class ConnectionStatusViewModel
        Inherits ObservableObject

        Private ReadOnly _monitor As IConnectionHealthMonitor
        Private _state As ConnectionState

        Public Sub New(monitor As IConnectionHealthMonitor)
            _monitor = monitor
            _state = monitor.CurrentState
            AddHandler _monitor.StateChanged, AddressOf OnStateChanged
            RetryCommand = New AsyncRelayCommand(AddressOf OnRetryAsync)
        End Sub

        Public ReadOnly Property RetryCommand As AsyncRelayCommand

        ''' <summary>Human-readable label for the badge (Online / Reconnecting… / Offline).</summary>
        Public ReadOnly Property StatusText As String
            Get
                Select Case _state
                    Case ConnectionState.Online : Return "Online"
                    Case ConnectionState.Reconnecting : Return "Reconnecting…"
                    Case Else : Return "Offline"
                End Select
            End Get
        End Property

        ''' <summary>Badge background colour matching the current state.</summary>
        Public ReadOnly Property IndicatorBrush As SolidColorBrush
            Get
                Select Case _state
                    Case ConnectionState.Online
                        Return New SolidColorBrush(Color.FromRgb(&H27, &HAE, &H60))
                    Case ConnectionState.Reconnecting
                        Return New SolidColorBrush(Color.FromRgb(&HF3, &H9C, &H12))
                    Case Else
                        Return New SolidColorBrush(Color.FromRgb(&HE7, &H4C, &H3C))
                End Select
            End Get
        End Property

        ''' <summary>True when the Retry button should be shown (Offline state only).</summary>
        Public ReadOnly Property IsRetryVisible As Boolean
            Get
                Return _state = ConnectionState.Offline
            End Get
        End Property

        ''' <summary>True when the spinner animation should run (Reconnecting state only).</summary>
        Public ReadOnly Property IsReconnecting As Boolean
            Get
                Return _state = ConnectionState.Reconnecting
            End Get
        End Property

        Private Sub OnStateChanged(sender As Object, e As ConnectionStateChangedEventArgs)
            _state = e.NewState
            OnPropertyChanged(NameOf(StatusText))
            OnPropertyChanged(NameOf(IndicatorBrush))
            OnPropertyChanged(NameOf(IsRetryVisible))
            OnPropertyChanged(NameOf(IsReconnecting))
        End Sub

        Private Async Function OnRetryAsync() As Task
            Await _monitor.RetryNowAsync()
        End Function

    End Class

End Namespace
