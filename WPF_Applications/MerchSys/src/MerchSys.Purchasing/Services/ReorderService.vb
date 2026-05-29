Imports System.Threading
Imports MediatR
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Helpers
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Queries

Namespace Services

    Public Class ReorderService
        Implements IReorderService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _mediator As IMediator
        Private _reorderSuggestionList As List(Of ReorderSuggestion)
        Private _reorderConfigList As List(Of ReorderConfig)

        Public Sub New(db As PurchasingDbContext,
                       mediator As IMediator)
            _db = db
            _mediator = mediator
        End Sub

        Public Async Function GenerateSuggestionsAsync() As Task(Of List(Of ReorderSuggestion)) Implements IReorderService.GenerateSuggestionsAsync
            Dim stockResult = Await _mediator.Send(New GetCurrentStockQuery())
            Dim stockMap = stockResult.Items.ToDictionary(Function(s) s.ProductId)

            _reorderConfigList = New List(Of ReorderConfig)()
            Dim genConnStr = _db.Database.GetConnectionString()
            Using genConn As New MySqlConnection(genConnStr)
                Await genConn.OpenAsync()
                Using genCmd = genConn.CreateCommand()
                    genCmd.CommandText = "SELECT Id, ProductId, ProductName, PreferredVendorId, MinimumThreshold, SafetyStock, " &
                                         "DefaultOrderQuantity, LeadTimeDays, IsSeasonalItem, SeasonalMultiplier, IsActive, " &
                                         "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Pur_ReorderConfigs WHERE IsActive = 1"
                    Using genReader = genCmd.ExecuteReader()
                        While genReader.Read()
                            _reorderConfigList.Add(New ReorderConfig With {
                                .Id = genReader.GetInt32(0),
                                .ProductId = genReader.GetInt32(1),
                                .ProductName = genReader.GetString(2),
                                .PreferredVendorId = If(genReader.IsDBNull(3), CType(Nothing, Integer?), genReader.GetInt32(3)),
                                .MinimumThreshold = genReader.GetInt32(4),
                                .SafetyStock = genReader.GetInt32(5),
                                .DefaultOrderQuantity = genReader.GetInt32(6),
                                .LeadTimeDays = genReader.GetInt32(7),
                                .IsSeasonalItem = genReader.GetBoolean(8),
                                .SeasonalMultiplier = genReader.GetDecimal(9),
                                .IsActive = genReader.GetBoolean(10)
                            })
                        End While
                    End Using
                End Using

                Dim vendorIdSet = _reorderConfigList.
                    Where(Function(cfg) cfg.PreferredVendorId.HasValue).
                    Select(Function(cfg) cfg.PreferredVendorId.Value).Distinct().ToList()

                If vendorIdSet.Any() Then
                    Dim genVendorDict As New Dictionary(Of Integer, Vendor)()
                    Dim genVidList As String = String.Join(",", vendorIdSet)
                    Using genVCmd = genConn.CreateCommand()
                        genVCmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                              $"FROM Pur_Vendors WHERE Id IN ({genVidList})"
                        Using genVReader = genVCmd.ExecuteReader()
                            While genVReader.Read()
                                Dim gv As New Vendor With {
                                    .Id = genVReader.GetInt32(0),
                                    .Name = genVReader.GetString(1),
                                    .ContactPerson = genVReader.GetString(2),
                                    .Phone = genVReader.GetString(3),
                                    .Email = If(genVReader.IsDBNull(4), Nothing, genVReader.GetString(4)),
                                    .Address = genVReader.GetString(5),
                                    .DefaultLeadTimeDays = genVReader.GetInt32(6),
                                    .Notes = If(genVReader.IsDBNull(7), Nothing, genVReader.GetString(7))
                                }
                                genVendorDict(gv.Id) = gv
                            End While
                        End Using
                    End Using
                    For Each cfg In _reorderConfigList
                        Dim gv As Vendor = Nothing
                        If cfg.PreferredVendorId.HasValue AndAlso genVendorDict.TryGetValue(cfg.PreferredVendorId.Value, gv) Then
                            cfg.PreferredVendor = gv
                        End If
                    Next
                End If
            End Using
            Dim configs = _reorderConfigList

            ' Collect product IDs that already have a pending suggestion to avoid duplicates.
            Dim pendingProductIds As New HashSet(Of Integer)(
                Await _db.ReorderSuggestions.
                    Where(Function(s) s.Status = "Pending").
                    Select(Function(s) s.ProductId).
                    ToListAsync())

            Dim newSuggestions As New List(Of ReorderSuggestion)()

            For Each config In configs
                If pendingProductIds.Contains(config.ProductId) Then
                    Continue For
                End If

                Dim stockLevel As GetCurrentStockResult.StockLevel = Nothing
                If Not stockMap.TryGetValue(config.ProductId, stockLevel) Then
                    Continue For
                End If

                Dim reorderPoint As Integer = config.MinimumThreshold
                Dim seasonalAdjusted As Boolean = False

                If config.IsSeasonalItem AndAlso config.SeasonalMultiplier > 1D Then
                    reorderPoint = CInt(Math.Ceiling(reorderPoint * config.SeasonalMultiplier))
                    seasonalAdjusted = True
                End If

                If stockLevel.CurrentQuantity <= reorderPoint Then
                    Dim suggestion As New ReorderSuggestion With {
                        .ProductId = config.ProductId,
                        .ProductName = config.ProductName,
                        .CurrentStock = stockLevel.CurrentQuantity,
                        .ReorderPoint = reorderPoint,
                        .SuggestedQuantity = config.DefaultOrderQuantity,
                        .PreferredVendorId = config.PreferredVendorId,
                        .PreferredVendorName = If(config.PreferredVendor IsNot Nothing, config.PreferredVendor.Name, Nothing),
                        .EstimatedLeadTimeDays = config.LeadTimeDays,
                        .IsSeasonalAdjusted = seasonalAdjusted,
                        .Status = "Pending"
                    }
                    newSuggestions.Add(suggestion)
                End If
            Next

            If newSuggestions.Any() Then
                _db.ReorderSuggestions.AddRange(newSuggestions)
                Await _db.SaveChangesAsync()
            End If

            Return newSuggestions
        End Function

        Public Async Function GetPendingSuggestionsAsync() As Task(Of List(Of ReorderSuggestion)) Implements IReorderService.GetPendingSuggestionsAsync
            _reorderSuggestionList = New List(Of ReorderSuggestion)()
            Dim psConnStr = _db.Database.GetConnectionString()
            Using psConn As New MySqlConnection(psConnStr)
                Await psConn.OpenAsync()
                Using psCmd = psConn.CreateCommand()
                    psCmd.CommandText = "SELECT Id, ProductId, ProductName, CurrentStock, ReorderPoint, SuggestedQuantity, " &
                                        "PreferredVendorId, PreferredVendorName, EstimatedLeadTimeDays, IsSeasonalAdjusted, " &
                                        "Status, ConvertedToPOId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_ReorderSuggestions WHERE Status = 'Pending' ORDER BY CreatedAt DESC"
                    Using psReader = psCmd.ExecuteReader()
                        While psReader.Read()
                            _reorderSuggestionList.Add(ReadReorderSuggestion(psReader))
                        End While
                    End Using
                End Using
            End Using
            Return _reorderSuggestionList
        End Function

        Public Async Function AcceptSuggestionAsync(id As Integer) As Task(Of PurchaseOrder) Implements IReorderService.AcceptSuggestionAsync
            Dim suggestion = Await _db.ReorderSuggestions.
                FirstOrDefaultAsync(Function(s) s.Id = id)

            If suggestion Is Nothing Then
                Throw New InvalidOperationException($"Reorder suggestion {id} not found.")
            End If
            If suggestion.Status <> "Pending" Then
                Throw New InvalidOperationException($"Only Pending suggestions can be accepted. Current status: {suggestion.Status}.")
            End If
            If Not suggestion.PreferredVendorId.HasValue Then
                Throw New InvalidOperationException($"Suggestion {id} has no preferred vendor. Assign a vendor in ReorderConfig before accepting.")
            End If

            Dim year As Integer = DateTime.UtcNow.Year
            ' Include soft-deleted rows: the OrderNumber unique index spans every row
            ' (deleted included), so the sequence must not reuse a deleted PO's number.
            Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
                IgnoreQueryFilters().
                Select(Function(p) p.OrderNumber).
                ToListAsync()
            Dim orderNumber As String = SequentialNumberGenerator.Generate("PO", year, existingNumbers)

            Dim po As New PurchaseOrder With {
                .OrderNumber = orderNumber,
                .VendorId = suggestion.PreferredVendorId.Value,
                .Status = PurchaseOrderStatus.Draft,
                .OrderDate = DateTime.UtcNow,
                .Notes = $"Auto-generated from reorder suggestion #{suggestion.Id} for {suggestion.ProductName}.",
                .TotalAmount = 0D
            }

            ' UnitCost is 0 as a draft placeholder; the manager must set actual pricing before submission.
            po.Lines.Add(New PurchaseOrderLine With {
                .ProductId = suggestion.ProductId,
                .ProductName = suggestion.ProductName,
                .QuantityOrdered = suggestion.SuggestedQuantity,
                .UnitCost = 0D,
                .LineTotal = 0D
            })

            _db.PurchaseOrders.Add(po)
            Await _db.SaveChangesAsync()

            suggestion.Status = "Accepted"
            suggestion.ConvertedToPOId = po.Id
            Await _db.SaveChangesAsync()

            Return Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                Include(Function(p) p.Vendor).
                FirstOrDefaultAsync(Function(p) p.Id = po.Id)
        End Function

        Public Async Function DismissSuggestionAsync(id As Integer) As Task Implements IReorderService.DismissSuggestionAsync
            Dim suggestion = Await _db.ReorderSuggestions.
                FirstOrDefaultAsync(Function(s) s.Id = id)

            If suggestion Is Nothing Then
                Throw New InvalidOperationException($"Reorder suggestion {id} not found.")
            End If
            If suggestion.Status <> "Pending" Then
                Throw New InvalidOperationException($"Only Pending suggestions can be dismissed. Current status: {suggestion.Status}.")
            End If

            suggestion.Status = "Dismissed"
            Await _db.SaveChangesAsync()
        End Function

        Public Async Function UpdateConfigAsync(config As ReorderConfig) As Task(Of ReorderConfig) Implements IReorderService.UpdateConfigAsync
            Dim existing = Await _db.ReorderConfigs.
                FirstOrDefaultAsync(Function(c) c.Id = config.Id)

            If existing Is Nothing Then
                _db.ReorderConfigs.Add(config)
            Else
                existing.ProductName = config.ProductName
                existing.PreferredVendorId = config.PreferredVendorId
                existing.MinimumThreshold = config.MinimumThreshold
                existing.SafetyStock = config.SafetyStock
                existing.DefaultOrderQuantity = config.DefaultOrderQuantity
                existing.LeadTimeDays = config.LeadTimeDays
                existing.IsSeasonalItem = config.IsSeasonalItem
                existing.SeasonalMultiplier = config.SeasonalMultiplier
                existing.IsActive = config.IsActive
            End If

            Await _db.SaveChangesAsync()

            Return Await _db.ReorderConfigs.
                Include(Function(c) c.PreferredVendor).
                FirstOrDefaultAsync(Function(c) c.ProductId = config.ProductId)
        End Function

        Public Async Function GetAllConfigsAsync() As Task(Of List(Of ReorderConfig)) Implements IReorderService.GetAllConfigsAsync
            _reorderConfigList = New List(Of ReorderConfig)()
            Dim acConnStr = _db.Database.GetConnectionString()
            Using acConn As New MySqlConnection(acConnStr)
                Await acConn.OpenAsync()
                Using acCmd = acConn.CreateCommand()
                    acCmd.CommandText = "SELECT Id, ProductId, ProductName, PreferredVendorId, MinimumThreshold, SafetyStock, " &
                                        "DefaultOrderQuantity, LeadTimeDays, IsSeasonalItem, SeasonalMultiplier, IsActive, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_ReorderConfigs ORDER BY ProductName ASC"
                    Using acReader = acCmd.ExecuteReader()
                        While acReader.Read()
                            _reorderConfigList.Add(New ReorderConfig With {
                                .Id = acReader.GetInt32(0),
                                .ProductId = acReader.GetInt32(1),
                                .ProductName = acReader.GetString(2),
                                .PreferredVendorId = If(acReader.IsDBNull(3), CType(Nothing, Integer?), acReader.GetInt32(3)),
                                .MinimumThreshold = acReader.GetInt32(4),
                                .SafetyStock = acReader.GetInt32(5),
                                .DefaultOrderQuantity = acReader.GetInt32(6),
                                .LeadTimeDays = acReader.GetInt32(7),
                                .IsSeasonalItem = acReader.GetBoolean(8),
                                .SeasonalMultiplier = acReader.GetDecimal(9),
                                .IsActive = acReader.GetBoolean(10)
                            })
                        End While
                    End Using
                End Using

                Dim acVendorSet = _reorderConfigList.
                    Where(Function(cfg) cfg.PreferredVendorId.HasValue).
                    Select(Function(cfg) cfg.PreferredVendorId.Value).Distinct().ToList()

                If acVendorSet.Any() Then
                    Dim acVendorDict As New Dictionary(Of Integer, Vendor)()
                    Dim acVidList As String = String.Join(",", acVendorSet)
                    Using acVCmd = acConn.CreateCommand()
                        acVCmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                             $"FROM Pur_Vendors WHERE Id IN ({acVidList})"
                        Using acVReader = acVCmd.ExecuteReader()
                            While acVReader.Read()
                                Dim acV As New Vendor With {
                                    .Id = acVReader.GetInt32(0),
                                    .Name = acVReader.GetString(1),
                                    .ContactPerson = acVReader.GetString(2),
                                    .Phone = acVReader.GetString(3),
                                    .Email = If(acVReader.IsDBNull(4), Nothing, acVReader.GetString(4)),
                                    .Address = acVReader.GetString(5),
                                    .DefaultLeadTimeDays = acVReader.GetInt32(6),
                                    .Notes = If(acVReader.IsDBNull(7), Nothing, acVReader.GetString(7))
                                }
                                acVendorDict(acV.Id) = acV
                            End While
                        End Using
                    End Using
                    For Each cfg In _reorderConfigList
                        Dim acV As Vendor = Nothing
                        If cfg.PreferredVendorId.HasValue AndAlso acVendorDict.TryGetValue(cfg.PreferredVendorId.Value, acV) Then
                            cfg.PreferredVendor = acV
                        End If
                    Next
                End If
            End Using
            Return _reorderConfigList
        End Function

        Public Async Function GetAllSuggestionsAsync() As Task(Of List(Of ReorderSuggestion)) Implements IReorderService.GetAllSuggestionsAsync
            _reorderSuggestionList = New List(Of ReorderSuggestion)()
            Dim asConnStr = _db.Database.GetConnectionString()
            Using asConn As New MySqlConnection(asConnStr)
                Await asConn.OpenAsync()
                Using asCmd = asConn.CreateCommand()
                    asCmd.CommandText = "SELECT Id, ProductId, ProductName, CurrentStock, ReorderPoint, SuggestedQuantity, " &
                                        "PreferredVendorId, PreferredVendorName, EstimatedLeadTimeDays, IsSeasonalAdjusted, " &
                                        "Status, ConvertedToPOId, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_ReorderSuggestions ORDER BY CreatedAt DESC"
                    Using asReader = asCmd.ExecuteReader()
                        While asReader.Read()
                            _reorderSuggestionList.Add(ReadReorderSuggestion(asReader))
                        End While
                    End Using
                End Using
            End Using
            Return _reorderSuggestionList
        End Function

        Private Shared Function ReadReorderSuggestion(r As MySqlDataReader) As ReorderSuggestion
            Return New ReorderSuggestion With {
                .Id = r.GetInt32(0),
                .ProductId = r.GetInt32(1),
                .ProductName = r.GetString(2),
                .CurrentStock = r.GetInt32(3),
                .ReorderPoint = r.GetInt32(4),
                .SuggestedQuantity = r.GetInt32(5),
                .PreferredVendorId = If(r.IsDBNull(6), CType(Nothing, Integer?), r.GetInt32(6)),
                .PreferredVendorName = If(r.IsDBNull(7), Nothing, r.GetString(7)),
                .EstimatedLeadTimeDays = r.GetInt32(8),
                .IsSeasonalAdjusted = r.GetBoolean(9),
                .Status = r.GetString(10),
                .ConvertedToPOId = If(r.IsDBNull(11), CType(Nothing, Integer?), r.GetInt32(11)),
                .CreatedBy = If(r.IsDBNull(12), Nothing, r.GetString(12)),
                .CreatedAt = r.GetDateTime(13),
                .ModifiedBy = If(r.IsDBNull(14), Nothing, r.GetString(14)),
                .ModifiedAt = If(r.IsDBNull(15), Nothing, CType(r.GetDateTime(15), DateTime?))
            }
        End Function

    End Class

End Namespace
