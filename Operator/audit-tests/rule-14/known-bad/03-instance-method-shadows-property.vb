' Rule 14 known-bad: Non-shared instance method parameter shadows an instance property
' 'vendors' parameter (lowercase) shadows 'Vendors' property (PascalCase) on same class.
' VB.NET case-insensitive resolution binds Vendors.Clear() to the parameter.

Namespace ViewModels
    Public Class PurchaseOrderEditorViewModel
        Public Property Vendors As System.Collections.ObjectModel.ObservableCollection(Of String)

        Public Sub LoadVendors(vendors As List(Of String))
            Vendors.Clear()          ' silently resolves to vendors.Clear() — empties the INPUT list
            For Each v In vendors
                Vendors.Add(v)       ' resolves to vendors.Add — adds back to the now-empty input
            Next
        End Sub
    End Class
End Namespace
