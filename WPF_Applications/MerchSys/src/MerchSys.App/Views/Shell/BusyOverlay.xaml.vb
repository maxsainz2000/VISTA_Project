Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell

    Public Class BusyOverlay
        Inherits UserControl

        Public Shared ReadOnly MessageProperty As DependencyProperty =
            DependencyProperty.Register("Message", GetType(String), GetType(BusyOverlay), New PropertyMetadata("Loading..."))

        Public Property Message As String
            Get
                Return CStr(GetValue(MessageProperty))
            End Get
            Set(value As String)
                SetValue(MessageProperty, value)
            End Set
        End Property

        Public Shared ReadOnly IsBusyProperty As DependencyProperty =
            DependencyProperty.Register("IsBusy", GetType(Boolean), GetType(BusyOverlay), New PropertyMetadata(False))

        Public Property IsBusy As Boolean
            Get
                Return CBool(GetValue(IsBusyProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsBusyProperty, value)
            End Set
        End Property

        Public Shared ReadOnly LastLoadedAtProperty As DependencyProperty =
            DependencyProperty.Register("LastLoadedAt", GetType(DateTime?), GetType(BusyOverlay), New PropertyMetadata(Nothing))

        Public Property LastLoadedAt As DateTime?
            Get
                Return CType(GetValue(LastLoadedAtProperty), DateTime?)
            End Get
            Set(value As DateTime?)
                SetValue(LastLoadedAtProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
