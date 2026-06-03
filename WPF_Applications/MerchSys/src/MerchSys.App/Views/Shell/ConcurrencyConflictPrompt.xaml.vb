Imports System.Windows

Namespace Views.Shell

    Public Partial Class ConcurrencyConflictPrompt
        Inherits Window

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub RefreshButton_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = True
            Close()
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
        End Sub

    End Class

End Namespace
