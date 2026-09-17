Imports System.Threading
Imports System.Threading.Tasks
Imports MerchSys.POS.Entities

Namespace Services.ReceiptRendering

    ''' <summary>
    ''' Emits the receipt to the renderer's configured output channel
    ''' (console stream or PDF file). A future thermal-printer renderer
    ''' will plug into this same interface.
    ''' </summary>
    Public Interface IReceiptRenderer

        ''' <summary>
        ''' Emits the receipt to the renderer's configured output channel
        ''' (console stream or PDF file).
        ''' </summary>
        Function RenderAsync(
            receipt As OfficialReceipt,
            body As ReceiptBody,
            cancellationToken As CancellationToken
        ) As Task

    End Interface

End Namespace
