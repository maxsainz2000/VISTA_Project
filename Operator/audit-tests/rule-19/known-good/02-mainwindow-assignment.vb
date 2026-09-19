' Rule 19 known-good: Application.Current.MainWindow = x (assignment — recommended remediation)
' Prevention option 3 from the wiki: explicitly set MainWindow after showing the shell.
' Assignments are NOT the antipattern; the antipattern is reading/dereferencing MainWindow.

Namespace App
    Public Class AppStartup
        Private _shellWindow As System.Windows.Window

        Public Sub HandleLoginSucceeded()
            _shellWindow = New System.Windows.Window()
            _shellWindow.Show()
            System.Windows.Application.Current.MainWindow = _shellWindow
        End Sub
    End Class
End Namespace
