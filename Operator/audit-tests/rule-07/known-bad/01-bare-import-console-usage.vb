' Rule 7 known-bad: Imports Microsoft.Extensions.Logging (exact bare) + Console.WriteLine in code
' Both conditions fire — Console resolves to MEL Console class, not System.Console.

Imports Microsoft.Extensions.Logging

Namespace Tests
    Public Module DiagnosticHarness
        Public Sub Run()
            Console.WriteLine("Starting harness...")
            Console.WriteLine("Test passed.")
        End Sub
    End Module
End Namespace
