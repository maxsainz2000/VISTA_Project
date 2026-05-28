Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class AccountsPayableService
        Implements IAccountsPayableService

        Private ReadOnly _db As PurchasingDbContext
        Private _apList As List(Of AccountsPayableEntry)
        Private _grListForAp As List(Of GoodsReceipt)

        Public Sub New(db As PurchasingDbContext)
            _db = db
        End Sub

        Public Async Function CreateFromPurchaseOrderAsync(purchaseOrderId As Integer, invoiceNumber As String, invoiceDate As DateTime, dueDate As DateTime) As Task(Of AccountsPayableEntry) Implements IAccountsPayableService.CreateFromPurchaseOrderAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                FirstOrDefaultAsync(Function(p) p.Id = purchaseOrderId)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {purchaseOrderId} not found.")
            End If

            Dim alreadyExists As Boolean = Await _db.AccountsPayableEntries.
                AnyAsync(Function(ap) ap.PurchaseOrderId = purchaseOrderId)

            If alreadyExists Then
                Throw New InvalidOperationException($"An accounts payable entry already exists for purchase order {purchaseOrderId}.")
            End If

            _grListForAp = New List(Of GoodsReceipt)()
            Dim grConnStr = _db.Database.GetConnectionString()
            Using grConn As New MySqlConnection(grConnStr)
                Await grConn.OpenAsync()
                Using grCmd = grConn.CreateCommand()
                    grCmd.CommandText = "SELECT Id, PurchaseOrderId, ReceiptNumber, ReceivedDate, ReceivedBy, Notes " &
                                        "FROM Pur_GoodsReceipts WHERE PurchaseOrderId = @poId"
                    grCmd.Parameters.Add(New MySqlParameter("@poId", purchaseOrderId))
                    Using grReader = grCmd.ExecuteReader()
                        While grReader.Read()
                            _grListForAp.Add(New GoodsReceipt With {
                                .Id = grReader.GetInt32(0),
                                .PurchaseOrderId = grReader.GetInt32(1),
                                .ReceiptNumber = grReader.GetString(2),
                                .ReceivedDate = grReader.GetDateTime(3),
                                .ReceivedBy = If(grReader.IsDBNull(4), Nothing, grReader.GetString(4)),
                                .Notes = If(grReader.IsDBNull(5), Nothing, grReader.GetString(5))
                            })
                        End While
                    End Using
                End Using

                If _grListForAp.Any() Then
                    Dim grIds As String = String.Join(",", _grListForAp.Select(Function(receipt) receipt.Id))
                    Dim grMap = _grListForAp.ToDictionary(Function(receipt) receipt.Id)
                    Using grlCmd = grConn.CreateCommand()
                        grlCmd.CommandText = "SELECT GoodsReceiptId, QuantityReceived, UnitCost " &
                                             $"FROM Pur_GoodsReceiptLines WHERE GoodsReceiptId IN ({grIds})"
                        Using grlReader = grlCmd.ExecuteReader()
                            While grlReader.Read()
                                Dim lineGrId = grlReader.GetInt32(0)
                                Dim grl As New GoodsReceiptLine With {
                                    .GoodsReceiptId = lineGrId,
                                    .QuantityReceived = grlReader.GetInt32(1),
                                    .UnitCost = grlReader.GetDecimal(2)
                                }
                                Dim parentGr As GoodsReceipt = Nothing
                                If grMap.TryGetValue(lineGrId, parentGr) Then
                                    parentGr.Lines.Add(grl)
                                End If
                            End While
                        End Using
                    End Using
                End If
            End Using
            Dim receipts As List(Of GoodsReceipt) = _grListForAp

            Dim totalAmount As Decimal = receipts.
                SelectMany(Function(gr) gr.Lines).
                Sum(Function(grl) CDec(grl.QuantityReceived) * grl.UnitCost)

            Dim entry As New AccountsPayableEntry With {
                .PurchaseOrderId = purchaseOrderId,
                .VendorId = po.VendorId,
                .InvoiceNumber = invoiceNumber,
                .InvoiceDate = invoiceDate,
                .DueDate = dueDate,
                .TotalAmount = totalAmount,
                .AmountPaid = 0D,
                .Balance = totalAmount,
                .IsPaid = (totalAmount = 0D)
            }

            _db.AccountsPayableEntries.Add(entry)
            Await _db.SaveChangesAsync()

            Return Await GetByIdWithNavigationAsync(entry.Id)
        End Function

        Public Async Function RecordPaymentAsync(apEntryId As Integer, amount As Decimal) As Task(Of AccountsPayableEntry) Implements IAccountsPayableService.RecordPaymentAsync
            If amount <= 0D Then
                Throw New InvalidOperationException("Payment amount must be greater than zero.")
            End If

            Dim entry As AccountsPayableEntry = Await _db.AccountsPayableEntries.
                FirstOrDefaultAsync(Function(ap) ap.Id = apEntryId)

            If entry Is Nothing Then
                Throw New InvalidOperationException($"Accounts payable entry {apEntryId} not found.")
            End If

            If entry.IsPaid Then
                Throw New InvalidOperationException($"Accounts payable entry {apEntryId} is already fully paid.")
            End If

            If amount > entry.Balance Then
                Throw New InvalidOperationException(
                    $"Payment amount {amount:F2} exceeds outstanding balance {entry.Balance:F2}.")
            End If

            entry.AmountPaid += amount
            entry.Balance = entry.TotalAmount - entry.AmountPaid
            entry.IsPaid = (entry.Balance = 0D)

            Await _db.SaveChangesAsync()

            Return Await GetByIdWithNavigationAsync(apEntryId)
        End Function

        Public Async Function GetAllOutstandingAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetAllOutstandingAsync
            _apList = New List(Of AccountsPayableEntry)()
            Dim connStr = _db.Database.GetConnectionString()
            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, PurchaseOrderId, VendorId, InvoiceNumber, InvoiceDate, DueDate, " &
                                      "TotalAmount, AmountPaid, Balance, IsPaid, Notes, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                      "FROM Pur_AccountsPayable WHERE IsPaid = 0 ORDER BY DueDate ASC"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            _apList.Add(ReadApEntry(reader))
                        End While
                    End Using
                End Using
            End Using
            Await PopulateApNavigationsAsync(_apList)
            Return _apList
        End Function

        Public Async Function GetByVendorAsync(vendorId As Integer) As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetByVendorAsync
            _apList = New List(Of AccountsPayableEntry)()
            Dim byVConnStr = _db.Database.GetConnectionString()
            Using byVConn As New MySqlConnection(byVConnStr)
                Await byVConn.OpenAsync()
                Using byVCmd = byVConn.CreateCommand()
                    byVCmd.CommandText = "SELECT Id, PurchaseOrderId, VendorId, InvoiceNumber, InvoiceDate, DueDate, " &
                                         "TotalAmount, AmountPaid, Balance, IsPaid, Notes, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Pur_AccountsPayable WHERE VendorId = @vendorId ORDER BY InvoiceDate DESC"
                    byVCmd.Parameters.Add(New MySqlParameter("@vendorId", vendorId))
                    Using byVReader = byVCmd.ExecuteReader()
                        While byVReader.Read()
                            _apList.Add(ReadApEntry(byVReader))
                        End While
                    End Using
                End Using
            End Using
            Await PopulateApNavigationsAsync(_apList)
            Return _apList
        End Function

        Public Async Function GetOverdueAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetOverdueAsync
            _apList = New List(Of AccountsPayableEntry)()
            Dim odConnStr = _db.Database.GetConnectionString()
            Using odConn As New MySqlConnection(odConnStr)
                Await odConn.OpenAsync()
                Using odCmd = odConn.CreateCommand()
                    odCmd.CommandText = "SELECT Id, PurchaseOrderId, VendorId, InvoiceNumber, InvoiceDate, DueDate, " &
                                        "TotalAmount, AmountPaid, Balance, IsPaid, Notes, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_AccountsPayable WHERE DueDate < @today AND IsPaid = 0 ORDER BY DueDate ASC"
                    odCmd.Parameters.Add(New MySqlParameter("@today", DateTime.UtcNow.Date.ToString("o")))
                    Using odReader = odCmd.ExecuteReader()
                        While odReader.Read()
                            _apList.Add(ReadApEntry(odReader))
                        End While
                    End Using
                End Using
            End Using
            Await PopulateApNavigationsAsync(_apList)
            Return _apList
        End Function

        Public Async Function GetTotalOutstandingAsync() As Task(Of Decimal) Implements IAccountsPayableService.GetTotalOutstandingAsync
            Dim hasOutstanding As Boolean = Await _db.AccountsPayableEntries.
                AnyAsync(Function(ap) Not ap.IsPaid)

            If Not hasOutstanding Then
                Return 0D
            End If

            Return Await _db.AccountsPayableEntries.
                Where(Function(ap) Not ap.IsPaid).
                SumAsync(Function(ap) ap.Balance)
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetAllAsync
            _apList = New List(Of AccountsPayableEntry)()
            Dim gaConnStr = _db.Database.GetConnectionString()
            Using gaConn As New MySqlConnection(gaConnStr)
                Await gaConn.OpenAsync()
                Using gaCmd = gaConn.CreateCommand()
                    gaCmd.CommandText = "SELECT Id, PurchaseOrderId, VendorId, InvoiceNumber, InvoiceDate, DueDate, " &
                                        "TotalAmount, AmountPaid, Balance, IsPaid, Notes, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pur_AccountsPayable ORDER BY InvoiceDate DESC"
                    Using gaReader = gaCmd.ExecuteReader()
                        While gaReader.Read()
                            _apList.Add(ReadApEntry(gaReader))
                        End While
                    End Using
                End Using
            End Using
            Await PopulateApNavigationsAsync(_apList)
            Return _apList
        End Function

        Private Async Function GetByIdWithNavigationAsync(id As Integer) As Task(Of AccountsPayableEntry)
            Return Await _db.AccountsPayableEntries.
                Include(Function(ap) ap.Vendor).
                Include(Function(ap) ap.PurchaseOrder).
                FirstOrDefaultAsync(Function(ap) ap.Id = id)
        End Function

        Private Shared Function ReadApEntry(r As MySqlDataReader) As AccountsPayableEntry
            Return New AccountsPayableEntry With {
                .Id = r.GetInt32(0),
                .PurchaseOrderId = r.GetInt32(1),
                .VendorId = r.GetInt32(2),
                .InvoiceNumber = If(r.IsDBNull(3), Nothing, r.GetString(3)),
                .InvoiceDate = r.GetDateTime(4),
                .DueDate = r.GetDateTime(5),
                .TotalAmount = r.GetDecimal(6),
                .AmountPaid = r.GetDecimal(7),
                .Balance = r.GetDecimal(8),
                .IsPaid = r.GetBoolean(9),
                .Notes = If(r.IsDBNull(10), Nothing, r.GetString(10)),
                .CreatedBy = If(r.IsDBNull(11), Nothing, r.GetString(11)),
                .CreatedAt = r.GetDateTime(12),
                .ModifiedBy = If(r.IsDBNull(13), Nothing, r.GetString(13)),
                .ModifiedAt = If(r.IsDBNull(14), Nothing, CType(r.GetDateTime(14), DateTime?))
            }
        End Function

        Private Async Function PopulateApNavigationsAsync(apEntries As List(Of AccountsPayableEntry)) As Task
            If Not apEntries.Any() Then Return

            Dim vendorIds As String = String.Join(",", apEntries.Select(Function(ap) ap.VendorId).Distinct())
            Dim purchOrderIds As String = String.Join(",", apEntries.Select(Function(ap) ap.PurchaseOrderId).Distinct())

            Dim vendorDict As New Dictionary(Of Integer, Vendor)()
            Dim poDict As New Dictionary(Of Integer, PurchaseOrder)()

            Dim navConnStr = _db.Database.GetConnectionString()
            Using navConn As New MySqlConnection(navConnStr)
                Await navConn.OpenAsync()

                Using vCmd = navConn.CreateCommand()
                    vCmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                       $"FROM Pur_Vendors WHERE Id IN ({vendorIds})"
                    Using vReader = vCmd.ExecuteReader()
                        While vReader.Read()
                            Dim vendorEntry As New Vendor With {
                                .Id = vReader.GetInt32(0),
                                .Name = vReader.GetString(1),
                                .ContactPerson = vReader.GetString(2),
                                .Phone = vReader.GetString(3),
                                .Email = If(vReader.IsDBNull(4), Nothing, vReader.GetString(4)),
                                .Address = vReader.GetString(5),
                                .DefaultLeadTimeDays = vReader.GetInt32(6),
                                .Notes = If(vReader.IsDBNull(7), Nothing, vReader.GetString(7))
                            }
                            vendorDict(vendorEntry.Id) = vendorEntry
                        End While
                    End Using
                End Using

                Using pCmd = navConn.CreateCommand()
                    pCmd.CommandText = "SELECT Id, OrderNumber, VendorId, Status, OrderDate, TotalAmount, Notes " &
                                       $"FROM Pur_PurchaseOrders WHERE Id IN ({purchOrderIds})"
                    Using pReader = pCmd.ExecuteReader()
                        While pReader.Read()
                            Dim poEntry As New PurchaseOrder With {
                                .Id = pReader.GetInt32(0),
                                .OrderNumber = pReader.GetString(1),
                                .VendorId = pReader.GetInt32(2),
                                .Status = CType(pReader.GetInt32(3), PurchaseOrderStatus),
                                .OrderDate = pReader.GetDateTime(4),
                                .TotalAmount = pReader.GetDecimal(5),
                                .Notes = If(pReader.IsDBNull(6), Nothing, pReader.GetString(6))
                            }
                            poDict(poEntry.Id) = poEntry
                        End While
                    End Using
                End Using
            End Using

            For Each apEntry In apEntries
                Dim foundVendor As Vendor = Nothing
                If vendorDict.TryGetValue(apEntry.VendorId, foundVendor) Then apEntry.Vendor = foundVendor
                Dim foundPo As PurchaseOrder = Nothing
                If poDict.TryGetValue(apEntry.PurchaseOrderId, foundPo) Then apEntry.PurchaseOrder = foundPo
            Next
        End Function

    End Class

End Namespace
