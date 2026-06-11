Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.Purchasing.Entities

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the Purchasing module.
    ''' All tables in this context use the <c>Pur_</c> prefix to prevent naming collisions
    ''' with other modules in the shared centralized MariaDB database.
    ''' </summary>
    Public Class PurchasingDbContext
        Inherits BaseDbContext

        Public Property Vendors As DbSet(Of Vendor)
        Public Property PurchaseOrders As DbSet(Of PurchaseOrder)
        Public Property PurchaseOrderLines As DbSet(Of PurchaseOrderLine)
        Public Property GoodsReceipts As DbSet(Of GoodsReceipt)
        Public Property GoodsReceiptLines As DbSet(Of GoodsReceiptLine)
        Public Property AccountsPayableEntries As DbSet(Of AccountsPayableEntry)
        Public Property ReorderConfigs As DbSet(Of ReorderConfig)
        Public Property ReorderSuggestions As DbSet(Of ReorderSuggestion)
        Public Property PriceChangeAlerts As DbSet(Of PriceChangeAlert)
        Public Property VendorProducts As DbSet(Of VendorProduct)
        Public Property OrderSequences As DbSet(Of OrderSequence)

        Public Sub New(options As DbContextOptions(Of PurchasingDbContext), session As MerchSys.SharedKernel.Interfaces.ISessionService)
            MyBase.New(options, session)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(PurchasingDbContext).Assembly)
            SeedData.PurchasingSeedData.Seed(modelBuilder)
        End Sub

    End Class

End Namespace
