Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Collections
Imports System.Collections.Specialized

Namespace Views.Shell

    Public Class FilterSummaryBar
        Inherits UserControl

        Public Shared ReadOnly ShownCountProperty As DependencyProperty =
            DependencyProperty.Register("ShownCount", GetType(Integer), GetType(FilterSummaryBar), New PropertyMetadata(0, AddressOf OnCountChanged))

        Public Shared ReadOnly TotalCountProperty As DependencyProperty =
            DependencyProperty.Register("TotalCount", GetType(Integer), GetType(FilterSummaryBar), New PropertyMetadata(0, AddressOf OnCountChanged))

        Public Shared ReadOnly ActiveFiltersProperty As DependencyProperty =
            DependencyProperty.Register("ActiveFilters", GetType(IEnumerable), GetType(FilterSummaryBar), New PropertyMetadata(Nothing, AddressOf OnActiveFiltersChanged))

        Public Shared ReadOnly ClearAllCommandProperty As DependencyProperty =
            DependencyProperty.Register("ClearAllCommand", GetType(ICommand), GetType(FilterSummaryBar), New PropertyMetadata(Nothing))

        Public Shared ReadOnly ResultCountTextProperty As DependencyProperty =
            DependencyProperty.Register("ResultCountText", GetType(String), GetType(FilterSummaryBar), New PropertyMetadata(String.Empty))

        Public Shared ReadOnly HasActiveFiltersProperty As DependencyProperty =
            DependencyProperty.Register("HasActiveFilters", GetType(Boolean), GetType(FilterSummaryBar), New PropertyMetadata(False))

        Public Property ShownCount As Integer
            Get
                Return CInt(GetValue(ShownCountProperty))
            End Get
            Set(value As Integer)
                SetValue(ShownCountProperty, value)
            End Set
        End Property

        Public Property TotalCount As Integer
            Get
                Return CInt(GetValue(TotalCountProperty))
            End Get
            Set(value As Integer)
                SetValue(TotalCountProperty, value)
            End Set
        End Property

        Public Property ActiveFilters As IEnumerable
            Get
                Return CType(GetValue(ActiveFiltersProperty), IEnumerable)
            End Get
            Set(value As IEnumerable)
                SetValue(ActiveFiltersProperty, value)
            End Set
        End Property

        Public Property ClearAllCommand As ICommand
            Get
                Return CType(GetValue(ClearAllCommandProperty), ICommand)
            End Get
            Set(value As ICommand)
                SetValue(ClearAllCommandProperty, value)
            End Set
        End Property

        Public Property ResultCountText As String
            Get
                Return CStr(GetValue(ResultCountTextProperty))
            End Get
            Set(value As String)
                SetValue(ResultCountTextProperty, value)
            End Set
        End Property

        Public Property HasActiveFilters As Boolean
            Get
                Return CBool(GetValue(HasActiveFiltersProperty))
            End Get
            Set(value As Boolean)
                SetValue(HasActiveFiltersProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Shared Sub OnCountChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim bar = CType(d, FilterSummaryBar)
            bar.UpdateState()
        End Sub

        Private Shared Sub OnActiveFiltersChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim bar = CType(d, FilterSummaryBar)

            ' Unsubscribe from old collection
            If e.OldValue IsNot Nothing AndAlso TypeOf e.OldValue Is INotifyCollectionChanged Then
                RemoveHandler CType(e.OldValue, INotifyCollectionChanged).CollectionChanged, AddressOf bar.OnCollectionChanged
            End If

            ' Subscribe to new collection
            If e.NewValue IsNot Nothing AndAlso TypeOf e.NewValue Is INotifyCollectionChanged Then
                AddHandler CType(e.NewValue, INotifyCollectionChanged).CollectionChanged, AddressOf bar.OnCollectionChanged
            End If

            bar.UpdateState()
        End Sub

        Private Sub OnCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
            UpdateState()
        End Sub

        Private Sub UpdateState()
            Dim hasFilters As Boolean = False
            If ActiveFilters IsNot Nothing Then
                Dim list = TryCast(ActiveFilters, ICollection)
                If list IsNot Nothing Then
                    hasFilters = (list.Count > 0)
                Else
                    Dim enumerator = ActiveFilters.GetEnumerator()
                    If enumerator IsNot Nothing AndAlso enumerator.MoveNext() Then
                        hasFilters = True
                    End If
                End If
            End If

            HasActiveFilters = hasFilters

            If hasFilters Then
                ResultCountText = $"{ShownCount} of {TotalCount} results"
            Else
                ResultCountText = $"{TotalCount} results"
            End If
        End Sub

    End Class

End Namespace
