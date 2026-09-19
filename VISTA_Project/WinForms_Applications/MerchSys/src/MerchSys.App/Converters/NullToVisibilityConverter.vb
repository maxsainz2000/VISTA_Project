Imports System.Windows
Imports System.Windows.Data
Imports System.Globalization

Namespace Converters

    Public Class NullToVisibilityConverter
        Implements IValueConverter

        Public Property Invert As Boolean = False

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim isNull As Boolean = (value Is Nothing)
            If Invert Then
                Return If(isNull, Visibility.Visible, Visibility.Collapsed)
            Else
                Return If(isNull, Visibility.Collapsed, Visibility.Visible)
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace
