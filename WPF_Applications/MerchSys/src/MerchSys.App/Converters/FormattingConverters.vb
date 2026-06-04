Imports System.Windows
Imports System.Windows.Data
Imports System.Globalization

Namespace Converters

    Public Class PesoConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return "₱0.00"
            End If
            
            Dim decValue As Decimal
            If Decimal.TryParse(value.ToString(), decValue) Then
                Return "₱" & decValue.ToString("N2", culture)
            End If
            
            Return "₱0.00"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return 0D
            End If
            
            strValue = strValue.Replace("₱", "").Trim()
            Dim decValue As Decimal
            If Decimal.TryParse(strValue, NumberStyles.Any, culture, decValue) Then
                Return decValue
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class PesoNoDecimalConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return "₱0"
            End If
            
            Dim decValue As Decimal
            If Decimal.TryParse(value.ToString(), decValue) Then
                Return "₱" & decValue.ToString("N0", culture)
            End If
            
            Return "₱0"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return 0D
            End If
            
            strValue = strValue.Replace("₱", "").Trim()
            Dim decValue As Decimal
            If Decimal.TryParse(strValue, NumberStyles.Any, culture, decValue) Then
                Return decValue
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class SignedPesoConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return "− ₱0.00"
            End If
            
            Dim decValue As Decimal
            If Decimal.TryParse(value.ToString(), decValue) Then
                Dim absValue = Math.Abs(decValue)
                Return "− ₱" & absValue.ToString("N2", culture)
            End If
            
            Return "− ₱0.00"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return 0D
            End If
            
            strValue = strValue.Replace("−", "").Replace("₱", "").Trim()
            Dim decValue As Decimal
            If Decimal.TryParse(strValue, NumberStyles.Any, culture, decValue) Then
                Return -Math.Abs(decValue)
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class DateFormatter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return String.Empty
            End If
            
            If TypeOf value Is DateTime Then
                Dim dt = DirectCast(value, DateTime)
                Dim formatStr = If(TypeOf parameter Is String, DirectCast(parameter, String), "MM/dd/yyyy")
                Return dt.ToString(formatStr, culture)
            End If
            
            Return value.ToString()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return Nothing
            End If
            
            Dim dt As DateTime
            If DateTime.TryParse(strValue, culture, DateTimeStyles.None, dt) Then
                Return dt
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class DateTimeFormatter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return String.Empty
            End If
            
            If TypeOf value Is DateTime Then
                Dim dt = DirectCast(value, DateTime)
                Dim formatStr = If(TypeOf parameter Is String, DirectCast(parameter, String), "MM/dd/yyyy HH:mm")
                Return dt.ToString(formatStr, culture)
            End If
            
            Return value.ToString()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return Nothing
            End If
            
            Dim dt As DateTime
            If DateTime.TryParse(strValue, culture, DateTimeStyles.None, dt) Then
                Return dt
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class QuantityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing OrElse IsDBNull(value) Then
                Return "0.00"
            End If
            
            Dim decValue As Decimal
            If Decimal.TryParse(value.ToString(), decValue) Then
                Dim formatStr = If(TypeOf parameter Is String, DirectCast(parameter, String), "N2")
                Return decValue.ToString(formatStr, culture)
            End If
            
            Return "0.00"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim strValue As String = TryCast(value, String)
            If String.IsNullOrWhiteSpace(strValue) Then
                Return 0D
            End If
            
            Dim decValue As Decimal
            If Decimal.TryParse(strValue, NumberStyles.Any, culture, decValue) Then
                Return decValue
            End If
            
            Return DependencyProperty.UnsetValue
        End Function
    End Class

End Namespace
