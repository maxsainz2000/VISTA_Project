Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace Views.Shell

    Public Class ErrorItem
        Public Property Message As String
        Public Property Element As FrameworkElement
    End Class

    Partial Public Class ErrorSummary
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            Validation.AddErrorHandler(Me, AddressOf OnValidationError)
        End Sub

        Public Shared ReadOnly ErrorsProperty As DependencyProperty =
            DependencyProperty.Register("Errors", GetType(ObservableCollection(Of ErrorItem)), GetType(ErrorSummary),
            New PropertyMetadata(Nothing))

        Public Property Errors As ObservableCollection(Of ErrorItem)
            Get
                Dim current = CType(GetValue(ErrorsProperty), ObservableCollection(Of ErrorItem))
                If current Is Nothing Then
                    current = New ObservableCollection(Of ErrorItem)()
                    SetValue(ErrorsProperty, current)
                End If
                Return current
            End Get
            Set(value As ObservableCollection(Of ErrorItem))
                SetValue(ErrorsProperty, value)
            End Set
        End Property

        Public Shared ReadOnly TargetContainerProperty As DependencyProperty =
            DependencyProperty.Register("TargetContainer", GetType(DependencyObject), GetType(ErrorSummary),
            New PropertyMetadata(Nothing, AddressOf OnTargetContainerChanged))

        Public Property TargetContainer As DependencyObject
            Get
                Return CType(GetValue(TargetContainerProperty), DependencyObject)
            End Get
            Set(value As DependencyObject)
                SetValue(TargetContainerProperty, value)
            End Set
        End Property

        Private Shared Sub OnTargetContainerChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim summary = DirectCast(d, ErrorSummary)
            Dim oldContainer = TryCast(e.OldValue, FrameworkElement)
            Dim newContainer = TryCast(e.NewValue, FrameworkElement)

            If oldContainer IsNot Nothing Then
                RemoveHandler oldContainer.Unloaded, AddressOf summary.OnContainerUnloaded
                RemoveHandler oldContainer.IsVisibleChanged, AddressOf summary.OnContainerIsVisibleChanged
            End If

            If newContainer IsNot Nothing Then
                AddHandler newContainer.Unloaded, AddressOf summary.OnContainerUnloaded
                AddHandler newContainer.IsVisibleChanged, AddressOf summary.OnContainerIsVisibleChanged
                summary.RefreshErrors()
            End If
        End Sub

        Private Sub OnContainerUnloaded(sender As Object, e As RoutedEventArgs)
            Errors.Clear()
            Me.Visibility = Visibility.Collapsed
        End Sub

        Private Sub OnContainerIsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If CBool(e.NewValue) Then
                RefreshErrors()
            Else
                Errors.Clear()
                Me.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub OnValidationError(sender As Object, e As ValidationErrorEventArgs)
            Dispatcher.BeginInvoke(Sub() RefreshErrors())
        End Sub

        Public Sub RefreshErrors()
            Dim root = TargetContainer
            If root Is Nothing Then root = Me.Parent
            If root Is Nothing Then Return

            Dim list As New List(Of ErrorItem)()
            FindErrorsInVisualTree(root, list)

            Errors.Clear()
            For Each item In list
                Errors.Add(item)
            Next

            ErrorsControl.ItemsSource = Errors
            Me.Visibility = If(Errors.Count > 0, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub FindErrorsInVisualTree(obj As DependencyObject, list As List(Of ErrorItem))
            If obj Is Nothing Then Return

            Dim element = TryCast(obj, FrameworkElement)
            If element IsNot Nothing AndAlso Validation.GetHasError(element) Then
                For Each valErr In Validation.GetErrors(element)
                    If Not list.Any(Function(item) item.Element Is element) Then
                        list.Add(New ErrorItem With {
                            .Message = valErr.ErrorContent.ToString(),
                            .Element = element
                        })
                    End If
                Next
            End If

            Dim count = VisualTreeHelper.GetChildrenCount(obj)
            For i As Integer = 0 To count - 1
                Dim child = VisualTreeHelper.GetChild(obj, i)
                FindErrorsInVisualTree(child, list)
            Next
        End Sub

        Private Sub ErrorButton_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn?.DataContext, ErrorItem)
            If item?.Element IsNot Nothing Then
                item.Element.Focus()
            End If
        End Sub
    End Class

End Namespace
