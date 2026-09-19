Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell

    Public Enum DeltaDirection
        Flat = 0
        Up = 1
        Down = 2
    End Enum

    Public Class DeltaIndicator
        Inherits UserControl

        Public Shared ReadOnly PercentProperty As DependencyProperty =
            DependencyProperty.Register("Percent", GetType(Double), GetType(DeltaIndicator),
                New FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, AddressOf OnPercentChanged))

        Public Shared ReadOnly InvertSemanticsProperty As DependencyProperty =
            DependencyProperty.Register("InvertSemantics", GetType(Boolean), GetType(DeltaIndicator),
                New FrameworkPropertyMetadata(False, FrameworkPropertyMetadataOptions.AffectsRender))

        Private Shared ReadOnly KeyDirectionPropertyKey As DependencyPropertyKey =
            DependencyProperty.RegisterReadOnly("Direction", GetType(DeltaDirection), GetType(DeltaIndicator),
                New FrameworkPropertyMetadata(DeltaDirection.Flat))

        Public Shared ReadOnly DirectionProperty As DependencyProperty =
            KeyDirectionPropertyKey.DependencyProperty

        Public Property Percent As Double
            Get
                Return CDbl(GetValue(PercentProperty))
            End Get
            Set(value As Double)
                SetValue(PercentProperty, value)
            End Set
        End Property

        Public Property InvertSemantics As Boolean
            Get
                Return CBool(GetValue(InvertSemanticsProperty))
            End Get
            Set(value As Boolean)
                SetValue(InvertSemanticsProperty, value)
            End Set
        End Property

        Public Property Direction As DeltaDirection
            Get
                Return CType(GetValue(DirectionProperty), DeltaDirection)
            End Get
            Private Set(value As DeltaDirection)
                SetValue(KeyDirectionPropertyKey, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Shared Sub OnPercentChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = TryCast(d, DeltaIndicator)
            If ctrl IsNot Nothing Then
                Dim val = CDbl(e.NewValue)
                If val > 0.001 Then
                    ctrl.Direction = DeltaDirection.Up
                ElseIf val < -0.001 Then
                    ctrl.Direction = DeltaDirection.Down
                Else
                    ctrl.Direction = DeltaDirection.Flat
                End If
            End If
        End Sub

    End Class

End Namespace
