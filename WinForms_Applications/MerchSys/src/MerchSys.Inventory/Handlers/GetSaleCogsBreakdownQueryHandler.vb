Imports System.Threading
Imports System.Linq
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetSaleCogsBreakdownQuery"/> sent by the Accounting module.
    ''' Queries the persisted per-batch FIFO COGS records for a sale.
    ''' </summary>
    Public Class GetSaleCogsBreakdownQueryHandler
        Implements IRequestHandler(Of GetSaleCogsBreakdownQuery, GetSaleCogsBreakdownResult)

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of GetSaleCogsBreakdownQueryHandler)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of GetSaleCogsBreakdownQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetSaleCogsBreakdownQuery, cancellationToken As CancellationToken) As Task(Of GetSaleCogsBreakdownResult) Implements IRequestHandler(Of GetSaleCogsBreakdownQuery, GetSaleCogsBreakdownResult).Handle
            _logger.LogInformation("Retrieving COGS breakdown for Tx {Tx} / Product {Product}.", request.TransactionId, request.ProductId)

            ' ACC-21: Project to SaleCogsLine DTO inside the LINQ tree to bypass the EF Core 10 VB.NET ToListAsync full entity discovery bug.
            Dim lines = Await _db.SaleCogsRecords.
                Where(Function(r) r.TransactionId = request.TransactionId AndAlso r.ProductId = request.ProductId).
                Select(Function(r) New SaleCogsLine With {
                    .BatchId = r.BatchId,
                    .QuantityDeducted = r.QuantityDeducted,
                    .UnitCost = r.UnitCost,
                    .Cogs = r.Cogs
                }).
                ToListAsync(cancellationToken)

            Dim totalCogs As Decimal = lines.Sum(Function(l) l.Cogs)

            _logger.LogInformation("Found {Count} batch lines for Tx {Tx} / Product {Product}. Total COGS = {Total}.",
                lines.Count, request.TransactionId, request.ProductId, totalCogs)

            Return New GetSaleCogsBreakdownResult With {
                .TotalCogs = totalCogs,
                .Lines = lines
            }
        End Function

    End Class

End Namespace
