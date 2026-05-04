Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.Purchasing.Entities

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the Purchasing module.
    ''' All tables in this context use the <c>Pur_</c> prefix to prevent naming collisions
    ''' with other modules in the shared <c>merchsys.db</c> SQLite file.
    ''' </summary>
    Public Class PurchasingDbContext
        Inherits BaseDbContext

        Public Property Vendors As DbSet(Of Vendor)
        Public Property PurchaseOrders As DbSet(Of PurchaseOrder)
        Public Property PurchaseOrderLines As DbSet(Of PurchaseOrderLine)
        Public Property GoodsReceipts As DbSet(Of GoodsReceipt)
        Public Property GoodsReceiptLines As DbSet(Of GoodsReceiptLine)
        Public Property AccountsPayableEntries As DbSet(Of AccountsPayableEntry)

        Public Sub New(options As DbContextOptions(Of PurchasingDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(PurchasingDbContext).Assembly)
            SeedData.PurchasingSeedData.Seed(modelBuilder)
        End Sub

    End Class

End Namespace
