Imports MerchSys.SharedKernel.Enums

Namespace Interfaces

    ''' <summary>
    ''' Provides the current user's session identity for audit and business-rule enforcement.
    ''' </summary>
    Public Interface ISessionService
        ReadOnly Property CurrentUsername As String
        ReadOnly Property CurrentRole As UserRole
        ReadOnly Property IsAuthenticated As Boolean
    End Interface

End Namespace
