Imports System.Threading
Imports MediatR
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetCurrentStockQuery"/> from Purchasing (reorder engine) or any cross-module caller.
    ''' Returns aggregated stock levels across all non-expired FIFO batches.
    ''' </summary>
    Public Class GetCurrentStockHandler
        Implements IRequestHandler(Of GetCurrentStockQuery, GetCurrentStockResult)

        Private ReadOnly _stockService As IStockService

        Public Sub New(stockService As IStockService)
            _stockService = stockService
        End Sub

        Public Async Function Handle(request As GetCurrentStockQuery, cancellationToken As CancellationToken) As Task(Of GetCurrentStockResult) Implements IRequestHandler(Of GetCurrentStockQuery, GetCurrentStockResult).Handle
            Dim levels = Await _stockService.GetCurrentStockAsync(request.ProductId)

            Dim result As New GetCurrentStockResult()
            For Each level In levels
                result.Items.Add(New GetCurrentStockResult.StockLevel With {
                    .ProductId = level.ProductId,
                    .ProductName = level.ProductName,
                    .CurrentQuantity = level.CurrentQuantity,
                    .MinimumThreshold = level.MinimumThreshold,
                    .IsBelowThreshold = level.IsBelowThreshold
                })
            Next
            Return result
        End Function

    End Class

End Namespace
