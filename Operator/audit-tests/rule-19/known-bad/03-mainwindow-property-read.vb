' Rule 19 known-bad: Application.Current.MainWindow standalone read (not assignment, not comment)
' Reading .MainWindow.Title — same antipattern, different member access.

Namespace Views
    Public Class SomeView
        Public Sub LogShellTitle()
            Dim title = System.Windows.Application.Current.MainWindow.Title
            System.Console.WriteLine(title)
        End Sub
    End Class
End Namespace
