Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Data
Imports MerchSys.App.Helpers

Namespace Views.Shell

    Public Class FreshnessChip
        Inherits UserControl

        Public Shared ReadOnly LastLoadedAtProperty As DependencyProperty =
            DependencyProperty.Register("LastLoadedAt", GetType(DateTime?), GetType(FreshnessChip), New PropertyMetadata(Nothing, AddressOf OnLastLoadedAtChanged))

        Public Shared ReadOnly RefreshCommandProperty As DependencyProperty =
            DependencyProperty.Register("RefreshCommand", GetType(ICommand), GetType(FreshnessChip), New PropertyMetadata(Nothing))

        Public Shared ReadOnly StalenessThresholdProperty As DependencyProperty =
            DependencyProperty.Register("StalenessThreshold", GetType(TimeSpan), GetType(FreshnessChip), New PropertyMetadata(TimeSpan.FromMinutes(5), AddressOf OnStalenessThresholdChanged))

        Public Shared ReadOnly IsNeverLoadedProperty As DependencyProperty =
            DependencyProperty.Register("IsNeverLoaded", GetType(Boolean), GetType(FreshnessChip), New PropertyMetadata(True))

        Public Shared ReadOnly IsStaleProperty As DependencyProperty =
            DependencyProperty.Register("IsStale", GetType(Boolean), GetType(FreshnessChip), New PropertyMetadata(False))

        Public Property LastLoadedAt As DateTime?
            Get
                Return CType(GetValue(LastLoadedAtProperty), DateTime?)
            End Get
            Set(value As DateTime?)
                SetValue(LastLoadedAtProperty, value)
            End Set
        End Property

        Public Property RefreshCommand As ICommand
            Get
                Return CType(GetValue(RefreshCommandProperty), ICommand)
            End Get
            Set(value As ICommand)
                SetValue(RefreshCommandProperty, value)
            End Set
        End Property

        Public Property StalenessThreshold As TimeSpan
            Get
                Return CType(GetValue(StalenessThresholdProperty), TimeSpan)
            End Get
            Set(value As TimeSpan)
                SetValue(StalenessThresholdProperty, value)
            End Set
        End Property

        Public Property IsNeverLoaded As Boolean
            Get
                Return CBool(GetValue(IsNeverLoadedProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsNeverLoadedProperty, value)
            End Set
        End Property

        Public Property IsStale As Boolean
            Get
                Return CBool(GetValue(IsStaleProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsStaleProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub FreshnessChip_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            AddHandler FreshnessTimer.Tick, AddressOf OnFreshnessTick
            UpdateFreshnessState()
        End Sub

        Private Sub FreshnessChip_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            RemoveHandler FreshnessTimer.Tick, AddressOf OnFreshnessTick
        End Sub

        Private Sub OnFreshnessTick(sender As Object, e As EventArgs)
            UpdateFreshnessState()
        End Sub

        Private Shared Sub OnLastLoadedAtChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim chip = CType(d, FreshnessChip)
            chip.UpdateFreshnessState()
        End Sub

        Private Shared Sub OnStalenessThresholdChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim chip = CType(d, FreshnessChip)
            chip.UpdateFreshnessState()
        End Sub

        Private Sub UpdateFreshnessState()
            Dim loadedAt = LastLoadedAt
            IsNeverLoaded = Not loadedAt.HasValue

            If loadedAt.HasValue Then
                Dim age = DateTime.Now - loadedAt.Value
                IsStale = age >= StalenessThreshold
            Else
                IsStale = False
            End If

            ' Force the TextBlock binding to re-evaluate so that the relative time is updated immediately
            If TimeText IsNot Nothing Then
                Dim expression = BindingOperations.GetBindingExpression(TimeText, TextBlock.TextProperty)
                If expression IsNot Nothing Then
                    expression.UpdateTarget()
                End If
            End If
        End Sub

    End Class

End Namespace
