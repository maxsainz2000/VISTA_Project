Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class VendorProductService
        Implements IVendorProductService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of PurchasingDbContext)
        Private ReadOnly _session As ISessionService

        Public Sub New(db As PurchasingDbContext,
                       repository As ISyncableRepository(Of PurchasingDbContext),
                       session As ISessionService)
            _db = db
            _repository = repository
            _session = session
        End Sub

        Public Async Function GetCatalogForVendorAsync(vendorId As Integer) As Task(Of IReadOnlyList(Of VendorProductDto)) Implements IVendorProductService.GetCatalogForVendorAsync
            Dim list As New List(Of VendorProductDto)()
            Dim connStr = _db.Database.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT Id, VendorId, ProductId, ProductName, LastUnitCost, Notes " &
                                      "FROM Pur_VendorProducts WHERE VendorId = @vendorId AND IsDeleted = 0 ORDER BY ProductName"
                    cmd.Parameters.AddWithValue("@vendorId", vendorId)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim costStr = reader.GetString(4)
                            Dim costVal As Decimal = 0D
                            Decimal.TryParse(costStr, costVal)

                            list.Add(New VendorProductDto With {
                                .Id = reader.GetInt32(0),
                                .VendorId = reader.GetInt32(1),
                                .ProductId = reader.GetInt32(2),
                                .ProductName = reader.GetString(3),
                                .LastUnitCost = costVal,
                                .Notes = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function AddCatalogEntryAsync(vendorId As Integer, productId As Integer, productName As String, unitCost As Decimal, notes As String) As Task(Of VendorProduct) Implements IVendorProductService.AddCatalogEntryAsync
            If _session.CurrentRole <> UserRole.Manager Then
                Throw New UnauthorizedAccessException("Only Managers are authorized to perform this operation.")
            End If

            Dim exists As Boolean = Await _db.VendorProducts.
                AnyAsync(Function(vp) Not vp.IsDeleted AndAlso vp.VendorId = vendorId AndAlso vp.ProductId = productId)

            If exists Then
                Throw New InvalidOperationException("This product is already in the vendor's catalog.")
            End If

            Dim entry As New VendorProduct With {
                .VendorId = vendorId,
                .ProductId = productId,
                .ProductName = productName,
                .LastUnitCost = unitCost,
                .Notes = notes,
                .CreatedBy = _session.CurrentUsername,
                .CreatedAt = DateTime.UtcNow
            }

            _db.VendorProducts.Add(entry)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None)

            Return entry
        End Function

        Public Async Function UpdateCatalogEntryAsync(id As Integer, unitCost As Decimal, notes As String) As Task Implements IVendorProductService.UpdateCatalogEntryAsync
            If _session.CurrentRole <> UserRole.Manager Then
                Throw New UnauthorizedAccessException("Only Managers are authorized to perform this operation.")
            End If

            Dim entry = Await _db.VendorProducts.
                FirstOrDefaultAsync(Function(vp) vp.Id = id AndAlso Not vp.IsDeleted)

            If entry Is Nothing Then
                Throw New InvalidOperationException("Catalog entry not found.")
            End If

            entry.LastUnitCost = unitCost
            entry.Notes = notes
            entry.ModifiedBy = _session.CurrentUsername
            entry.ModifiedAt = DateTime.UtcNow

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None)
        End Function

        Public Async Function RemoveCatalogEntryAsync(id As Integer) As Task Implements IVendorProductService.RemoveCatalogEntryAsync
            If _session.CurrentRole <> UserRole.Manager Then
                Throw New UnauthorizedAccessException("Only Managers are authorized to perform this operation.")
            End If

            Dim entry = Await _db.VendorProducts.
                FirstOrDefaultAsync(Function(vp) vp.Id = id AndAlso Not vp.IsDeleted)

            If entry Is Nothing Then
                Throw New InvalidOperationException("Catalog entry not found.")
            End If

            entry.IsDeleted = True
            entry.DeletedBy = _session.CurrentUsername
            entry.DeletedAt = DateTime.UtcNow

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None)
        End Function

        Public Async Function UpdateLastUnitCostAsync(vendorId As Integer, productId As Integer, newCost As Decimal) As Task Implements IVendorProductService.UpdateLastUnitCostAsync
            If _session.CurrentRole <> UserRole.Manager Then
                Throw New UnauthorizedAccessException("Only Managers are authorized to perform this operation.")
            End If

            Dim entry = Await _db.VendorProducts.
                FirstOrDefaultAsync(Function(vp) vp.VendorId = vendorId AndAlso vp.ProductId = productId AndAlso Not vp.IsDeleted)

            If entry IsNot Nothing Then
                entry.LastUnitCost = newCost
                entry.ModifiedBy = _session.CurrentUsername
                entry.ModifiedAt = DateTime.UtcNow
                Await _repository.SaveChangesWithJournalAsync(CancellationToken.None)
            End If
        End Function

    End Class

End Namespace
