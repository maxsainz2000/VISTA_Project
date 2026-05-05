Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Class VendorService
        Implements IVendorService

        Private ReadOnly _db As PurchasingDbContext

        Public Sub New(db As PurchasingDbContext)
            _db = db
        End Sub

        Public Async Function CreateAsync(dto As CreateVendorDto) As Task(Of Vendor) Implements IVendorService.CreateAsync
            ValidateDto(dto.Name, dto.Phone, dto.DefaultLeadTimeDays)

            Dim nameExists As Boolean = Await _db.Vendors.
                AnyAsync(Function(v) Not v.IsDeleted AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            If nameExists Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists.")
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
            Return Await _db.Vendors.
                Where(Function(v) Not v.IsDeleted).
                OrderBy(Function(v) v.Name).
                ToListAsync()
        End Function

        Public Async Function UpdateAsync(id As Integer, dto As UpdateVendorDto) As Task(Of Vendor) Implements IVendorService.UpdateAsync
            ValidateDto(dto.Name, dto.Phone, dto.DefaultLeadTimeDays)

            Dim vendor As Vendor = Await _db.Vendors.
                FirstOrDefaultAsync(Function(v) v.Id = id AndAlso Not v.IsDeleted)

            If vendor Is Nothing Then
                Throw New InvalidOperationException($"Vendor {id} not found.")
            End If

            Dim nameConflict As Boolean = Await _db.Vendors.
                AnyAsync(Function(v) Not v.IsDeleted AndAlso
                         v.Id <> id AndAlso
                         v.Name.ToLower() = dto.Name.ToLower())

            If nameConflict Then
                Throw New InvalidOperationException($"A vendor named '{dto.Name}' already exists.")
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

            Dim term As String = searchTerm.ToLower()

            Return Await _db.Vendors.
                Where(Function(v) Not v.IsDeleted AndAlso (
                    v.Name.ToLower().Contains(term) OrElse
                    v.ContactPerson.ToLower().Contains(term) OrElse
                    v.Phone.ToLower().Contains(term)
                )).
                OrderBy(Function(v) v.Name).
                ToListAsync()
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

            Dim receipts = Await _db.GoodsReceipts.
                Where(Function(gr) orders.Select(Function(po) po.Id).Contains(gr.PurchaseOrderId)).
                ToListAsync()

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
