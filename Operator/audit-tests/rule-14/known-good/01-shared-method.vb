' Rule 14 known-good: Shared method with parameter matching a class field name
' Shared methods have no instance scope — parameter cannot shadow an instance property.
' Source: VelocityService.vb Classify method (verified false positive in 2026-05-24 audit)

Namespace Services
    Public Class VelocityService
        Public Property AvgDailySales As Decimal

        Private Shared Function Classify(avgDailySales As Decimal) As String
            ' 'avgDailySales' matches property above by name, but Shared has no instance scope
            If avgDailySales > 10 Then Return "Fast"
            If avgDailySales > 3 Then Return "Medium"
            Return "Slow"
        End Function
    End Class
End Namespace
