Imports System.Windows
Imports System.Windows.Controls
Imports System.Collections.ObjectModel
Imports System.Collections.Generic
Imports System.Linq

Namespace Views.Shell

    Public Class SparklineBarItem
        Public Property Height As Double
        Public Property Value As Double
    End Class

    Public Class Sparkline
        Inherits UserControl

        Public Shared ReadOnly PointsProperty As DependencyProperty =
            DependencyProperty.Register("Points", GetType(IEnumerable(Of Double)), GetType(Sparkline),
                New FrameworkPropertyMetadata(Nothing, AddressOf OnPointsChanged))

        Public Property Points As IEnumerable(Of Double)
            Get
                Return CType(GetValue(PointsProperty), IEnumerable(Of Double))
            End Get
            Set(value As IEnumerable(Of Double))
                SetValue(PointsProperty, value)
            End Set
        End Property

        Private Shared ReadOnly InternalBarsPropertyKey As DependencyPropertyKey =
            DependencyProperty.RegisterReadOnly("InternalBars", GetType(ObservableCollection(Of SparklineBarItem)), GetType(Sparkline),
                New FrameworkPropertyMetadata(Nothing))

        Public Shared ReadOnly InternalBarsProperty As DependencyProperty =
            InternalBarsPropertyKey.DependencyProperty

        Public Property InternalBars As ObservableCollection(Of SparklineBarItem)
            Get
                Return CType(GetValue(InternalBarsProperty), ObservableCollection(Of SparklineBarItem))
            End Get
            Private Set(value As ObservableCollection(Of SparklineBarItem))
                SetValue(InternalBarsPropertyKey, value)
            End Set
        End Property

        Public Sub New()
            InternalBars = New ObservableCollection(Of SparklineBarItem)()
            InitializeComponent()
        End Sub

        Private Shared Sub OnPointsChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = TryCast(d, Sparkline)
            If ctrl IsNot Nothing Then
                ctrl.UpdateBars()
            End If
        End Sub

        Private Sub UpdateBars()
            InternalBars.Clear()
            If Points Is Nothing OrElse Not Points.Any() Then Return

            Dim maxVal As Double = Points.Max()
            If maxVal <= 0 Then maxVal = 1.0

            Const SparkHeight As Double = 24.0

            For Each pt In Points
                Dim barH = Math.Max(2.0, (pt / maxVal) * SparkHeight)
                InternalBars.Add(New SparklineBarItem With {.Height = barH, .Value = pt})
            Next
        End Sub
    End Class

End Namespace
