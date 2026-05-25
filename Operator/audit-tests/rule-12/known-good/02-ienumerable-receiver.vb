' Rule 12 known-good: .Count(predicate) on IEnumerable(Of T)
' IEnumerable does not have a Count property — LINQ extension is the only candidate.

Namespace Services
    Public Class ExampleService
        Public Function CountActiveItems(items As IEnumerable(Of String)) As Integer
            Return items.Count(Function(x) x.Length > 0)
        End Function
    End Class
End Namespace
