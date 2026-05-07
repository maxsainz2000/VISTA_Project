Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles <see cref="GetProductCatalogQuery"/> sent by the POS module.
    ''' Returns active products with current stock levels for the POS cart search panel.
    ''' </summary>
    Public Class GetProductCatalogQueryHandler
        Implements IRequestHandler(Of GetProductCatalogQuery, GetProductCatalogResult)

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of GetProductCatalogQueryHandler)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of GetProductCatalogQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetProductCatalogQuery, cancellationToken As CancellationToken) As Task(Of GetProductCatalogResult) Implements IRequestHandler(Of GetProductCatalogQuery, GetProductCatalogResult).Handle
            _logger.LogDebug("GetProductCatalogQuery: SearchTerm='{SearchTerm}', ProductId={ProductId}.",
                request.SearchTerm, request.ProductId)

            Dim now As DateTime = DateTime.UtcNow

            Dim query = _db.Products _
                .Where(Function(p) Not p.IsDeleted AndAlso p.IsActive) _
                .Include(Function(p) p.StockBatches) _
                .AsQueryable()

            If request.ProductId.HasValue Then
                query = query.Where(Function(p) p.Id = request.ProductId.Value)
            End If

            If Not String.IsNullOrWhiteSpace(request.SearchTerm) Then
                Dim term = request.SearchTerm.Trim().ToLower()
                query = query.Where(Function(p) p.Name.ToLower().Contains(term) OrElse p.Sku.ToLower().Contains(term))
            End If

            Dim products = Await query.OrderBy(Function(p) p.Name).ToListAsync(cancellationToken)

            Dim result As New GetProductCatalogResult()

            For Each p In products
                Dim availableStock As Integer = p.StockBatches _
                    .Where(Function(b) b.QuantityRemaining > 0 AndAlso
                                       (Not b.ExpiryDate.HasValue OrElse b.ExpiryDate.Value >= now)) _
                    .Sum(Function(b) b.QuantityRemaining)

                result.Items.Add(New GetProductCatalogResult.ProductCatalogItem() With {
                    .ProductId = p.Id,
                    .ProductName = p.Name,
                    .Sku = p.Sku,
                    .UnitPrice = p.RetailPrice,
                    .AvailableStock = availableStock,
                    .IsLowStock = availableStock <= p.MinimumThreshold
                })
            Next

            _logger.LogDebug("GetProductCatalogQuery returned {Count} products.", result.Items.Count)
            Return result
        End Function

    End Class

End Namespace
