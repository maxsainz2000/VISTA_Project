' Rule 3 known-bad: Bare DbSet.ToListAsync() — full entity materialisation
' No projection, no filtering — classic shape that triggers the EF10 VB.NET bug.

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetAllVendorsAsync() As Task(Of List(Of Vendor))
            Return Await _db.Vendors.ToListAsync()
        End Function
    End Class
End Namespace
