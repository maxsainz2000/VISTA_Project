Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell

    Public Class EmptyStatePanel
        Inherits UserControl

        Public Shared ReadOnly TitleProperty As DependencyProperty =
            DependencyProperty.Register("Title", GetType(String), GetType(EmptyStatePanel), New PropertyMetadata("No Data Available"))

        Public Shared ReadOnly DescriptionProperty As DependencyProperty =
            DependencyProperty.Register("Description", GetType(String), GetType(EmptyStatePanel), New PropertyMetadata("There are no records to display."))

        Public Property Title As String
            Get
                Return CStr(GetValue(TitleProperty))
            End Get
            Set(value As String)
                SetValue(TitleProperty, value)
            End Set
        End Property

        Public Property Description As String
            Get
                Return CStr(GetValue(DescriptionProperty))
            End Get
            Set(value As String)
                SetValue(DescriptionProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
