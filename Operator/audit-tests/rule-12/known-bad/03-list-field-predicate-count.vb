' Rule 12 known-bad: .Count(predicate) on a class-level field typed As List(Of T)

Namespace Services
    Public Class ExampleViewModel
        Private _vendors As List(Of Vendor)

        Public Function GetActiveVendorCount() As Integer
            Return _vendors.Count(Function(v) v.IsActive)
        End Function
    End Class
End Namespace
