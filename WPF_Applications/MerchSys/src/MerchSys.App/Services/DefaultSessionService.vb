Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Stub session that always returns Manager — for developer DEBUG bypass only.
    ''' Registered as ISessionService only when the VISTA_BYPASS_LOGIN=1 environment variable
    ''' is set in a DEBUG build. The release build always uses <see cref="LoginSessionService"/>.
    ''' </summary>
    Public Class DefaultSessionService
        Implements ISessionService

        Public ReadOnly Property CurrentUsername As String Implements ISessionService.CurrentUsername
            Get
                Return "Manager"
            End Get
        End Property

        Public ReadOnly Property CurrentRole As UserRole Implements ISessionService.CurrentRole
            Get
                Return UserRole.Manager
            End Get
        End Property

        Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionService.IsAuthenticated
            Get
                Return True
            End Get
        End Property

    End Class

End Namespace
