Imports System.Globalization
Imports System.Windows.Data
Imports System.Windows.Media
Imports MerchSys.App.ViewModels.Shell

Namespace Converters

    Public Class SeverityToBrushConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type,
                                parameter As Object, culture As CultureInfo) As Object _
                                Implements IValueConverter.Convert
            Dim severity = If(TypeOf value Is IndicatorSeverity,
                              CType(value, IndicatorSeverity),
                              IndicatorSeverity.Idle)
            Select Case severity
                Case IndicatorSeverity.Healthy
                    Return New SolidColorBrush(Color.FromRgb(39, 174, 96))   ' #27AE60 green
                Case IndicatorSeverity.Warning
                    Return New SolidColorBrush(Color.FromRgb(243, 156, 18))  ' #F39C12 amber
                Case IndicatorSeverity.Critical
                    Return New SolidColorBrush(Color.FromRgb(231, 76, 60))   ' #E74C3C red
                Case Else
                    Return New SolidColorBrush(Color.FromRgb(149, 165, 166)) ' #95A5A6 gray (Idle)
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As Type,
                                    parameter As Object, culture As CultureInfo) As Object _
                                    Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function

    End Class

End Namespace
