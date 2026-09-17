Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Text.RegularExpressions

Namespace Helpers

    Public Enum NumericInputMode
        None
        PositiveInteger
        PositiveDecimal
    End Enum

    ''' <summary>
    ''' Attached-property helper for forms to control field requirements, input filtering, and alignment.
    ''' </summary>
    Public Class FormHelper

        ' ── IsRequired Property ──────────────────────────────────────────────────

        Public Shared ReadOnly IsRequiredProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "IsRequired",
                GetType(Boolean),
                GetType(FormHelper),
                New PropertyMetadata(False))

        Public Shared Function GetIsRequired(dp As DependencyObject) As Boolean
            Return CBool(dp.GetValue(IsRequiredProperty))
        End Function

        Public Shared Sub SetIsRequired(dp As DependencyObject, value As Boolean)
            dp.SetValue(IsRequiredProperty, value)
        End Sub

        ' ── InputMode Property ────────────────────────────────────────────────────

        Public Shared ReadOnly InputModeProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "InputMode",
                GetType(NumericInputMode),
                GetType(FormHelper),
                New PropertyMetadata(NumericInputMode.None, AddressOf OnInputModeChanged))

        Public Shared Function GetInputMode(dp As DependencyObject) As NumericInputMode
            Return CType(dp.GetValue(InputModeProperty), NumericInputMode)
        End Function

        Public Shared Sub SetInputMode(dp As DependencyObject, value As NumericInputMode)
            dp.SetValue(InputModeProperty, value)
        End Sub

        Private Shared Sub OnInputModeChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim tb = TryCast(d, TextBox)
            If tb Is Nothing Then Return

            Dim newMode = CType(e.NewValue, NumericInputMode)
            
            RemoveHandler tb.PreviewTextInput, AddressOf OnPreviewTextInput
            RemoveHandler tb.LostFocus, AddressOf OnLostFocus
            DataObject.RemovePastingHandler(tb, AddressOf OnPasting)

            If newMode <> NumericInputMode.None Then
                AddHandler tb.PreviewTextInput, AddressOf OnPreviewTextInput
                AddHandler tb.LostFocus, AddressOf OnLostFocus
                DataObject.AddPastingHandler(tb, AddressOf OnPasting)
                
                tb.TextAlignment = TextAlignment.Right
            End If
        End Sub

        Private Shared Sub OnPreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            Dim tb = DirectCast(sender, TextBox)
            Dim mode = GetInputMode(tb)
            
            Dim currentText = tb.Text
            Dim selectionStart = tb.SelectionStart
            Dim selectionLength = tb.SelectionLength
            Dim newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, e.Text)

            Dim isValid = False
            If mode = NumericInputMode.PositiveInteger Then
                isValid = Regex.IsMatch(newText, "^\d*$")
            ElseIf mode = NumericInputMode.PositiveDecimal Then
                isValid = Regex.IsMatch(newText, "^\d*(\.\d{0,2})?$")
            End If

            e.Handled = Not isValid
        End Sub

        Private Shared Sub OnPasting(sender As Object, e As DataObjectPastingEventArgs)
            Dim tb = DirectCast(sender, TextBox)
            Dim mode = GetInputMode(tb)

            If e.DataObject.GetDataPresent(DataFormats.Text) Then
                Dim pastedText = CStr(e.DataObject.GetData(DataFormats.Text))
                Dim currentText = tb.Text
                Dim selectionStart = tb.SelectionStart
                Dim selectionLength = tb.SelectionLength
                Dim newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, pastedText)

                Dim isValid = False
                If mode = NumericInputMode.PositiveInteger Then
                    isValid = Regex.IsMatch(newText, "^\d*$")
                ElseIf mode = NumericInputMode.PositiveDecimal Then
                    isValid = Regex.IsMatch(newText, "^\d*(\.\d{0,2})?$")
                End If

                If Not isValid Then
                    e.CancelCommand()
                End If
            Else
                e.CancelCommand()
            End If
        End Sub

        Private Shared Sub OnLostFocus(sender As Object, e As RoutedEventArgs)
            Dim tb = DirectCast(sender, TextBox)
            Dim mode = GetInputMode(tb)
            If mode = NumericInputMode.PositiveDecimal Then
                Dim val As Decimal
                If Decimal.TryParse(tb.Text, val) Then
                    tb.Text = val.ToString("F2")
                Else
                    tb.Text = "0.00"
                End If
            ElseIf mode = NumericInputMode.PositiveInteger Then
                Dim val As Integer
                If Integer.TryParse(tb.Text, val) Then
                    tb.Text = val.ToString()
                Else
                    tb.Text = "0"
                End If
            End If
        End Sub

    End Class

End Namespace
