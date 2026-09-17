Imports System.Threading.Tasks
Imports System.Windows
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation

Namespace Services

    ''' <summary>
    ''' Concrete implementation of IConfirmationPresenter that displays a modal confirmation dialog.
    ''' </summary>
    Public Class DefaultConfirmationPresenter
        Implements IConfirmationPresenter

        Public Function PromptAsync(request As ConfirmationRequest) As Task(Of Boolean) Implements IConfirmationPresenter.PromptAsync
            Dim tcs As New TaskCompletionSource(Of Boolean)()

            Application.Current.Dispatcher.Invoke(Sub()
                Try
                    Dim dialog As New Views.Shell.ConfirmationDialog(request)
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
