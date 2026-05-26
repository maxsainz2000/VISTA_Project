Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="ShrinkageRecordedEvent"/> within the Inventory module.
    ''' Triggers low-stock alert generation after stock is reduced by shrinkage.
    ''' </summary>
    Public Class ShrinkageRecordedHandler
        Implements INotificationHandler(Of ShrinkageRecordedEvent)

        Private ReadOnly _alertService As ILowStockAlertService
        Private ReadOnly _writeContext As IWriteContextScope
        Private ReadOnly _logger As ILogger(Of ShrinkageRecordedHandler)

        Public Sub New(alertService As ILowStockAlertService, writeContext As IWriteContextScope, logger As ILogger(Of ShrinkageRecordedHandler))
            _alertService = alertService
            _writeContext = writeContext
            _logger = logger
        End Sub

        Public Async Function Handle(notification As ShrinkageRecordedEvent, cancellationToken As CancellationToken) As Task Implements INotificationHandler(Of ShrinkageRecordedEvent).Handle
            Using _writeContext.Enter(WriteContextKind.System)
                _logger.LogInformation(
                    "Checking low-stock alerts after shrinkage for ProductId={ProductId} ({ProductName}), QtyLost={Qty}.",
                    notification.ProductId, notification.ProductName, notification.QuantityLost)

                Dim alerts = Await _alertService.CheckAndGenerateAlertsAsync()

                If alerts.Count > 0 Then
                    _logger.LogWarning("{AlertCount} low-stock alert(s) triggered after shrinkage event.", alerts.Count)
                End If
            End Using
        End Function

    End Class

End Namespace
