Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Events

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="SaleCompletedEvent"/> published by the POS module.
    ''' Deducts stock via FIFO for each item sold. COGS data remains within Inventory;
    ''' Accounting calculates its own COGS by handling the same event independently.
    ''' </summary>
    Public Class SaleCompletedHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _stockService As IStockService
        Private ReadOnly _logger As ILogger(Of SaleCompletedHandler)

        Public Sub New(stockService As IStockService, logger As ILogger(Of SaleCompletedHandler))
            _stockService = stockService
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of SaleCompletedEvent).Handle
            _logger.LogInformation("Processing SaleCompletedEvent for Transaction {TransactionId} with {ItemCount} items.", notification.TransactionId, notification.Items.Count)

            For Each item In notification.Items
                Try
                    Dim deductions = Await _stockService.DeductStockFIFOAsync(item.ProductId, item.Quantity)
                    Dim totalCogs As Decimal = deductions.Sum(Function(d) d.COGS)
                    _logger.LogInformation("FIFO deduction for Product {ProductId} ({ProductName}): Qty={Qty}, COGS={COGS}.",
                        item.ProductId, item.ProductName, item.Quantity, totalCogs)
                Catch ex As InsufficientStockException
                    _logger.LogError(ex, "Insufficient stock during sale for Product {ProductId}: Requested={Requested}, Available={Available}.",
                        ex.ProductId, ex.RequestedQuantity, ex.AvailableQuantity)
                    Throw
                End Try
            Next
        End Function

    End Class

End Namespace
