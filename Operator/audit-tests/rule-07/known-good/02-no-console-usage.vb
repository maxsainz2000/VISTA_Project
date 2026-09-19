' Rule 7 known-good: Has bare Imports Microsoft.Extensions.Logging but no Console. usage
' Gate 1 fires (exact import present) but gate 2 fails (no Console. in executable code).

Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class SomeService
        Private ReadOnly _logger As ILogger(Of SomeService)

        Public Sub New(logger As ILogger(Of SomeService))
            _logger = logger
        End Sub

        Public Sub DoWork()
            _logger.LogInformation("Work done.")
        End Sub
    End Class
End Namespace
