Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.App.Services
Imports MerchSys.SharedKernel.Entities

Namespace ViewModels

    Public Class LoginViewModel
        Inherits ObservableObject

        Private ReadOnly _auth As IAuthenticationService
        Private ReadOnly _session As LoginSessionService

        ' Stored between LoginAsync (success) and ChangePasswordAndLoginAsync
        Private _pendingUser As UserAccount = Nothing
        Private _pendingPassword As String = String.Empty

        ' ── Commands (exposed as properties — WPF binding requires properties, not fields) ───

        Private ReadOnly _loginCommand As AsyncRelayCommand
        Public ReadOnly Property LoginCommand As AsyncRelayCommand
            Get
                Return _loginCommand
            End Get
        End Property

        Private ReadOnly _changePasswordCommand As AsyncRelayCommand
        Public ReadOnly Property ChangePasswordCommand As AsyncRelayCommand
            Get
                Return _changePasswordCommand
            End Get
        End Property

        Private ReadOnly _toggleShowPasswordCommand As RelayCommand
        Public ReadOnly Property ToggleShowPasswordCommand As RelayCommand
            Get
                Return _toggleShowPasswordCommand
            End Get
        End Property

        Private ReadOnly _toggleShowNewPasswordCommand As RelayCommand
        Public ReadOnly Property ToggleShowNewPasswordCommand As RelayCommand
            Get
                Return _toggleShowNewPasswordCommand
            End Get
        End Property

        ''' <summary>
        ''' Raised when authentication (and mandatory password change, if required) succeeds.
        ''' Application.xaml.vb subscribes to hide LoginView and show MainWindow.
        ''' </summary>
        Public Event LoginSucceeded As EventHandler

        ' ── Observable properties ─────────────────────────────────────────────

        Private _username As String = String.Empty
        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                SetProperty(_username, value)
            End Set
        End Property

        Private _password As String = String.Empty
        Public Property Password As String
            Get
                Return _password
            End Get
            Set(value As String)
                SetProperty(_password, value)
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                If SetProperty(_errorMessage, value) Then
                    OnPropertyChanged(NameOf(HasError))
                End If
            End Set
        End Property

        Public ReadOnly Property HasError As Boolean
            Get
                Return Not String.IsNullOrEmpty(_errorMessage)
            End Get
        End Property

        Private _isLoggingIn As Boolean = False
        Public Property IsLoggingIn As Boolean
            Get
                Return _isLoggingIn
            End Get
            Set(value As Boolean)
                If SetProperty(_isLoggingIn, value) Then
                    OnPropertyChanged(NameOf(IsNotLoggingIn))
                End If
            End Set
        End Property

        Public ReadOnly Property IsNotLoggingIn As Boolean
            Get
                Return Not _isLoggingIn
            End Get
        End Property

        Private _showPasswordChange As Boolean = False
        Public Property ShowPasswordChange As Boolean
            Get
                Return _showPasswordChange
            End Get
            Set(value As Boolean)
                If SetProperty(_showPasswordChange, value) Then
                    OnPropertyChanged(NameOf(HidePasswordChange))
                End If
            End Set
        End Property

        Public ReadOnly Property HidePasswordChange As Boolean
            Get
                Return Not _showPasswordChange
            End Get
        End Property

        Private _showPassword As Boolean = False
        Public Property ShowPassword As Boolean
            Get
                Return _showPassword
            End Get
            Set(value As Boolean)
                If SetProperty(_showPassword, value) Then
                    OnPropertyChanged(NameOf(HidePassword))
                End If
            End Set
        End Property

        ''' <summary>Inverse of ShowPassword — drives PasswordBox visibility in LoginView.</summary>
        Public ReadOnly Property HidePassword As Boolean
            Get
                Return Not _showPassword
            End Get
        End Property

        Private _newPassword As String = String.Empty
        Public Property NewPassword As String
            Get
                Return _newPassword
            End Get
            Set(value As String)
                SetProperty(_newPassword, value)
            End Set
        End Property

        Private _confirmNewPassword As String = String.Empty
        Public Property ConfirmNewPassword As String
            Get
                Return _confirmNewPassword
            End Get
            Set(value As String)
                SetProperty(_confirmNewPassword, value)
            End Set
        End Property

        Private _showNewPassword As Boolean = False
        Public Property ShowNewPassword As Boolean
            Get
                Return _showNewPassword
            End Get
            Set(value As Boolean)
                If SetProperty(_showNewPassword, value) Then
                    OnPropertyChanged(NameOf(HideNewPassword))
                End If
            End Set
        End Property

        Public ReadOnly Property HideNewPassword As Boolean
            Get
                Return Not _showNewPassword
            End Get
        End Property

        Public Sub New(auth As IAuthenticationService, sessionSvc As LoginSessionService)
            _auth = auth
            _session = sessionSvc
            _loginCommand = New AsyncRelayCommand(AddressOf LoginAsync)
            _changePasswordCommand = New AsyncRelayCommand(AddressOf ChangePasswordAndLoginAsync)
            _toggleShowPasswordCommand = New RelayCommand(Sub() ShowPassword = Not ShowPassword)
            _toggleShowNewPasswordCommand = New RelayCommand(Sub() ShowNewPassword = Not ShowNewPassword)
        End Sub

        ''' <summary>Resets all fields — called on each new LoginView display (initial and post-logout).</summary>
        Public Sub Reset()
            Username = String.Empty
            Password = String.Empty
            ErrorMessage = String.Empty
            IsLoggingIn = False
            ShowPasswordChange = False
            ShowPassword = False
            NewPassword = String.Empty
            ConfirmNewPassword = String.Empty
            ShowNewPassword = False
            _pendingUser = Nothing
            _pendingPassword = String.Empty
        End Sub

        ' ── Login flow ────────────────────────────────────────────────────────

        Private Async Function LoginAsync() As Task
            If IsLoggingIn Then Return
            If String.IsNullOrWhiteSpace(Username) OrElse String.IsNullOrEmpty(Password) Then
                ErrorMessage = "Please enter your username and password."
                Return
            End If

            IsLoggingIn = True
            ErrorMessage = String.Empty

            Dim result As AuthenticationResult = Nothing
            Dim capturedEx As Exception = Nothing
            Try
                result = Await _auth.AuthenticateAsync(Username, Password)
            Catch ex As Exception
                capturedEx = ex
            End Try

            If capturedEx IsNot Nothing Then
                ErrorMessage = "An unexpected error occurred. Please try again."
                IsLoggingIn = False
                Return
            End If

            If Not result.Success Then
                ErrorMessage = result.FailureReason
                Password = String.Empty
                IsLoggingIn = False
                Return
            End If

            ' DA6: first-login mandatory password change
            If result.User.LastPasswordChangeAt Is Nothing Then
                _pendingUser = result.User
                _pendingPassword = Password
                Password = String.Empty
                ShowPasswordChange = True
                ErrorMessage = "Please set a new password before continuing."
                IsLoggingIn = False
                Return
            End If

            _session.SetUser(result.User)
            IsLoggingIn = False
            RaiseEvent LoginSucceeded(Me, EventArgs.Empty)
        End Function

        ' ── First-login password change flow ─────────────────────────────────

        Private Async Function ChangePasswordAndLoginAsync() As Task
            If IsLoggingIn Then Return
            If NewPassword <> ConfirmNewPassword Then
                ErrorMessage = "Passwords do not match."
                Return
            End If
            If _pendingUser Is Nothing Then
                ErrorMessage = "Session error. Please log in again."
                ShowPasswordChange = False
                Return
            End If

            IsLoggingIn = True
            ErrorMessage = String.Empty

            Dim changeResult As PasswordChangeResult = Nothing
            Dim capturedEx As Exception = Nothing
            Try
                changeResult = Await _auth.ChangePasswordAsync(_pendingUser.Id, _pendingPassword, NewPassword)
            Catch ex As Exception
                capturedEx = ex
            End Try

            If capturedEx IsNot Nothing Then
                ErrorMessage = "An unexpected error occurred. Please try again."
                IsLoggingIn = False
                Return
            End If

            If Not changeResult.Success Then
                ErrorMessage = String.Join(Environment.NewLine, changeResult.ValidationErrors)
                IsLoggingIn = False
                Return
            End If

            _pendingUser.LastPasswordChangeAt = DateTime.UtcNow
            _session.SetUser(_pendingUser)
            _pendingUser = Nothing
            _pendingPassword = String.Empty
            IsLoggingIn = False
            RaiseEvent LoginSucceeded(Me, EventArgs.Empty)
        End Function

    End Class

End Namespace
