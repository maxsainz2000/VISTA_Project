' Rule 14 known-bad: Constructor parameters shadow class properties
' Source: NavigationItem.vb:28 (NavigationGroup.New) — verified true positive in 2026-05-24 audit
' 'groupName' shadows GroupName, 'items' shadows Items.

Namespace Models
    Public Class NavigationGroup
        Public Property GroupName As String
        Public Property Items As List(Of Object)

        Public Sub New(groupName As String, items As List(Of Object))
            Me.GroupName = groupName
            Me.Items = items
        End Sub
    End Class
End Namespace
