Imports MerchSys.POS.Entities

Namespace Services

    Public Interface IReceiptService
        Function GenerateReceiptAsync(transactionId As Integer) As Task(Of OfficialReceipt)
        Function GetReceiptAsync(receiptNumber As String) As Task(Of OfficialReceipt)
        Function GetReceiptByTransactionAsync(transactionId As Integer) As Task(Of OfficialReceipt)
        Function PrintReceiptAsync(receiptId As Integer) As Task
    End Interface

End Namespace
