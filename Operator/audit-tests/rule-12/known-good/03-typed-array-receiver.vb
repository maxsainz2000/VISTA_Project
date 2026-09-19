' Rule 12 known-good: .Count(predicate) on a typed array (String())
' Arrays have Length, not Count — LINQ extension resolves unambiguously.

Namespace Services
    Public Class ExampleService
        Public Function CountLong(names As String()) As Integer
            Return names.Count(Function(n) n.Length > 5)
        End Function
    End Class
End Namespace
