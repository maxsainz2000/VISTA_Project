' Rule 7 known-bad: Bare Imports MEL + Console.Write in an instance method
' Exact import present + Console. in executable code — flagged.

Imports Microsoft.Extensions.Logging
Imports System.Collections.Generic

Namespace Services
    Public Class ReportService
        Public Sub PrintReport(items As List(Of String))
            For Each item In items
                Console.Write(item)
                Console.WriteLine()
            Next
        End Sub
    End Class
End Namespace
