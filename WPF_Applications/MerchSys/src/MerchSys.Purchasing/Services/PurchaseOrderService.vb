Imports System.Threading
Imports MySqlConnector
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
        Private _poList As List(Of PurchaseOrder)
        Private _poLineList As List(Of PurchaseOrderLine)
        Private _poVendorList As List(Of Vendor)

        Public Sub New(db As PurchasingDbContext)
            _db = db
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
            Await _db.SaveChangesAsync()

            Return Await GetByIdAsync(po.Id)
        End Function

        Public Async Function GetByIdAsync(id As Integer) As Task(Of PurchaseOrder) Implements IPurchaseOrderService.GetByIdAsync
            Return Await _db.PurchaseOrders.
                Include(Function(po) po.Lines).
                Include(Function(po) po.Vendor).
                FirstOrDefaultAsync(Function(po) po.Id = id)
        End Function

        Public Async Function GetAllAsync(Optional status As PurchaseOrderStatus? = Nothing) As Task(Of List(Of PurchaseOrder)) Implements IPurchaseOrderService.GetAllAsync
            ' Step 1: load PurchaseOrders
            _poList = New List(Of PurchaseOrder)()
            Dim connStr = _db.Database.GetConnectionString()
            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    If status.HasValue Then
                        cmd.CommandText = "SELECT Id, OrderNumber, VendorId, Status, OrderDate, ExpectedDeliveryDate, TotalAmount, Notes " &
                                          "FROM Pur_PurchaseOrders WHERE IsDeleted = 0 AND Status = @status ORDER BY OrderDate DESC"
                        cmd.Parameters.Add(New MySqlParameter("@status", CInt(status.Value)))
                    Else
                        cmd.CommandText = "SELECT Id, OrderNumber, VendorId, Status, OrderDate, ExpectedDeliveryDate, TotalAmount, Notes " &
                                          "FROM Pur_PurchaseOrders WHERE IsDeleted = 0 ORDER BY OrderDate DESC"
                    End If
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            _poList.Add(New PurchaseOrder With {
                                .Id = reader.GetInt32(0),
                                .OrderNumber = reader.GetString(1),
                                .VendorId = reader.GetInt32(2),
                                .Status = CType(reader.GetInt32(3), PurchaseOrderStatus),
                                .OrderDate = reader.GetDateTime(4),
                                .ExpectedDeliveryDate = If(reader.IsDBNull(5), CType(Nothing, DateTime?), CType(reader.GetDateTime(5), DateTime?)),
                                .TotalAmount = reader.GetDecimal(6),
                                .Notes = If(reader.IsDBNull(7), Nothing, reader.GetString(7))
                            })
                        End While
                    End Using
                End Using

                If Not _poList.Any() Then Return _poList

                ' Step 2: load PurchaseOrderLines
                _poLineList = New List(Of PurchaseOrderLine)()
                Dim poIds As String = String.Join(",", _poList.Select(Function(po) po.Id))
                Using lnCmd = conn.CreateCommand()
                    lnCmd.CommandText = "SELECT Id, PurchaseOrderId, ProductId, ProductName, QuantityOrdered, UnitCost, LineTotal " &
                                        $"FROM Pur_PurchaseOrderLines WHERE PurchaseOrderId IN ({poIds})"
                    Using lnReader = lnCmd.ExecuteReader()
                        While lnReader.Read()
                            _poLineList.Add(New PurchaseOrderLine With {
                                .Id = lnReader.GetInt32(0),
                                .PurchaseOrderId = lnReader.GetInt32(1),
                                .ProductId = lnReader.GetInt32(2),
                                .ProductName = lnReader.GetString(3),
                                .QuantityOrdered = lnReader.GetInt32(4),
                                .UnitCost = lnReader.GetDecimal(5),
                                .LineTotal = lnReader.GetDecimal(6)
                            })
                        End While
                    End Using
                End Using

                ' Step 3: load Vendors
                _poVendorList = New List(Of Vendor)()
                Dim vendorIds As String = String.Join(",", _poList.Select(Function(po) po.VendorId).Distinct())
                Using vnCmd = conn.CreateCommand()
                    vnCmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                        $"FROM Pur_Vendors WHERE Id IN ({vendorIds})"
                    Using vnReader = vnCmd.ExecuteReader()
                        While vnReader.Read()
                            _poVendorList.Add(New Vendor With {
                                .Id = vnReader.GetInt32(0),
                                .Name = vnReader.GetString(1),
                                .ContactPerson = vnReader.GetString(2),
                                .Phone = vnReader.GetString(3),
                                .Email = If(vnReader.IsDBNull(4), Nothing, vnReader.GetString(4)),
                                .Address = vnReader.GetString(5),
                                .DefaultLeadTimeDays = vnReader.GetInt32(6),
                                .Notes = If(vnReader.IsDBNull(7), Nothing, vnReader.GetString(7))
                            })
                        End While
                    End Using
                End Using
            End Using

            ' Step 4: reassemble
            Dim vendorDict = _poVendorList.ToDictionary(Function(v) v.Id)
            Dim linesByPo As New Dictionary(Of Integer, List(Of PurchaseOrderLine))()
            For Each ln In _poLineList
                If Not linesByPo.ContainsKey(ln.PurchaseOrderId) Then linesByPo(ln.PurchaseOrderId) = New List(Of PurchaseOrderLine)()
                linesByPo(ln.PurchaseOrderId).Add(ln)
            Next
            For Each po In _poList
                Dim foundVendor As Vendor = Nothing
                If vendorDict.TryGetValue(po.VendorId, foundVendor) Then po.Vendor = foundVendor
                Dim poLines As List(Of PurchaseOrderLine) = Nothing
                If linesByPo.TryGetValue(po.Id, poLines) Then
                    For Each ln In poLines
                        po.Lines.Add(ln)
                    Next
                End If
            Next
            Return _poList
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
            Await _db.SaveChangesAsync()

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
            Await _db.SaveChangesAsync()

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
            Await _db.SaveChangesAsync()

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
            Await _db.SaveChangesAsync()

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

            Await _db.SaveChangesAsync()

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
            Await _db.SaveChangesAsync()

            Return True
        End Function

        Private Shared Sub RecalculateTotal(po As PurchaseOrder)
            po.TotalAmount = po.Lines.Sum(Function(l) l.LineTotal)
        End Sub

    End Class

End Namespace
