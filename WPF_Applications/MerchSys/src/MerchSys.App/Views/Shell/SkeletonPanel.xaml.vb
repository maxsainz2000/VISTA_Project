Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell

    Public Class SkeletonPanel
        Inherits UserControl

        Public Shared ReadOnly IsBusyProperty As DependencyProperty =
            DependencyProperty.Register("IsBusy", GetType(Boolean), GetType(SkeletonPanel), New PropertyMetadata(False))

        Public Property IsBusy As Boolean
            Get
                Return CBool(GetValue(IsBusyProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsBusyProperty, value)
            End Set
        End Property

        Public Shared ReadOnly LastLoadedAtProperty As DependencyProperty =
            DependencyProperty.Register("LastLoadedAt", GetType(DateTime?), GetType(SkeletonPanel), New PropertyMetadata(Nothing))

        Public Property LastLoadedAt As DateTime?
            Get
                Return CType(GetValue(LastLoadedAtProperty), DateTime?)
            End Get
            Set(value As DateTime?)
                SetValue(LastLoadedAtProperty, value)
            End Set
        End Property

        Public Shared ReadOnly KindProperty As DependencyProperty =
            DependencyProperty.Register("Kind", GetType(String), GetType(SkeletonPanel), New PropertyMetadata("Cards"))

        Public Property Kind As String
            Get
                Return CStr(GetValue(KindProperty))
            End Get
            Set(value As String)
                SetValue(KindProperty, value)
            End Set
        End Property

        Public Shared ReadOnly CountProperty As DependencyProperty =
            DependencyProperty.Register("Count", GetType(Integer), GetType(SkeletonPanel), New PropertyMetadata(5))

        Public Property Count As Integer
            Get
                Return CInt(GetValue(CountProperty))
            End Get
            Set(value As Integer)
                SetValue(CountProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
