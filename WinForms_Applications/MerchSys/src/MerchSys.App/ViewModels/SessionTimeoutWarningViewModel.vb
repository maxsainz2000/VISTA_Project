Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input

Namespace ViewModels

    Public Class SessionTimeoutWarningViewModel
        Inherits ObservableObject

        Private _remainingSeconds As Integer
        Public Property RemainingSeconds As Integer
            Get
                Return _remainingSeconds
            End Get
            Set(value As Integer)
                If SetProperty(_remainingSeconds, value) Then
                    OnPropertyChanged(NameOf(CountdownDisplay))
                End If
            End Set
        End Property

        Public ReadOnly Property CountdownDisplay As String
            Get
                Dim mins = _remainingSeconds \ 60
                Dim secs = _remainingSeconds Mod 60
                Return $"{mins}:{secs:D2}"
            End Get
        End Property

        Public ReadOnly Property StaySignedInCommand As RelayCommand
        Public ReadOnly Property SignOutCommand As RelayCommand

        Public Event StayRequested As EventHandler
        Public Event SignOutRequested As EventHandler

        Public Sub New()
            StaySignedInCommand = New RelayCommand(AddressOf DoStaySignedIn)
            SignOutCommand = New RelayCommand(AddressOf DoSignOut)
        End Sub

        ''' <summary>Called each second by Application.xaml.vb while the dialog is open.</summary>
        Public Sub Tick(remaining As Integer)
            RemainingSeconds = remaining
        End Sub

        Private Sub DoStaySignedIn()
            RaiseEvent StayRequested(Me, EventArgs.Empty)
        End Sub

        Private Sub DoSignOut()
            RaiseEvent SignOutRequested(Me, EventArgs.Empty)
        End Sub

    End Class

End Namespace
