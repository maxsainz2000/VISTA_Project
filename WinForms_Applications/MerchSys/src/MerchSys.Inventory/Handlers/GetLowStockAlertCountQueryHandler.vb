Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetLowStockAlertCountQuery"/> sent by the Accounting module.
    ''' Returns the number of active low-stock alerts via <see cref="ILowStockAlertService"/>.
    ''' </summary>
    Public Class GetLowStockAlertCountQueryHandler
        Implements IRequestHandler(Of GetLowStockAlertCountQuery, Integer)

        Private ReadOnly _alertService As ILowStockAlertService
        Private ReadOnly _logger As ILogger(Of GetLowStockAlertCountQueryHandler)

        Public Sub New(alertService As ILowStockAlertService, logger As ILogger(Of GetLowStockAlertCountQueryHandler))
            _alertService = alertService
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetLowStockAlertCountQuery, cancellationToken As CancellationToken) As Task(Of Integer) Implements IRequestHandler(Of GetLowStockAlertCountQuery, Integer).Handle
            Dim alerts = Await _alertService.GetCurrentAlertsAsync()
            Dim count = alerts.Count

            _logger.LogDebug("GetLowStockAlertCountQuery returned {Count} active alerts.", count)
            Return count
        End Function

    End Class

End Namespace
