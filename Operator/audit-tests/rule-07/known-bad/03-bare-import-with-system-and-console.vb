' Rule 7 known-bad: Bare MEL import alongside other imports + Console.WriteLine
' Multiple imports do not cancel the gate; only the bare MEL import matters.

Imports System
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Logging.Abstractions

Namespace Tests
    Public Class TestRunner
        Public Sub Execute()
            Console.WriteLine("Executing test...")
        End Sub
    End Class
End Namespace
