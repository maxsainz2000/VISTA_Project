Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GoodsReceivedEvent"/> published by the Purchasing module.
    ''' Creates a new FIFO stock batch for each line item in the receipt.
    ''' </summary>
    Public Class GoodsReceivedHandler
        Implements INotificationHandler(Of GoodsReceivedEvent)

        Private ReadOnly _stockService As IStockService
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of GoodsReceivedHandler)

        Public Sub New(stockService As IStockService, writeContext As IWriteContextScope, logger As ILogger(Of GoodsReceivedHandler))
            _stockService = stockService
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As GoodsReceivedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of GoodsReceivedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Processing GoodsReceivedEvent for PO {PurchaseOrderId} with {ItemCount} items.", notification.PurchaseOrderId, notification.Items.Count)

                For Each item In notification.Items
                    Await _stockService.AddStockBatchAsync(
                        item.ProductId,
                        item.QuantityReceived,
                        item.UnitCost,
                        notification.ReceivedDate,
                        item.ExpiryDate,
                        notification.PurchaseOrderId)

                    _logger.LogInformation("Stock batch created for Product {ProductId} ({ProductName}): Qty={Qty}, UnitCost={UnitCost}.",
                        item.ProductId, item.ProductName, item.QuantityReceived, item.UnitCost)
                Next
            End Using
        End Function

    End Class

End Namespace
