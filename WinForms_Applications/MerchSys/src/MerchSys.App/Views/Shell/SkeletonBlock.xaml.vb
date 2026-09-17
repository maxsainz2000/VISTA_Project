Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell

    Public Class SkeletonBlock
        Inherits UserControl

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty =
            DependencyProperty.Register("CornerRadius", GetType(CornerRadius), GetType(SkeletonBlock), New PropertyMetadata(New CornerRadius(4)))

        Public Property CornerRadius As CornerRadius
            Get
                Return CType(GetValue(CornerRadiusProperty), CornerRadius)
            End Get
            Set(value As CornerRadius)
                SetValue(CornerRadiusProperty, value)
            End Set
        End Property

        ''' <summary>
        ''' Reflects the global reduced-motion gate (the <c>MotionEnabled</c> application resource,
        ''' resolved once at startup). The shimmer storyboard binds its trigger condition to this
        ''' instead of to the resource directly, because <see cref="Condition.Binding"/> only accepts
        ''' a real binding — a DynamicResource cannot be set on it. Defaults to True if unresolved.
        ''' </summary>
        Public ReadOnly Property ShimmerEnabled As Boolean
            Get
                Dim motionFlag As Object = Application.Current?.Resources("MotionEnabled")
                Return motionFlag Is Nothing OrElse CBool(motionFlag)
            End Get
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
