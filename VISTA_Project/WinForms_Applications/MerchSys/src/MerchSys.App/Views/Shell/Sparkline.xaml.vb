Imports System.Windows
Imports System.Windows.Controls
Imports System.Collections.ObjectModel
Imports System.Collections.Generic
Imports System.Linq

Namespace Views.Shell

    Public Class SparklineBarItem
        Public Property Height As Double
        Public Property Value As Double
        Public Property Label As String
    End Class

    Public Class Sparkline
        Inherits UserControl

        Public Shared ReadOnly PointsProperty As DependencyProperty =
            DependencyProperty.Register("Points", GetType(IEnumerable(Of Double)), GetType(Sparkline),
                New FrameworkPropertyMetadata(Nothing, AddressOf OnPointsChanged))

        Public Shared ReadOnly LabelsProperty As DependencyProperty =
            DependencyProperty.Register("Labels", GetType(IEnumerable(Of String)), GetType(Sparkline),
                New FrameworkPropertyMetadata(Nothing, AddressOf OnLabelsChanged))

        Public Property Points As IEnumerable(Of Double)
            Get
                Return CType(GetValue(PointsProperty), IEnumerable(Of Double))
            End Get
            Set(value As IEnumerable(Of Double))
                SetValue(PointsProperty, value)
            End Set
        End Property

        Public Property Labels As IEnumerable(Of String)
            Get
                Return CType(GetValue(LabelsProperty), IEnumerable(Of String))
            End Get
            Set(value As IEnumerable(Of String))
                SetValue(LabelsProperty, value)
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
            ' Rebuild once the control has a measured height (and on resize) so bars scale to the
            ' instance's actual height rather than a fixed constant — UpdateBars during a Points/Labels
            ' change can run before layout, when ActualHeight is still 0.
            AddHandler Me.SizeChanged, Sub(s, e) UpdateBars()
        End Sub

        Private Shared Sub OnPointsChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = TryCast(d, Sparkline)
            If ctrl IsNot Nothing Then
                ctrl.UpdateBars()
            End If
        End Sub

        Private Shared Sub OnLabelsChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
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

            ' Scale bars to the control's measured height; fall back to an explicit Height, then 24px
            ' (the UX-12 default) when neither is available yet.
            Dim sparkHeight As Double = ActualHeight
            If sparkHeight <= 0 Then sparkHeight = If(Double.IsNaN(Height), 24.0, Height)
            If sparkHeight <= 0 Then sparkHeight = 24.0

            Dim labelList = If(Labels IsNot Nothing, Labels.ToList(), New List(Of String)())
            Dim idx As Integer = 0

            For Each pt In Points
                Dim barH = Math.Max(2.0, (pt / maxVal) * sparkHeight)
                Dim lbl = If(idx < labelList.Count, labelList(idx), String.Empty)
                InternalBars.Add(New SparklineBarItem With {.Height = barH, .Value = pt, .Label = lbl})
                idx += 1
            Next
        End Sub
    End Class

End Namespace
