Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class InventoryAuditService
        Implements IInventoryAuditService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _logger As ILogger(Of InventoryAuditService)

        Public Sub New(db As InventoryDbContext,
                       logger As ILogger(Of InventoryAuditService))
            _db = db
            _logger = logger
        End Sub

        Public Async Function PerformStockCountAsync(productId As Integer, physicalCount As Integer, performedBy As String, notes As String) As Task(Of StockAuditRecord) Implements IInventoryAuditService.PerformStockCountAsync
            If physicalCount < 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(physicalCount), "Physical count cannot be negative.")
            End If
            If String.IsNullOrWhiteSpace(performedBy) Then
                Throw New ArgumentException("PerformedBy is required.", NameOf(performedBy))
            End If

            Dim product As Product = Await _db.Products _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)
            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim expectedQty As Integer = If(
                Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = productId) _
                    .Select(Function(b) CType(b.QuantityRemaining, Integer?)) _
                    .SumAsync(),
                0)

            Dim variance As Integer = physicalCount - expectedQty
            Dim now As DateTime = DateTime.UtcNow

            Dim record As New StockAuditRecord With {
                .ProductId = productId,
                .ExpectedQuantity = expectedQty,
                .PhysicalCount = physicalCount,
                .Variance = variance,
                .Reason = "Stock Count",
                .Notes = If(notes, String.Empty),
                .PerformedBy = performedBy,
                .AuditedAt = now,
                .CreatedBy = performedBy,
                .CreatedAt = now
            }
            _db.StockAuditRecords.Add(record)

            If variance <> 0 Then
                _db.StockMovements.Add(New StockMovement With {
                    .ProductId = productId,
                    .MovementType = MovementType.Adjustment,
                    .Quantity = variance,
                    .OccurredAt = now,
                    .CreatedBy = performedBy,
                    .CreatedAt = now
                })
            End If

            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Stock count performed: ProductId={ProductId}, Expected={Expected}, Physical={Physical}, Variance={Variance}, By={By}",
                productId, expectedQty, physicalCount, variance, performedBy)

            Return record
        End Function

        Public Async Function RecordAdjustmentAsync(productId As Integer, adjustedQuantity As Integer, reason As String, performedBy As String) As Task(Of StockAuditRecord) Implements IInventoryAuditService.RecordAdjustmentAsync
            If String.IsNullOrWhiteSpace(reason) Then
                Throw New ArgumentException("Reason is required.", NameOf(reason))
            End If
            If String.IsNullOrWhiteSpace(performedBy) Then
                Throw New ArgumentException("PerformedBy is required.", NameOf(performedBy))
            End If

            Dim product As Product = Await _db.Products _
                .FirstOrDefaultAsync(Function(p) p.Id = productId AndAlso Not p.IsDeleted)
            If product Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} not found.")
            End If

            Dim currentQty As Integer = If(
                Await _db.StockBatches _
                    .Where(Function(b) b.ProductId = productId) _
                    .Select(Function(b) CType(b.QuantityRemaining, Integer?)) _
                    .SumAsync(),
                0)

            Dim now As DateTime = DateTime.UtcNow

            Dim record As New StockAuditRecord With {
                .ProductId = productId,
                .ExpectedQuantity = currentQty,
                .PhysicalCount = adjustedQuantity,
                .Variance = adjustedQuantity - currentQty,
                .Reason = reason,
                .Notes = String.Empty,
                .PerformedBy = performedBy,
                .AuditedAt = now,
                .CreatedBy = performedBy,
                .CreatedAt = now
            }
            _db.StockAuditRecords.Add(record)

            Dim variance As Integer = adjustedQuantity - currentQty
            If variance <> 0 Then
                _db.StockMovements.Add(New StockMovement With {
                    .ProductId = productId,
                    .MovementType = MovementType.Adjustment,
                    .Quantity = variance,
                    .OccurredAt = now,
                    .CreatedBy = performedBy,
                    .CreatedAt = now
                })
            End If

            Await _db.SaveChangesAsync()

            _logger.LogInformation(
                "Stock adjustment recorded: ProductId={ProductId}, Adjusted={Adjusted}, Variance={Variance}, Reason={Reason}, By={By}",
                productId, adjustedQuantity, variance, reason, performedBy)

            Return record
        End Function

        Public Async Function GetAuditHistoryAsync(Optional productId As Integer? = Nothing, Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetAuditHistoryAsync
            Dim auditHistoryList As New List(Of StockAuditRecord)()
            Dim ahConnStr = _db.Database.GetConnectionString()
            Using ahConn As New MySqlConnection(ahConnStr)
                Await ahConn.OpenAsync()

                Dim ahSql = "SELECT Id, ProductId, ExpectedQuantity, PhysicalCount, Variance, Reason, Notes, " &
                             "PerformedBy, AuditedAt, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                             "FROM Inv_StockAuditRecords WHERE 1=1"
                If productId.HasValue Then ahSql &= " AND ProductId = @productId"
                If startDate.HasValue Then ahSql &= " AND AuditedAt >= @startDate"
                If endDate.HasValue Then ahSql &= " AND AuditedAt <= @endDate"
                ahSql &= " ORDER BY AuditedAt DESC"

                Using ahCmd = ahConn.CreateCommand()
                    ahCmd.CommandText = ahSql
                    If productId.HasValue Then ahCmd.Parameters.Add(New MySqlParameter("@productId", productId.Value))
                    If startDate.HasValue Then ahCmd.Parameters.Add(New MySqlParameter("@startDate", startDate.Value))
                    If endDate.HasValue Then ahCmd.Parameters.Add(New MySqlParameter("@endDate", endDate.Value))
                    Using ahReader = ahCmd.ExecuteReader()
                        While ahReader.Read()
                            auditHistoryList.Add(New StockAuditRecord With {
                                .Id = ahReader.GetInt32(0),
                                .ProductId = ahReader.GetInt32(1),
                                .ExpectedQuantity = ahReader.GetInt32(2),
                                .PhysicalCount = ahReader.GetInt32(3),
                                .Variance = ahReader.GetInt32(4),
                                .Reason = ahReader.GetString(5),
                                .Notes = If(ahReader.IsDBNull(6), Nothing, ahReader.GetString(6)),
                                .PerformedBy = ahReader.GetString(7),
                                .AuditedAt = ahReader.GetDateTime(8),
                                .CreatedBy = ahReader.GetString(9),
                                .CreatedAt = ahReader.GetDateTime(10),
                                .ModifiedBy = If(ahReader.IsDBNull(11), Nothing, ahReader.GetString(11)),
                                .ModifiedAt = If(ahReader.IsDBNull(12), Nothing, CType(ahReader.GetDateTime(12), DateTime?))
                            })
                        End While
                    End Using
                End Using

                If auditHistoryList.Count > 0 Then
                    Dim pIds = String.Join(",", auditHistoryList.Select(Function(a) a.ProductId).Distinct())
                    Dim productMap As New Dictionary(Of Integer, Product)()
                    Using pCmd = ahConn.CreateCommand()
                        pCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                           "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_Products WHERE Id IN ({pIds})"
                        Using pReader = pCmd.ExecuteReader()
                            While pReader.Read()
                                Dim p = StockService.ReadProduct(pReader)
                                productMap(p.Id) = p
                            End While
                        End Using
                    End Using
                    For Each rec In auditHistoryList
                        Dim prod As Product = Nothing
                        If productMap.TryGetValue(rec.ProductId, prod) Then rec.Product = prod
                    Next
                End If
            End Using
            Return auditHistoryList
        End Function

        Public Async Function GetLatestAuditPerProductAsync() As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetLatestAuditPerProductAsync
            Dim latestAudits As New List(Of StockAuditRecord)()
            Dim connStr = _db.Database.GetConnectionString()
            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                
                Dim sql = "SELECT a.Id, a.ProductId, a.ExpectedQuantity, a.PhysicalCount, a.Variance, a.Reason, a.Notes, " &
                          "a.PerformedBy, a.AuditedAt, a.CreatedBy, a.CreatedAt, a.ModifiedBy, a.ModifiedAt " &
                          "FROM Inv_StockAuditRecords a " &
                          "WHERE a.Id = (" &
                          "    SELECT sub.Id " &
                          "    FROM Inv_StockAuditRecords sub " &
                          "    WHERE sub.ProductId = a.ProductId " &
                          "    ORDER BY sub.AuditedAt DESC, sub.Id DESC " &
                          "    LIMIT 1" &
                          ")"

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = sql
                    Using reader = Await cmd.ExecuteReaderAsync()
                        While reader.Read()
                            latestAudits.Add(New StockAuditRecord With {
                                .Id = reader.GetInt32(0),
                                .ProductId = reader.GetInt32(1),
                                .ExpectedQuantity = reader.GetInt32(2),
                                .PhysicalCount = reader.GetInt32(3),
                                .Variance = reader.GetInt32(4),
                                .Reason = reader.GetString(5),
                                .Notes = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                                .PerformedBy = reader.GetString(7),
                                .AuditedAt = reader.GetDateTime(8),
                                .CreatedBy = reader.GetString(9),
                                .CreatedAt = reader.GetDateTime(10),
                                .ModifiedBy = If(reader.IsDBNull(11), Nothing, reader.GetString(11)),
                                .ModifiedAt = If(reader.IsDBNull(12), Nothing, CType(reader.GetDateTime(12), DateTime?))
                            })
                        End While
                    End Using
                End Using

                If latestAudits.Count > 0 Then
                    Dim pIds = String.Join(",", latestAudits.Select(Function(a) a.ProductId).Distinct())
                    Dim productMap As New Dictionary(Of Integer, Product)()
                    Using pCmd = conn.CreateCommand()
                        pCmd.CommandText = "SELECT Id, Name, Sku, CategoryId, Description, RetailPrice, Unit, HasExpiry, " &
                                           "MinimumThreshold, IsActive, IsDeleted, DeletedBy, DeletedAt, " &
                                           "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                           $"FROM Inv_Products WHERE Id IN ({pIds})"
                        Using pReader = pCmd.ExecuteReader()
                            While pReader.Read()
                                Dim p = StockService.ReadProduct(pReader)
                                productMap(p.Id) = p
                            End While
                        End Using
                    End Using
                    For Each rec In latestAudits
                        Dim prod As Product = Nothing
                        If productMap.TryGetValue(rec.ProductId, prod) Then rec.Product = prod
                    Next
                End If
            End Using
            Return latestAudits
        End Function

    End Class

End Namespace
