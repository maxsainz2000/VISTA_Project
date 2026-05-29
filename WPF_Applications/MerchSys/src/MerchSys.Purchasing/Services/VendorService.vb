Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class VendorService
        Implements IVendorService

        Private ReadOnly _db As PurchasingDbContext
        Private _vendorList As List(Of Vendor)
        Private _grListVendorHistory As List(Of GoodsReceipt)

        Public Sub New(db As PurchasingDbContext)
            _db = db
        End Sub

        Public Async Function CreateAsync(dto As CreateVendorDto) As Task(Of Vendor) Implements IVendorService.CreateAsync
            ValidateDto(dto.Name, dto.Phone, dto.DefaultLeadTimeDays)

            Dim nameExistsActive As Boolean = Await _db.Vendors.
                IgnoreQueryFilters().
                AnyAsync(Function(v) Not v.IsDeleted AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            Dim nameExistsDeleted As Boolean = Await _db.Vendors.
                IgnoreQueryFilters().
                AnyAsync(Function(v) v.IsDeleted AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            If nameExistsActive Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists.")
            End If

            If nameExistsDeleted Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists in a deleted state. Please contact your administrator to restore it or choose a different name.")
            End If

            Dim vendor As New Vendor With {
                .Name = dto.Name,
                .ContactPerson = dto.ContactPerson,
                .Phone = dto.Phone,
                .Email = dto.Email,
                .Address = dto.Address,
                .DefaultLeadTimeDays = dto.DefaultLeadTimeDays,
                .Notes = dto.Notes
            }

            _db.Vendors.Add(vendor)
            Await _db.SaveChangesAsync()

            Return Await GetByIdAsync(vendor.Id)
        End Function

        Public Async Function GetByIdAsync(id As Integer) As Task(Of Vendor) Implements IVendorService.GetByIdAsync
            Return Await _db.Vendors.
                Include(Function(v) v.PurchaseOrders).
                FirstOrDefaultAsync(Function(v) v.Id = id AndAlso Not v.IsDeleted)
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of Vendor)) Implements IVendorService.GetAllAsync
            _vendorList = New List(Of Vendor)()
            Dim connStr = _db.Database.GetConnectionString()
            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                      "FROM Pur_Vendors WHERE IsDeleted = 0 ORDER BY Name"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            _vendorList.Add(New Vendor With {
                                .Id = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .ContactPerson = reader.GetString(2),
                                .Phone = reader.GetString(3),
                                .Email = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                                .Address = reader.GetString(5),
                                .DefaultLeadTimeDays = reader.GetInt32(6),
                                .Notes = If(reader.IsDBNull(7), Nothing, reader.GetString(7))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return _vendorList
        End Function

        Public Async Function UpdateAsync(id As Integer, dto As UpdateVendorDto) As Task(Of Vendor) Implements IVendorService.UpdateAsync
            ValidateDto(dto.Name, dto.Phone, dto.DefaultLeadTimeDays)

            Dim vendor As Vendor = Await _db.Vendors.
                FirstOrDefaultAsync(Function(v) v.Id = id AndAlso Not v.IsDeleted)

            If vendor Is Nothing Then
                Throw New InvalidOperationException($"Vendor {id} not found.")
            End If

            Dim nameConflictActive As Boolean = Await _db.Vendors.
                IgnoreQueryFilters().
                AnyAsync(Function(v) Not v.IsDeleted AndAlso
                         v.Id <> id AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            Dim nameConflictDeleted As Boolean = Await _db.Vendors.
                IgnoreQueryFilters().
                AnyAsync(Function(v) v.IsDeleted AndAlso
                         v.Id <> id AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            If nameConflictActive Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists.")
            End If

            If nameConflictDeleted Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists in a deleted state. Please choose a different name.")
            End If

            vendor.Name = dto.Name
            vendor.ContactPerson = dto.ContactPerson
            vendor.Phone = dto.Phone
            vendor.Email = dto.Email
            vendor.Address = dto.Address
            vendor.DefaultLeadTimeDays = dto.DefaultLeadTimeDays
            vendor.Notes = dto.Notes

            Await _db.SaveChangesAsync()

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function DeleteAsync(id As Integer) As Task(Of Boolean) Implements IVendorService.DeleteAsync
            Dim vendor As Vendor = Await _db.Vendors.
                FirstOrDefaultAsync(Function(v) v.Id = id AndAlso Not v.IsDeleted)

            If vendor Is Nothing Then
                Return False
            End If

            vendor.IsDeleted = True
            vendor.DeletedAt = DateTime.UtcNow
            Await _db.SaveChangesAsync()

            Return True
        End Function

        Public Async Function SearchAsync(searchTerm As String) As Task(Of List(Of Vendor)) Implements IVendorService.SearchAsync
            If String.IsNullOrWhiteSpace(searchTerm) Then
                Return Await GetAllAsync()
            End If

            _vendorList = New List(Of Vendor)()
            Dim saConnStr = _db.Database.GetConnectionString()
            Using saConn As New MySqlConnection(saConnStr)
                Await saConn.OpenAsync()
                Using saCmd = saConn.CreateCommand()
                    saCmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes " &
                                        "FROM Pur_Vendors WHERE IsDeleted = 0 AND " &
                                        "(lower(Name) LIKE @term OR lower(ContactPerson) LIKE @term OR lower(Phone) LIKE @term) " &
                                        "ORDER BY Name"
                    saCmd.Parameters.Add(New MySqlParameter("@term", "%" & searchTerm.ToLower() & "%"))
                    Using saReader = saCmd.ExecuteReader()
                        While saReader.Read()
                            _vendorList.Add(New Vendor With {
                                .Id = saReader.GetInt32(0),
                                .Name = saReader.GetString(1),
                                .ContactPerson = saReader.GetString(2),
                                .Phone = saReader.GetString(3),
                                .Email = If(saReader.IsDBNull(4), Nothing, saReader.GetString(4)),
                                .Address = saReader.GetString(5),
                                .DefaultLeadTimeDays = saReader.GetInt32(6),
                                .Notes = If(saReader.IsDBNull(7), Nothing, saReader.GetString(7))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return _vendorList
        End Function

        Public Async Function GetVendorWithPurchaseHistoryAsync(id As Integer) As Task(Of VendorDetailDto) Implements IVendorService.GetVendorWithPurchaseHistoryAsync
            Dim vendor As Vendor = Await _db.Vendors.
                Include(Function(v) v.PurchaseOrders).
                FirstOrDefaultAsync(Function(v) v.Id = id AndAlso Not v.IsDeleted)

            If vendor Is Nothing Then
                Return Nothing
            End If

            Dim orders = vendor.PurchaseOrders.ToList()

            Dim totalSpent As Decimal = orders.Sum(Function(po) po.TotalAmount)
            Dim lastOrderDate As DateTime? = If(orders.Any(), orders.Max(Function(po) po.OrderDate), CType(Nothing, DateTime?))

            _grListVendorHistory = New List(Of GoodsReceipt)()
            If orders.Any() Then
                Dim poIdList As String = String.Join(",", orders.Select(Function(po) po.Id))
                Dim ghConnStr = _db.Database.GetConnectionString()
                Using ghConn As New MySqlConnection(ghConnStr)
                    Await ghConn.OpenAsync()
                    Using ghCmd = ghConn.CreateCommand()
                        ghCmd.CommandText = "SELECT Id, PurchaseOrderId, ReceiptNumber, ReceivedDate, ReceivedBy, Notes " &
                                            "FROM Pur_GoodsReceipts WHERE PurchaseOrderId IN (" & poIdList & ")"
                        Using ghReader = ghCmd.ExecuteReader()
                            While ghReader.Read()
                                _grListVendorHistory.Add(New GoodsReceipt With {
                                    .Id = ghReader.GetInt32(0),
                                    .PurchaseOrderId = ghReader.GetInt32(1),
                                    .ReceiptNumber = ghReader.GetString(2),
                                    .ReceivedDate = ghReader.GetDateTime(3),
                                    .ReceivedBy = If(ghReader.IsDBNull(4), Nothing, ghReader.GetString(4)),
                                    .Notes = If(ghReader.IsDBNull(5), Nothing, ghReader.GetString(5))
                                })
                            End While
                        End Using
                    End Using
                End Using
            End If
            Dim receipts = _grListVendorHistory

            Dim averageLeadTimeDays As Double = 0
            If receipts.Any() Then
                Dim leadTimes = receipts.
                    Join(orders,
                         Function(gr) gr.PurchaseOrderId,
                         Function(po) po.Id,
                         Function(gr, po) (gr.ReceivedDate - po.OrderDate).TotalDays).
                    ToList()
                averageLeadTimeDays = leadTimes.Average()
            End If

            Return New VendorDetailDto With {
                .Vendor = vendor,
                .TotalPurchaseOrders = orders.Count,
                .TotalSpent = totalSpent,
                .LastOrderDate = lastOrderDate,
                .AverageLeadTimeDays = averageLeadTimeDays
            }
        End Function

        Private Shared Sub ValidateDto(name As String, phone As String, defaultLeadTimeDays As Integer)
            If String.IsNullOrWhiteSpace(name) Then
                Throw New ArgumentException("Vendor name is required.")
            End If
            If String.IsNullOrWhiteSpace(phone) Then
                Throw New ArgumentException("Phone is required.")
            End If
            If defaultLeadTimeDays <= 0 Then
                Throw New ArgumentException("DefaultLeadTimeDays must be greater than 0.")
            End If
        End Sub

    End Class

End Namespace
