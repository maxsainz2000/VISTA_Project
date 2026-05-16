Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Helpers
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class PurchaseOrderService
        Implements IPurchaseOrderService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of PurchasingDbContext)

        Public Sub New(db As PurchasingDbContext,
                       repository As ISyncableRepository(Of PurchasingDbContext))
            _db = db
            _repository = repository
        End Sub

        Public Async Function CreateDraftAsync(vendorId As Integer,
                                               lines As List(Of CreatePOLineDto),
                                               Optional notes As String = Nothing,
                                               Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.CreateDraftAsync
            Dim year As Integer = DateTime.UtcNow.Year
            Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
                Select(Function(p) p.OrderNumber).
                ToListAsync()
            Dim orderNumber As String = SequentialNumberGenerator.Generate("PO", year, existingNumbers)

            Dim po As New PurchaseOrder With {
                .OrderNumber = orderNumber,
                .VendorId = vendorId,
                .Status = PurchaseOrderStatus.Draft,
                .OrderDate = DateTime.UtcNow,
                .TotalAmount = 0D,
                .Notes = notes,
                .ExpectedDeliveryDate = expectedDeliveryDate
            }

            For Each dto In lines
                po.Lines.Add(New PurchaseOrderLine With {
                    .ProductId = dto.ProductId,
                    .ProductName = dto.ProductName,
                    .QuantityOrdered = dto.QuantityOrdered,
                    .UnitCost = dto.UnitCost,
                    .LineTotal = dto.QuantityOrdered * dto.UnitCost
                })
            Next

            RecalculateTotal(po)
            _db.PurchaseOrders.Add(po)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(po.Id)
        End Function

        Public Async Function GetByIdAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.GetByIdAsync
            Return Await _db.PurchaseOrders.
                Include(Function(po) po.Lines).
                Include(Function(po) po.Vendor).
                FirstOrDefaultAsync(Function(po) po.Id = id)
        End Function

        Public Async Function GetAllAsync(Optional status As PurchaseOrderStatus? = Nothing) As Task(Of List(Of PurchaseOrder)) Implements IPurchaseOrderService.GetAllAsync
            Dim query = _db.PurchaseOrders.
                Include(Function(po) po.Lines).
                Include(Function(po) po.Vendor).
                AsQueryable()

            If status.HasValue Then
                query = query.Where(Function(po) po.Status = status.Value)
            End If

            Return Await query.OrderByDescending(Function(po) po.OrderDate).ToListAsync()
        End Function

        Public Async Function UpdateDraftAsync(id As Integer,
                                               lines As List(Of CreatePOLineDto),
                                               Optional notes As String = Nothing,
                                               Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.UpdateDraftAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {id} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Draft Then
                Throw New InvalidOperationException($"Only Draft purchase orders can be updated. Current status: {po.Status}.")
            End If

            If notes IsNot Nothing Then po.Notes = notes
            If expectedDeliveryDate.HasValue Then po.ExpectedDeliveryDate = expectedDeliveryDate

            _db.PurchaseOrderLines.RemoveRange(po.Lines)
            po.Lines.Clear()

            For Each dto In lines
                po.Lines.Add(New PurchaseOrderLine With {
                    .ProductId = dto.ProductId,
                    .ProductName = dto.ProductName,
                    .QuantityOrdered = dto.QuantityOrdered,
                    .UnitCost = dto.UnitCost,
                    .LineTotal = dto.QuantityOrdered * dto.UnitCost
                })
            Next

            RecalculateTotal(po)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function SubmitAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.SubmitAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {id} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Draft Then
                Throw New InvalidOperationException($"Only Draft purchase orders can be submitted. Current status: {po.Status}.")
            End If
            If Not po.Lines.Any() Then
                Throw New InvalidOperationException("Cannot submit a purchase order with no lines.")
            End If
            If po.Lines.Any(Function(l) l.QuantityOrdered <= 0) Then
                Throw New InvalidOperationException("All line quantities must be greater than zero.")
            End If
            If po.Lines.Any(Function(l) l.UnitCost <= 0) Then
                Throw New InvalidOperationException("All line unit costs must be greater than zero.")
            End If

            po.Status = PurchaseOrderStatus.Submitted
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function MarkReceivedAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.MarkReceivedAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {id} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Submitted Then
                Throw New InvalidOperationException($"Only Submitted purchase orders can be marked as received. Current status: {po.Status}.")
            End If

            po.Status = PurchaseOrderStatus.Received
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function VerifyAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.VerifyAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {id} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Received Then
                Throw New InvalidOperationException($"Only Received purchase orders can be verified. Current status: {po.Status}.")
            End If

            po.Status = PurchaseOrderStatus.Verified
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function CloseAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.CloseAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {id} not found.")
            End If
            If po.Status <> PurchaseOrderStatus.Verified Then
                Throw New InvalidOperationException($"Only Verified purchase orders can be closed. Current status: {po.Status}.")
            End If

            po.Status = PurchaseOrderStatus.Closed

            _db.AccountsPayableEntries.Add(New AccountsPayableEntry With {
                .PurchaseOrderId = po.Id,
                .VendorId = po.VendorId,
                .InvoiceNumber = po.OrderNumber,
                .InvoiceDate = DateTime.UtcNow,
                .DueDate = DateTime.UtcNow.AddDays(30),
                .TotalAmount = po.TotalAmount,
                .AmountPaid = 0D,
                .Balance = po.TotalAmount,
                .IsPaid = False
            })

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function DeleteDraftAsync(id As Integer) As Task(Of Boolean) Implements IPurchaseOrderService.DeleteDraftAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                Include(Function(p) p.Lines).
                FirstOrDefaultAsync(Function(p) p.Id = id)

            If po Is Nothing Then
                Return False
            End If
            If po.Status <> PurchaseOrderStatus.Draft Then
                Throw New InvalidOperationException($"Only Draft purchase orders can be deleted. Current status: {po.Status}.")
            End If

            _db.PurchaseOrders.Remove(po)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return True
        End Function

        Private Shared Sub RecalculateTotal(po As PurchaseOrder)
            po.TotalAmount = po.Lines.Sum(Function(l) l.LineTotal)
        End Sub

    End Class

End Namespace
