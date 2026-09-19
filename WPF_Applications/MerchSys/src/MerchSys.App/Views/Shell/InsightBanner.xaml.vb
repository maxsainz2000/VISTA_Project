Imports System.Windows
Imports System.Windows.Controls
Imports MerchSys.SharedKernel.Enums

Namespace Views.Shell

    Public Class InsightBanner
        Inherits UserControl

        Public Shared ReadOnly TitleProperty As DependencyProperty =
            DependencyProperty.Register("Title", GetType(String), GetType(InsightBanner), New PropertyMetadata("What This Means"))

        Public Shared ReadOnly TextProperty As DependencyProperty =
            DependencyProperty.Register("Text", GetType(String), GetType(InsightBanner), New PropertyMetadata(String.Empty))

        Public Shared ReadOnly SeverityProperty As DependencyProperty =
            DependencyProperty.Register("Severity", GetType(InsightSeverity), GetType(InsightBanner), New PropertyMetadata(InsightSeverity.Info))

        Public Property Title As String
            Get
                Return CStr(GetValue(TitleProperty))
            End Get
            Set(value As String)
                SetValue(TitleProperty, value)
            End Set
        End Property

        Public Property Text As String
            Get
                Return CStr(GetValue(TextProperty))
            End Get
            Set(value As String)
                SetValue(TextProperty, value)
            End Set
        End Property

        Public Property Severity As InsightSeverity
            Get
                Return CType(GetValue(SeverityProperty), InsightSeverity)
            End Get
            Set(value As InsightSeverity)
                SetValue(SeverityProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
