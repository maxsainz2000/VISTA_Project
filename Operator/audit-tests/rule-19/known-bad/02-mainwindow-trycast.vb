' Rule 19 known-bad: TryCast(Application.Current.MainWindow, ...) in executable code
' Reading MainWindow to find the shell window — always returns LoginView in VISTA.

Namespace Views
    Public Class SomeView
        Private Sub OnNavigate()
            Dim shell = TryCast(System.Windows.Application.Current.MainWindow, System.Windows.Window)
            If shell IsNot Nothing Then
                shell.Show()
            End If
        End Sub
    End Class
End Namespace
