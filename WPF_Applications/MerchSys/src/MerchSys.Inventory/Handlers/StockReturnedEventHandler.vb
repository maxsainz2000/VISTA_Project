Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="StockReturnedEvent"/> published by the POS module when a sale is reversed.
    ''' Adds the returned quantity back into the FIFO stock pool via <see cref="IStockService"/>.
    ''' </summary>
    Public Class StockReturnedEventHandler
        Implements INotificationHandler(Of StockReturnedEvent)

        Private ReadOnly _stockService As IStockService
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of StockReturnedEventHandler)

        Public Sub New(stockService As IStockService, writeContext As IWriteContextScope, logger As ILogger(Of StockReturnedEventHandler))
            _stockService = stockService
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As StockReturnedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of StockReturnedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation(
                    "Processing StockReturnedEvent: ReturnId={ReturnId}, ProductId={ProductId} ({ProductName}), Qty={Qty}.",
                    notification.ReturnId, notification.ProductId, notification.ProductName, notification.QuantityReturned)

                Await _stockService.AddStockBatchAsync(
                    productId:=notification.ProductId,
                    qty:=notification.QuantityReturned,
                    unitCost:=notification.UnitPrice,
                    receiptDate:=notification.ReturnDate,
                    expiryDate:=Nothing,
                    sourcePOId:=Nothing,
                    movementType:=MovementType.[Return])

                _logger.LogInformation(
                    "Restocked {Qty} unit(s) of ProductId={ProductId} from return ReturnId={ReturnId}.",
                    notification.QuantityReturned, notification.ProductId, notification.ReturnId)
            End Using
        End Function

    End Class

End Namespace
