Imports System.Windows.Input
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Enums

Namespace ViewModels.Shell

    Public Class ShortcutsOverlayViewModel
        Inherits ObservableObject

        Private ReadOnly _sessionService As ISessionService
        Private _isOpen As Boolean

        Public Property IsOpen As Boolean
            Get
                Return _isOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isOpen, value) Then
                    ' On open, we notify changes if role changed, though it's static per session.
                    OnPropertyChanged(NameOf(IsDeveloper))
                End If
            End Set
        End Property

        Public ReadOnly Property IsDeveloper As Boolean
            Get
                Return _sessionService.CurrentRole = UserRole.Developer
            End Get
        End Property

        Public ReadOnly Property OpenCommand As RelayCommand
        Public ReadOnly Property CloseCommand As RelayCommand
        Public ReadOnly Property ToggleCommand As RelayCommand

        Public Sub New(sessionService As ISessionService)
            _sessionService = sessionService
            _isOpen = False

            OpenCommand = New RelayCommand(Sub() IsOpen = True)
            CloseCommand = New RelayCommand(Sub() IsOpen = False)
            ToggleCommand = New RelayCommand(Sub() IsOpen = Not IsOpen)
        End Sub

    End Class

End Namespace
