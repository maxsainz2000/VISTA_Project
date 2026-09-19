' Rule 14 known-good: Method inside a Module — modules have no instance state
' Source: IAuthenticationService.vb PasswordHashHelper module (verified FP in 2026-05-24 audit)

Namespace Services
    Friend Module PasswordHashHelper
        Public Property Password As String   ' Module-level — not instance state

        Public Function Verify(password As String, storedHash As String) As Boolean
            ' 'password' name-matches the module member above but modules have no instance scope
            If String.IsNullOrEmpty(storedHash) Then Return False
            Return storedHash = password
        End Function
    End Module
End Namespace
