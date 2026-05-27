Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Data.SeedData

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the Inventory module.
    ''' All tables in this context use the <c>Inv_</c> prefix to prevent naming collisions
    ''' with other modules in the shared <c>merchsys.db</c> SQLite file.
    ''' </summary>
    Public Class InventoryDbContext
        Inherits BaseDbContext

        Public Property Products As DbSet(Of Product)
        Public Property StockBatches As DbSet(Of StockBatch)
        Public Property ShrinkageRecords As DbSet(Of ShrinkageRecord)
        Public Property StockAlertConfigs As DbSet(Of StockAlertConfig)
        Public Property ProductCategories As DbSet(Of ProductCategory)
        Public Property StockMovements As DbSet(Of StockMovement)
        Public Property StockAuditRecords As DbSet(Of StockAuditRecord)
        Public Property ProductPriceHistory As DbSet(Of ProductPriceHistory)
        Public Property SaleCogsRecords As DbSet(Of SaleCogsRecord)


        Public Sub New(options As DbContextOptions(Of InventoryDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(InventoryDbContext).Assembly)
            InventorySeedData.Seed(modelBuilder)
        End Sub

    End Class

End Namespace
