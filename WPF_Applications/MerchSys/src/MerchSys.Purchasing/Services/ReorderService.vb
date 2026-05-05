Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Helpers
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Queries

Namespace Services

    Public Class ReorderService
        Implements IReorderService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _mediator As IMediator

        Public Sub New(db As PurchasingDbContext, mediator As IMediator)
            _db = db
            _mediator = mediator
        End Sub

        Public Async Function GenerateSuggestionsAsync() As Task(Of List(Of ReorderSuggestion)) Implements IReorderService.GenerateSuggestionsAsync
            Dim stockResult = Await _mediator.Send(New GetCurrentStockQuery())
            Dim stockMap = stockResult.Items.ToDictionary(Function(s) s.ProductId)

            Dim configs = Await _db.ReorderConfigs.
                Include(Function(c) c.PreferredVendor).
                Where(Function(c) c.IsActive).
                ToListAsync()

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
            Return Await _db.ReorderSuggestions.
                Where(Function(s) s.Status = "Pending").
                OrderByDescending(Function(s) s.CreatedAt).
                ToListAsync()
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
            Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
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
            Return Await _db.ReorderConfigs.
                Include(Function(c) c.PreferredVendor).
                OrderBy(Function(c) c.ProductName).
                ToListAsync()
        End Function

    End Class

End Namespace
