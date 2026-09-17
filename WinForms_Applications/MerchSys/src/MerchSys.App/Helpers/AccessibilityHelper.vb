Imports System.Windows
Imports System.Windows.Input

Namespace Helpers

    ''' <summary>
    ''' Provides accessibility-related attached properties for keyboard focus and navigation containment.
    ''' </summary>
    Public Class AccessibilityHelper

        Public Shared ReadOnly IsFocusTrapProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "IsFocusTrap",
                GetType(Boolean),
                GetType(AccessibilityHelper),
                New PropertyMetadata(False, AddressOf OnIsFocusTrapChanged))

        Public Shared Function GetIsFocusTrap(dp As DependencyObject) As Boolean
            Return CBool(dp.GetValue(IsFocusTrapProperty))
        End Function

        Public Shared Sub SetIsFocusTrap(dp As DependencyObject, value As Boolean)
            dp.SetValue(IsFocusTrapProperty, value)
        End Sub

        Private Shared Sub OnIsFocusTrapChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim element = TryCast(d, UIElement)
            If element Is Nothing Then Return

            ' Cycle is what actually contains Tab/Shift+Tab (and Ctrl+Tab) within the modal.
            ' We deliberately do NOT set FocusManager.IsFocusScope here: a focus scope is meant for
            ' menus/toolbars and diverges logical from keyboard focus, which can interfere with
            ' IsDefault/RoutedCommand resolution and focus restoration on a modal overlay.
            If CBool(e.NewValue) Then
                KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.Cycle)
                KeyboardNavigation.SetControlTabNavigation(element, KeyboardNavigationMode.Cycle)
            Else
                KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.Continue)
                KeyboardNavigation.SetControlTabNavigation(element, KeyboardNavigationMode.Continue)
            End If
        End Sub

    End Class

End Namespace
