Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Views.Shell

    Public Class ErrorStatePanel
        Inherits UserControl

        Public Shared ReadOnly TitleProperty As DependencyProperty =
            DependencyProperty.Register("Title", GetType(String), GetType(ErrorStatePanel), New PropertyMetadata("Load Failed"))

        Public Shared ReadOnly MessageProperty As DependencyProperty =
            DependencyProperty.Register("Message", GetType(String), GetType(ErrorStatePanel), New PropertyMetadata("Could not load data. Check your connection and try again."))

        Public Shared ReadOnly RetryCommandProperty As DependencyProperty =
            DependencyProperty.Register("RetryCommand", GetType(ICommand), GetType(ErrorStatePanel), New PropertyMetadata(Nothing))

        Public Property Title As String
            Get
                Return CStr(GetValue(TitleProperty))
            End Get
            Set(value As String)
                SetValue(TitleProperty, value)
            End Set
        End Property

        Public Property Message As String
            Get
                Return CStr(GetValue(MessageProperty))
            End Get
            Set(value As String)
                SetValue(MessageProperty, value)
            End Set
        End Property

        Public Property RetryCommand As ICommand
            Get
                Return CType(GetValue(RetryCommandProperty), ICommand)
            End Get
            Set(value As ICommand)
                SetValue(RetryCommandProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
