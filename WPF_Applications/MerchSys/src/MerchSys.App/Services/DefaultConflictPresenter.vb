Imports System.Threading.Tasks
Imports System.Windows
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    ''' <summary>
    ''' Concrete implementation of IConflictPresenter that displays a modal prompt dialog.
    ''' </summary>
    Public Class DefaultConflictPresenter
        Implements IConflictPresenter

        Public Function PromptAsync() As Task(Of Boolean) Implements IConflictPresenter.PromptAsync
            Dim tcs As New TaskCompletionSource(Of Boolean)()

            Application.Current.Dispatcher.Invoke(Sub()
                Try
                    Dim dialog As New Views.Shell.ConcurrencyConflictPrompt()
                    dialog.Owner = Application.Current.MainWindow
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner
                    Dim result = dialog.ShowDialog()
                    tcs.SetResult(result.HasValue AndAlso result.Value)
                Catch ex As Exception
                    tcs.SetException(ex)
                End Try
            End Sub)

            Return tcs.Task
        End Function

    End Class

End Namespace
