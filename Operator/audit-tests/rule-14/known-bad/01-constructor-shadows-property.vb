' Rule 14 known-bad: Constructor parameter shadows instance property (case-insensitive)
' Source: VatReturnLockedException.vb:26 (verified true positive in 2026-05-24 audit)
' 'returnId' shadows 'ReturnId', 'year' shadows 'Year' — VB.NET case-insensitive match.

Namespace Exceptions
    Public Class VatReturnLockedException
        Inherits Exception

        Public ReadOnly Property ReturnId As Integer
        Public ReadOnly Property Year As Integer

        Public Sub New(returnId As Integer, year As Integer)
            Me.ReturnId = returnId
            Me.Year = year
        End Sub
    End Class
End Namespace
