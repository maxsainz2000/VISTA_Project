' Rule 7 known-good: Has exact bare import AND Console. text, but only in a comment
' Console. in comments must not be flagged.

Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class SomeService
        ' Note: Console.WriteLine was removed in favour of structured logging
        Public Sub DoWork()
            Dim x = 42
        End Sub
    End Class
End Namespace
