' Rule 12 known-bad: .Count(predicate) on an As New List(Of T) local variable
' Receiver declared As New List(Of T) — same issue as explicit List(Of T) declaration.

Namespace Services
    Public Class ExampleService
        Public Sub ProcessItems()
            Dim items As New List(Of String)()
            items.Add("hello")
            items.Add("world")
            items.Add("!")
            Dim longCount = items.Count(Function(s) s.Length > 3)
            System.Console.WriteLine(longCount)
        End Sub
    End Class
End Namespace
