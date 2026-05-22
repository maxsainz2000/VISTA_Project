Imports MerchSys.SharedKernel.Enums

Namespace Entities

    Public Class UserAccount

        Public Property Id As Integer
        Public Property Username As String
        ''' <summary>Argon2id hash: $argon2id$v=19$m=19456,t=2,p=1$&lt;base64Salt&gt;$&lt;base64Hash&gt;</summary>
        Public Property PasswordHash As String
        Public Property Role As UserRole
        Public Property IsActive As Boolean
        Public Property FailedLoginAttempts As Integer
        ''' <summary>Set to UtcNow + 15 min after 5 consecutive failures (DA2). Null = not locked.</summary>
        Public Property LockedUntil As DateTime?
        ''' <summary>Null signals first-login state — triggers mandatory password change (DA6).</summary>
        Public Property LastPasswordChangeAt As DateTime?
        Public Property CreatedAt As DateTime
        Public Property ModifiedAt As DateTime?

    End Class

End Namespace
