Imports System.Threading
Imports System.Threading.Tasks
Imports MerchSys.POS.Entities

Namespace Services.ReceiptRendering

    ''' <summary>
    ''' Emits the receipt body text directly to the system console.
    ''' Used as the default development and diagnostic output channel.
    ''' </summary>
    Public Class ConsoleReceiptRenderer
        Implements IReceiptRenderer

        Public Function RenderAsync(
            receipt As OfficialReceipt,
            body As ReceiptBody,
            cancellationToken As CancellationToken
        ) As Task Implements IReceiptRenderer.RenderAsync

            Dim rendered = String.Join(Environment.NewLine, body.AllLines)
            System.Console.WriteLine(rendered)
            Return Task.CompletedTask
        End Function

    End Class

End Namespace
