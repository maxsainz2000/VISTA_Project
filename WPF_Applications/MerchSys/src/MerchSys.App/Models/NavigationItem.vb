Imports CommunityToolkit.Mvvm.ComponentModel

Namespace Models

    Public Class NavigationItem
        Inherits ObservableObject

        Public Property DisplayName As String
        Public Property ViewType As Type

        Private _isActive As Boolean
        Public Property IsActive As Boolean
            Get
                Return _isActive
            End Get
            Set(value As Boolean)
                SetProperty(_isActive, value)
            End Set
        End Property

    End Class

    Public Class NavigationGroup

        Public Property GroupName As String
        Public Property Items As List(Of NavigationItem)

        Public Sub New(groupName As String, items As List(Of NavigationItem))
            Me.GroupName = groupName
            Me.Items = items
        End Sub

    End Class

End Namespace
