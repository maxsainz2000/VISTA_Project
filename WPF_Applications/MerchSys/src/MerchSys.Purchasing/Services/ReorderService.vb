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

        ''' <summary>
        ''' Generates reorder suggestions. A product qualifies only when it is at/below its
        ''' reorder point AND has at least one active Vendor Product Catalog association — a
        ''' suggestion must resolve to a real supplying vendor because it converts into a PO,
        ''' and a PO line can only reference a product in that vendor's catalog. The preferred
        ''' vendor is the catalog entry with the lowest LastUnitCost (tie-break: lowest VendorId).
        ''' An active Pur_ReorderConfigs row, when present, refines the threshold / order qty /
        ''' lead time / seasonal multiplier; otherwise sensible defaults are derived from the
        ''' product threshold and the chosen vendor's default lead time.
        ''' </summary>
        Public Async Function GenerateSuggestionsAsync() As Task(Of List(Of ReorderSuggestion)) Implements IReorderService.GenerateSuggestionsAsync
            ' Current stock levels already carry MinimumThreshold + IsBelowThreshold (from Inv_Products).
            Dim stockResult = Await _mediator.Send(New GetCurrentStockQuery())

            Dim catalogByProduct As New Dictionary(Of Integer, List(Of CatalogVendorOption))()
            Dim configByProduct As New Dictionary(Of Integer, ReorderConfig)()
            Dim vendorLeadById As New Dictionary(Of Integer, Integer)()
            Dim existingByProduct As New Dictionary(Of Integer, ExistingSuggestionInfo)()
            Dim openPoIds As New HashSet(Of Integer)()

            Dim genConnStr = _db.Database.GetConnectionString()
            Using genConn As New MySqlConnection(genConnStr)
                Await genConn.OpenAsync()

                ' 1. Active vendor-catalog associations -> ProductId => [(VendorId, VendorName, LastUnitCost)].
                Using catCmd = genConn.CreateCommand()
                    catCmd.CommandText = "SELECT vp.ProductId, vp.VendorId, v.Name, vp.LastUnitCost " &
                                         "FROM Pur_VendorProducts vp " &
                                         "INNER JOIN Pur_Vendors v ON v.Id = vp.VendorId " &
                                         "WHERE vp.IsDeleted = 0"
                    Using catReader = catCmd.ExecuteReader()
                        While catReader.Read()
                            Dim pid As Integer = catReader.GetInt32(0)
                            Dim optList As List(Of CatalogVendorOption) = Nothing
                            If Not catalogByProduct.TryGetValue(pid, optList) Then
                                optList = New List(Of CatalogVendorOption)()
                                catalogByProduct(pid) = optList
                            End If
                            optList.Add(New CatalogVendorOption With {
                                .VendorId = catReader.GetInt32(1),
                                .VendorName = catReader.GetString(2),
                                .LastUnitCost = catReader.GetDecimal(3)
                            })
                        End While
                    End Using
                End Using

                ' 2. Optional active reorder configs (refinement only) -> ProductId => config.
                Using cfgCmd = genConn.CreateCommand()
                    cfgCmd.CommandText = "SELECT ProductId, MinimumThreshold, SafetyStock, DefaultOrderQuantity, " &
                                         "LeadTimeDays, IsSeasonalItem, SeasonalMultiplier " &
                                         "FROM Pur_ReorderConfigs WHERE IsActive = 1"
                    Using cfgReader = cfgCmd.ExecuteReader()
                        While cfgReader.Read()
                            Dim pid As Integer = cfgReader.GetInt32(0)
                            configByProduct(pid) = New ReorderConfig With {
                                .ProductId = pid,
                                .MinimumThreshold = cfgReader.GetInt32(1),
                                .SafetyStock = cfgReader.GetInt32(2),
                                .DefaultOrderQuantity = cfgReader.GetInt32(3),
                                .LeadTimeDays = cfgReader.GetInt32(4),
                                .IsSeasonalItem = cfgReader.GetBoolean(5),
                                .SeasonalMultiplier = cfgReader.GetDecimal(6)
                            }
                        End While
                    End Using
                End Using

                ' 3. Default lead times for the vendors that appear in the catalog.
                Dim vendorIdSet = catalogByProduct.Values.
                    SelectMany(Function(l) l).
                    Select(Function(o) o.VendorId).Distinct().ToList()
                If vendorIdSet.Any() Then
                    Using vlCmd = genConn.CreateCommand()
                        vlCmd.CommandText = "SELECT Id, DefaultLeadTimeDays FROM Pur_Vendors WHERE Id IN (" &
                                            String.Join(",", vendorIdSet) & ")"
                        Using vlReader = vlCmd.ExecuteReader()
                            While vlReader.Read()
                                vendorLeadById(vlReader.GetInt32(0)) = vlReader.GetInt32(1)
                            End While
                        End Using
                    End Using
                End If

                ' 4. Most-recent existing suggestion per product (drives re-generate suppression).
                '    Ordering ASC + overwrite leaves the highest Id (latest) per product in the map.
                Using exCmd = genConn.CreateCommand()
                    exCmd.CommandText = "SELECT ProductId, Status, CurrentStock, ConvertedToPOId " &
                                        "FROM Pur_ReorderSuggestions ORDER BY Id ASC"
                    Using exReader = exCmd.ExecuteReader()
                        While exReader.Read()
                            existingByProduct(exReader.GetInt32(0)) = New ExistingSuggestionInfo With {
                                .Status = exReader.GetString(1),
                                .CurrentStock = exReader.GetInt32(2),
                                .ConvertedToPOId = If(exReader.IsDBNull(3), CType(Nothing, Integer?), exReader.GetInt32(3))
                            }
                        End While
                    End Using
                End Using

                ' 5. Of the accepted suggestions, which converted POs are still OPEN (Draft=1/Submitted=2)?
                '    An accepted suggestion only suppresses re-suggest while its PO is in flight.
                Dim acceptedPoIds = existingByProduct.Values.
                    Where(Function(e) e.Status = "Accepted" AndAlso e.ConvertedToPOId.HasValue).
                    Select(Function(e) e.ConvertedToPOId.Value).Distinct().ToList()
                If acceptedPoIds.Any() Then
                    Using poCmd = genConn.CreateCommand()
                        poCmd.CommandText = "SELECT Id FROM Pur_PurchaseOrders WHERE Status IN (1, 2) AND Id IN (" &
                                            String.Join(",", acceptedPoIds) & ")"
                        Using poReader = poCmd.ExecuteReader()
                            While poReader.Read()
                                openPoIds.Add(poReader.GetInt32(0))
                            End While
                        End Using
                    End Using
                End If
            End Using

            Dim newSuggestions As New List(Of ReorderSuggestion)()

            For Each level In stockResult.Items
                ' Gate: skip any product with no active vendor-catalog association.
                Dim vendorOptions As List(Of CatalogVendorOption) = Nothing
                If Not catalogByProduct.TryGetValue(level.ProductId, vendorOptions) OrElse vendorOptions.Count = 0 Then
                    Continue For
                End If

                ' Suppress re-suggesting a product already handled in a prior cycle:
                '   Pending   -> still awaiting the manager's decision (don't duplicate).
                '   Accepted  -> a draft PO is already replenishing it; skip while that PO is still open.
                '   Dismissed -> manager declined; only re-arm if stock has dropped further below the
                '                level recorded when it was dismissed.
                Dim existing As ExistingSuggestionInfo = Nothing
                If existingByProduct.TryGetValue(level.ProductId, existing) Then
                    Select Case existing.Status
                        Case "Pending"
                            Continue For
                        Case "Accepted"
                            If existing.ConvertedToPOId.HasValue AndAlso openPoIds.Contains(existing.ConvertedToPOId.Value) Then
                                Continue For
                            End If
                        Case "Dismissed"
                            If level.CurrentQuantity >= existing.CurrentStock Then
                                Continue For
                            End If
                    End Select
                End If

                ' Threshold (a config refines it; otherwise use the product's seeded threshold).
                Dim cfg As ReorderConfig = Nothing
                configByProduct.TryGetValue(level.ProductId, cfg)

                Dim reorderPoint As Integer = If(cfg IsNot Nothing, cfg.MinimumThreshold, level.MinimumThreshold)
                Dim seasonalAdjusted As Boolean = False
                If cfg IsNot Nothing AndAlso cfg.IsSeasonalItem AndAlso cfg.SeasonalMultiplier > 1D Then
                    reorderPoint = CInt(Math.Ceiling(reorderPoint * cfg.SeasonalMultiplier))
                    seasonalAdjusted = True
                End If

                If level.CurrentQuantity > reorderPoint Then
                    Continue For
                End If

                ' Preferred vendor: lowest last-agreed cost, tie-break lowest VendorId.
                Dim chosen = vendorOptions.
                    OrderBy(Function(o) o.LastUnitCost).
                    ThenBy(Function(o) o.VendorId).
                    First()

                Dim suggestedQty As Integer
                If cfg IsNot Nothing AndAlso cfg.DefaultOrderQuantity > 0 Then
                    suggestedQty = cfg.DefaultOrderQuantity
                Else
                    ' Default: top up to the reorder point (at least one unit).
                    suggestedQty = Math.Max(reorderPoint - level.CurrentQuantity, 1)
                End If

                Dim leadDays As Integer
                If cfg IsNot Nothing Then
                    leadDays = cfg.LeadTimeDays
                Else
                    Dim vendorLead As Integer = 0
                    vendorLeadById.TryGetValue(chosen.VendorId, vendorLead)
                    leadDays = vendorLead
                End If

                newSuggestions.Add(New ReorderSuggestion With {
                    .ProductId = level.ProductId,
                    .ProductName = level.ProductName,
                    .CurrentStock = level.CurrentQuantity,
                    .ReorderPoint = reorderPoint,
                    .SuggestedQuantity = suggestedQty,
                    .PreferredVendorId = chosen.VendorId,
                    .PreferredVendorName = chosen.VendorName,
                    .EstimatedLeadTimeDays = leadDays,
                    .IsSeasonalAdjusted = seasonalAdjusted,
                    .Status = "Pending"
                })
            Next

            If newSuggestions.Any() Then
                _db.ReorderSuggestions.AddRange(newSuggestions)
                Await _db.SaveChangesAsync()
            End If

            Return newSuggestions
        End Function

        ''' <summary>A candidate supplying vendor for a product, sourced from Pur_VendorProducts.</summary>
        Private Class CatalogVendorOption
            Public Property VendorId As Integer
            Public Property VendorName As String
            Public Property LastUnitCost As Decimal
        End Class

        ''' <summary>Snapshot of a product's most-recent reorder suggestion, used to suppress duplicate re-generation.</summary>
        Private Class ExistingSuggestionInfo
            Public Property Status As String
            Public Property CurrentStock As Integer
            Public Property ConvertedToPOId As Integer?
        End Class

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
