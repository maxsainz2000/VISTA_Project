' Rule 12 known-bad: .Count(Function(x) ...) on a List(Of T)
' Receiver is explicitly declared As List(Of Product) — triggers the BC32016 trap.

Namespace Services
    Public Class ExampleService
        Public Function CountActiveProducts(products As List(Of Product)) As Integer
            Return products.Count(Function(p) p.IsActive)
        End Function
    End Class
End Namespace
