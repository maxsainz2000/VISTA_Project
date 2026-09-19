' Rule 19 known-good: Application.Current.MainWindow appears only in a comment
' Source: FinancialOverviewView.xaml.vb:33 (verified false positive in 2026-05-24 audit)
' The file deliberately avoids MainWindow — the comment EXPLAINS why.

Namespace Views
    Public Class FinancialOverviewView
        Private Sub NavigateToDetail()
            ' Application.Current.MainWindow is the LoginView (first window shown), not the shell.
            ' Iterate Application.Current.Windows instead to find the shell by DataContext type.
            Dim mainVm As Object = Nothing
            For Each win As System.Windows.Window In System.Windows.Application.Current.Windows
                Dim vm = TryCast(win.DataContext, Object)
                If vm IsNot Nothing Then
                    mainVm = vm
                    Exit For
                End If
            Next
        End Sub
    End Class
End Namespace
