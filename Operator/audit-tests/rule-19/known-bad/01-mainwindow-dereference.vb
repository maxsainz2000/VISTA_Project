' Rule 19 known-bad: Application.Current.MainWindow?.DataContext dereference in executable code
' This is the exact antipattern — silently returns Nothing because MainWindow is LoginView.

Namespace ViewModels
    Public Class SomeViewModel
        Public Sub NavigateToAccounting()
            Dim mainWindow = TryCast(System.Windows.Application.Current.MainWindow?.DataContext, Object)
            If mainWindow Is Nothing Then Return
            System.Console.WriteLine("Navigating...")
        End Sub
    End Class
End Namespace
