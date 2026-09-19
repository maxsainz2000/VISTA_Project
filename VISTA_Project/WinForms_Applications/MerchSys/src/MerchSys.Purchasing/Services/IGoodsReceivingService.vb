Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Interface IGoodsReceivingService

        ''' <summary>
        ''' Records receipt of goods against a Submitted purchase order.
        ''' Flags discrepancies, generates a GR-YYYY-XXXX receipt number,
        ''' transitions the PO to Received, and publishes a GoodsReceivedEvent.
        ''' </summary>
        Function ReceiveGoodsAsync(purchaseOrderId As Integer, lines As List(Of ReceiveGoodsLineDto)) As Task(Of GoodsReceipt)

        ''' <summary>Returns a single goods receipt with its line items, or Nothing if not found.</summary>
        Function GetReceiptByIdAsync(id As Integer) As Task(Of GoodsReceipt)

        ''' <summary>Returns all goods receipts (with line items) recorded against the given purchase order.</summary>
        Function GetReceiptsForPOAsync(purchaseOrderId As Integer) As Task(Of List(Of GoodsReceipt))

    End Interface

End Namespace
