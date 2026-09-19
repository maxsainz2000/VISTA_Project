Imports System

Namespace Interfaces

    Public Enum WriteContextKind
        ''' <summary>Default. User-initiated UI write. Subject to role enforcement.</summary>
        User = 0
        ''' <summary>System-initiated (background service, MediatR handler). Bypasses role enforcement.</summary>
        System = 1
        ''' <summary>Auth-self-service (Owner changes own password). Caller carries Username assertion.</summary>
        AuthSelfService = 2
    End Enum

    Public Interface IWriteContextScope
        ''' <summary>The kind in force for the current async-flow scope.</summary>
        ReadOnly Property Current As WriteContextKind

        ''' <summary>
        ''' For <see cref="WriteContextKind.AuthSelfService"/> only — the username asserted by the
        ''' caller. The interceptor uses this to verify the caller owns the row being written.
        ''' </summary>
        ReadOnly Property SelfServiceUsername As String

        ''' <summary>
        ''' Enter a nested scope of the given kind. Disposing the returned scope restores the
        ''' previous kind. Implementations must be safe across async boundaries — use
        ''' <c>AsyncLocal(Of T)</c>, not thread-local.
        ''' </summary>
        Function Enter(kind As WriteContextKind, Optional selfServiceUsername As String = Nothing) As IDisposable
    End Interface

End Namespace
