Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.App.ViewModels
Imports MerchSys.App.ViewModels.Shell

Namespace Views.Shell

    Public Class ShortcutsOverlay
        Inherits UserControl

        Private _previouslyFocusedElement As IInputElement

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ShortcutsOverlay_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
            If CType(e.NewValue, Boolean) Then
                ' Save the currently focused element
                _previouslyFocusedElement = Keyboard.FocusedElement
                
                ' Focus the overlay container itself to ensure key routing works correctly
                Dim unused1 = Dispatcher.InvokeAsync(Sub()
                                           Me.Focus()
                                           Keyboard.Focus(Me)
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            Else
                ' Restore focus to the previously active control
                If _previouslyFocusedElement IsNot Nothing Then
                    Dim unused2 = Dispatcher.InvokeAsync(Sub()
                                               _previouslyFocusedElement.Focus()
                                               Keyboard.Focus(_previouslyFocusedElement)
                                           End Sub, System.Windows.Threading.DispatcherPriority.Input)
                End If
            End If
        End Sub

        Public Sub Background_MouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim vm = TryCast(DataContext, ShortcutsOverlayViewModel)
            If vm IsNot Nothing Then
                vm.CloseCommand.Execute(Nothing)
            End If
        End Sub

        Public Sub OverlayPanel_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Suppress bubbling to prevent click-away closing
            e.Handled = True
        End Sub

    End Class

End Namespace
