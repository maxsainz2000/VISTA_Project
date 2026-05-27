Imports System
Imports System.Threading
Imports System.Linq
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="SaleCompletedEvent"/> published by the POS module.
    ''' Deducts stock via FIFO for each item sold, persists the exact COGS breakdown,
    ''' and triggers low-stock alert generation.
    ''' </summary>
    Public Class SaleCompletedHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _stockService As IStockService
        Private ReadOnly _alertService As ILowStockAlertService
        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _repository As ISyncableRepository(Of InventoryDbContext)
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of SaleCompletedHandler)

        Public Sub New(stockService As IStockService, alertService As ILowStockAlertService, db As InventoryDbContext, repository As ISyncableRepository(Of InventoryDbContext), writeContext As IWriteContextScope, logger As ILogger(Of SaleCompletedHandler))
            _stockService = stockService
            _alertService = alertService
            _db = db
            _repository = repository
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As SaleCompletedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of SaleCompletedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation("Processing SaleCompletedEvent for Transaction {TransactionId} with {ItemCount} items.", notification.TransactionId, notification.Items.Count)

                For Each item In notification.Items
                    Try
                        Dim deductions = Await _stockService.DeductStockFIFOAsync(item.ProductId, item.Quantity)
                        
                        ' ACC-21: Persist the FIFO COGS breakdown to Inv_SaleCogs
                        Await PersistCogsBreakdownAsync(notification.TransactionId, item.ProductId, deductions, cancellationToken)
                        
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

        Private Async Function PersistCogsBreakdownAsync(transactionId As Integer, productId As Integer, deductions As List(Of FIFODeductionResult), cancellationToken As CancellationToken) As Task
            ' ACC-21: Idempotency check to avoid duplicate rows on event re-publish
            Dim alreadyPersisted = Await _db.SaleCogsRecords.AnyAsync(
                Function(r) r.TransactionId = transactionId AndAlso r.ProductId = productId,
                cancellationToken)

            If alreadyPersisted Then
                _logger.LogInformation("COGS breakdown already persisted for Tx {Tx} / Product {Product}. Skipping.", transactionId, productId)
                Return
            End If

            For Each line In deductions
                Dim record As New SaleCogsRecord With {
                    .TransactionId = transactionId,
                    .ProductId = productId,
                    .BatchId = line.BatchId,
                    .QuantityDeducted = line.QuantityDeducted,
                    .UnitCost = line.UnitCost,
                    .Cogs = line.COGS,
                    .DeductedAt = DateTime.UtcNow
                }
                _db.SaleCogsRecords.Add(record)
            Next

            Await _repository.SaveChangesWithJournalAsync(cancellationToken)
        End Function

    End Class

End Namespace
