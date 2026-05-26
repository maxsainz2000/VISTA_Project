Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="SaleCompletedEvent"/> published by the POS module.
    ''' Deducts stock via FIFO for each item sold, then triggers low-stock alert generation.
    ''' </summary>
    Public Class SaleCompletedHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _stockService As IStockService
        Private ReadOnly _alertService As ILowStockAlertService
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SaleCompletedHandler)

        Public Sub New(stockService As IStockService, alertService As ILowStockAlertService, writeContext As IWriteContextScope, logger As ILogger(Of SaleCompletedHandler))
            _stockService = stockService
            _alertService = alertService
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of SaleCompletedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
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

                Dim alerts = Await _alertService.CheckAndGenerateAlertsAsync()
                If alerts.Count > 0 Then
                    _logger.LogWarning("{AlertCount} low-stock alert(s) triggered after sale Transaction {TransactionId}.", alerts.Count, notification.TransactionId)
                End If
            End Using
        End Function

    End Class

End Namespace
