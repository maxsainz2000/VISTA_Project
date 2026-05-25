' Rule 14 known-good: Instance method with parameter that does NOT match any property
' 'result' parameter has no matching property on this class — no shadow.
' Source: ReceiptArchivalService.vb:110 (verified false positive in 2026-05-24 audit)

Namespace Services
    Public Class ArchivalService
        Private _archiveCount As Integer

        Public Sub LogBatchResult(result As String)
            ' 'result' does not match any property on ArchivalService
            System.Console.WriteLine(result)
            _archiveCount += 1
        End Sub
    End Class
End Namespace
