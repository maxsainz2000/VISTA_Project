Imports MerchSys.SharedKernel.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Singleton session service backed by an authenticated <see cref="UserAccount"/>.
    ''' Replaces <see cref="DefaultSessionService"/> in all builds.
    ''' Session state lives in memory only — no token is written to disk or SQLite (DA3).
    ''' Clearing <see cref="ClearUser"/> on logout is sufficient to terminate the session.
    ''' </summary>
    Public Class LoginSessionService
        Implements ISessionService

        Private _currentUser As UserAccount = Nothing

        Public Sub SetUser(user As UserAccount)
            _currentUser = user
        End Sub

        Public Sub ClearUser()
            _currentUser = Nothing
        End Sub

        Public ReadOnly Property IsAuthenticated As Boolean
            Get
                Return _currentUser IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property CurrentUsername As String Implements ISessionService.CurrentUsername
            Get
                Return If(_currentUser?.Username, String.Empty)
            End Get
        End Property

        Public ReadOnly Property CurrentRole As UserRole Implements ISessionService.CurrentRole
            Get
                Return If(_currentUser IsNot Nothing, _currentUser.Role, UserRole.Manager)
            End Get
        End Property

    End Class

End Namespace
