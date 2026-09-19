Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Stub session that always returns the Developer role — for developer DEBUG bypass only.
    ''' Registered as ISessionService only when the VISTA_BYPASS_LOGIN=1 environment variable
    ''' is set in a DEBUG build. The release build always uses <see cref="LoginSessionService"/>.
    ''' Developer is a Manager superset, so the bypass retains full operational access plus
    ''' the Developer Tools module.
    ''' </summary>
    Public Class DefaultSessionService
        Implements ISessionService

        Public ReadOnly Property CurrentUsername As String Implements ISessionService.CurrentUsername
            Get
                Return "Developer"
            End Get
        End Property

        Public ReadOnly Property CurrentRole As UserRole Implements ISessionService.CurrentRole
            Get
                Return UserRole.Developer
            End Get
        End Property

        Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionService.IsAuthenticated
            Get
                Return True
            End Get
        End Property

    End Class

End Namespace
