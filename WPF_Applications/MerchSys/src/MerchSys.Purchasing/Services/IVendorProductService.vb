Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Dtos

Namespace Services

    Public Interface IVendorProductService
        Function GetCatalogForVendorAsync(vendorId As Integer) As Task(Of IReadOnlyList(Of VendorProductDto))
        Function AddCatalogEntryAsync(vendorId As Integer, productId As Integer, productName As String, unitCost As Decimal, notes As String) As Task(Of VendorProduct)
        Function UpdateCatalogEntryAsync(id As Integer, unitCost As Decimal, notes As String) As Task
        Function RemoveCatalogEntryAsync(id As Integer) As Task
        Function UpdateLastUnitCostAsync(vendorId As Integer, productId As Integer, newCost As Decimal) As Task
    End Interface

End Namespace
