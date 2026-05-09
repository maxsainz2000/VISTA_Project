Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Default session implementation for the desktop (single-user) scenario.
    ''' Returns the active Manager session. Replace with a login-aware implementation
    ''' when a login screen is added in a future phase.
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

    End Class

End Namespace
