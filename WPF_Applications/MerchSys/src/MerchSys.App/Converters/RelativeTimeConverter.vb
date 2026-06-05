Imports System.Windows.Data
Imports System.Globalization

Namespace Converters

    ''' <summary>
    ''' Converts a DateTime? loading timestamp into a relative "just now", "Nm ago", or "Nh ago" string.
    ''' </summary>
    Public Class RelativeTimeConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing Then Return String.Empty
            If Not (TypeOf value Is DateTime) Then Return String.Empty

            Dim loadedAt = DirectCast(value, DateTime)
            Dim diff = DateTime.Now - loadedAt
            If diff.TotalSeconds < 60 Then
                Return "just now"
            ElseIf diff.TotalMinutes < 60 Then
                Dim minutes = CInt(Math.Floor(diff.TotalMinutes))
                Return $"{minutes}m ago"
            Else
                Dim hours = CInt(Math.Floor(diff.TotalHours))
                Return $"{hours}h ago"
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace
