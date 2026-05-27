Imports System.Threading
Imports MediatR
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Inventory.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Handles GetProductsForCatalogQuery sent from cross-module catalog editors.
    ''' Queries active products directly from SQLite database to avoid EF Core VB.NET discovery bugs.
    ''' </summary>
    Public Class GetProductsForCatalogQueryHandler
        Implements IRequestHandler(Of GetProductsForCatalogQuery, IReadOnlyList(Of ProductLookupDto))

        Private ReadOnly _db As InventoryDbContext

        Public Sub New(db As InventoryDbContext)
            _db = db
        End Sub

        Public Async Function Handle(request As GetProductsForCatalogQuery, cancellationToken As CancellationToken) As Task(Of IReadOnlyList(Of ProductLookupDto)) Implements IRequestHandler(Of GetProductsForCatalogQuery, IReadOnlyList(Of ProductLookupDto)).Handle
            Dim products As New List(Of ProductLookupDto)()
            Dim connStr = _db.Database.GetConnectionString()

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync(cancellationToken)

                Dim sql = "SELECT Id, Name, Sku FROM Inv_Products WHERE IsDeleted = 0 AND IsActive = 1"
                If Not String.IsNullOrWhiteSpace(request.SearchTerm) Then
                    sql &= " AND (lower(Name) LIKE @term OR lower(Sku) LIKE @term)"
                End If
                sql &= " ORDER BY Name"

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = sql
                    If Not String.IsNullOrWhiteSpace(request.SearchTerm) Then
                        cmd.Parameters.Add(New SqliteParameter("@term", "%" & request.SearchTerm.Trim().ToLower() & "%"))
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync(cancellationToken)
                        While Await reader.ReadAsync(cancellationToken)
                            products.Add(New ProductLookupDto With {
                                .Id = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Sku = reader.GetString(2)
                            })
                        End While
                    End Using
                End Using
            End Using

            Return products
        End Function

    End Class

End Namespace
