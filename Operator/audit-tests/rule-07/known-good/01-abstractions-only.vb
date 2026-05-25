' Rule 7 known-good: Imports Microsoft.Extensions.Logging.Abstractions only
' .Abstractions sub-namespace does NOT bring the Console class into scope.
' Source: Pos.SequenceConcurrencyHarness.vb (verified false positive in 2026-05-24 audit)

Imports System.Collections.Concurrent
Imports Microsoft.Extensions.Logging.Abstractions

Namespace Tests
    Public Module HarnessReport
        Public Sub Run()
            Console.WriteLine("Harness starting...")
            Console.WriteLine("Done.")
        End Sub
    End Module
End Namespace
