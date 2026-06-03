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

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
