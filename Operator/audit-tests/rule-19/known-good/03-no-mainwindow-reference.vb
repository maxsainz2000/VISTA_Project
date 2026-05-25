' Rule 19 known-good: No Application.Current.MainWindow reference at all
' File iterates Application.Current.Windows — the correct pattern.

Namespace Views
    Public Class SomeView
        Private Sub FindShellVm()
            For Each win As System.Windows.Window In System.Windows.Application.Current.Windows
                If TypeOf win.DataContext Is Object Then
                    Return
                End If
            Next
        End Sub
    End Class
End Namespace
