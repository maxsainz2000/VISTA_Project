Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Class CreateVendorDto
        Public Property Name As String
        Public Property ContactPerson As String
        Public Property Phone As String
        Public Property Email As String
        Public Property Address As String
        Public Property DefaultLeadTimeDays As Integer
        Public Property Notes As String
    End Class

    Public Class UpdateVendorDto
        Public Property Name As String
        Public Property ContactPerson As String
        Public Property Phone As String
        Public Property Email As String
        Public Property Address As String
        Public Property DefaultLeadTimeDays As Integer
        Public Property Notes As String
    End Class

    Public Class VendorDetailDto
        Public Property Vendor As Vendor
        Public Property TotalPurchaseOrders As Integer
        Public Property TotalSpent As Decimal
        Public Property LastOrderDate As DateTime?
        Public Property AverageLeadTimeDays As Double
    End Class

    Public Interface IVendorService
        Function CreateAsync(dto As CreateVendorDto) As Task(Of Vendor)
        Function GetByIdAsync(id As Integer) As Task(Of Vendor)
        Function GetAllAsync() As Task(Of List(Of Vendor))
        Function UpdateAsync(id As Integer, dto As UpdateVendorDto) As Task(Of Vendor)
        Function DeleteAsync(id As Integer) As Task(Of Boolean)
        Function RestoreAsync(id As Integer) As Task(Of Boolean)
        Function SearchAsync(searchTerm As String) As Task(Of List(Of Vendor))
        Function GetVendorWithPurchaseHistoryAsync(id As Integer) As Task(Of VendorDetailDto)
    End Interface

End Namespace
