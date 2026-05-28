Imports System.Threading
Imports MediatR
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Services
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
        Private _catalogProductList As List(Of Product)

        Public Sub New(db As InventoryDbContext, logger As ILogger(Of GetProductCatalogQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetProductCatalogQuery, cancellationToken As CancellationToken) As Task(Of GetProductCatalogResult) Implements IRequestHandler(Of GetProductCatalogQuery, GetProductCatalogResult).Handle
            _logger.LogDebug("GetProductCatalogQuery: SearchTerm='{SearchTerm}', ProductId={ProductId}.",
                request.SearchTerm, request.ProductId)

            Dim now As DateTime = DateTime.UtcNow
            _catalogProductList = New List(Of Product)()
            Dim catConnStr = _db.Database.GetConnectionString()
            Using catConn As New MySqlConnection(catConnStr)
                Await catConn.OpenAsync()

                Dim catSql = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                              "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                              "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                              "FROM Inv_Products WHERE IsDeleted = 0 AND IsActive = 1"
                If request.ProductId.HasValue Then catSql &= " AND Id = @productId"
                If Not String.IsNullOrWhiteSpace(request.SearchTerm) Then catSql &= " AND (lower(Name) LIKE @term OR lower(Sku) LIKE @term)"
                catSql &= " ORDER BY Name"

                Using catCmd = catConn.CreateCommand()
                    catCmd.CommandText = catSql
                    If request.ProductId.HasValue Then catCmd.Parameters.Add(New MySqlParameter("@productId", request.ProductId.Value))
                    If Not String.IsNullOrWhiteSpace(request.SearchTerm) Then
                        catCmd.Parameters.Add(New MySqlParameter("@term", "%" & request.SearchTerm.Trim().ToLower() & "%"))
                    End If
                    Using catReader = catCmd.ExecuteReader()
                        While catReader.Read()
                            _catalogProductList.Add(StockService.ReadProduct(catReader))
                        End While
                    End Using
                End Using

                If _catalogProductList.Count > 0 Then
                    Dim pIds = String.Join(",", _catalogProductList.Select(Function(p) p.Id))
                    Dim batchMap As New Dictionary(Of Integer, List(Of StockBatch))()
                    Using bCmd = catConn.CreateCommand()
                        bCmd.CommandText = "SELECT Id, ProductId, QuantityReceived, QuantityRemaining, UnitCost, " &
                                           "ReceiptDate, ExpiryDate, SourcePurchaseOrderId, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_StockBatches WHERE ProductId IN ({pIds})"
                        Using bReader = bCmd.ExecuteReader()
                            While bReader.Read()
                                Dim b = StockService.ReadStockBatch(bReader)
                                If Not batchMap.ContainsKey(b.ProductId) Then batchMap(b.ProductId) = New List(Of StockBatch)()
                                batchMap(b.ProductId).Add(b)
                            End While
                        End Using
                    End Using
                    For Each p In _catalogProductList
                        Dim pBatches As List(Of StockBatch) = Nothing
                        p.StockBatches = If(batchMap.TryGetValue(p.Id, pBatches), pBatches, New List(Of StockBatch)())
                    Next
                End If
            End Using
            Dim products As List(Of Product) = _catalogProductList

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
