' Rule 3 known-bad: .AsNoTracking().IgnoreQueryFilters().ToListAsync()
' Source: VendorService.vb:58 (verified true positive — same shape as the Vendor bug)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetAllVendorsReadOnlyAsync() As Task(Of List(Of Vendor))
            Return Await _db.Vendors.AsNoTracking().IgnoreQueryFilters().ToListAsync()
        End Function
    End Class
End Namespace
