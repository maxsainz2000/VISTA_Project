Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports MerchSys.Inventory.Data

Namespace Migrations

    <DbContext(GetType(InventoryDbContext))>
    Partial Class InventoryDbContextModelSnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder.HasAnnotation("ProductVersion", "10.0.7")

            modelBuilder.Entity("MerchSys.Inventory.Entities.ProductCategory", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Description").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Name").
                    IsRequired().
                    HasMaxLength(100).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("Name").IsUnique()
                b.ToTable("Inv_ProductCategories")
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.Product", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of Integer)("CategoryId").HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Description").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Boolean)("HasExpiry").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsActive").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of Integer)("MinimumThreshold").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Name").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("RetailPrice").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of String)("Sku").
                    IsRequired().
                    HasMaxLength(50).
                    HasColumnType("TEXT")
                b.Property(Of String)("Unit").HasMaxLength(50).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("Sku").IsUnique()
                b.ToTable("Inv_Products")
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.StockBatch", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ExpiryDate").HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of Integer)("QuantityReceived").HasColumnType("INTEGER")
                b.Property(Of Integer)("QuantityRemaining").HasColumnType("INTEGER")
                b.Property(Of DateTime)("ReceiptDate").HasColumnType("TEXT")
                b.Property(Of Integer?)("SourcePurchaseOrderId").HasColumnType("INTEGER")
                b.Property(Of Decimal)("UnitCost").
                    HasPrecision(18, 4).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("ProductId", "ReceiptDate")
                b.ToTable("Inv_StockBatches")
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.ShrinkageRecord", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of Integer)("QuantityLost").HasColumnType("INTEGER")
                b.Property(Of String)("Reason").
                    IsRequired().
                    HasMaxLength(50).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("RecordedDate").HasColumnType("TEXT")
                b.Property(Of Integer?)("StockBatchId").HasColumnType("INTEGER")
                b.Property(Of Decimal)("TotalValue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("UnitCost").
                    HasPrecision(18, 4).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.ToTable("Inv_ShrinkageRecords")
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.StockAlertConfig", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ExpiryAlertDays").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsAlertEnabled").HasColumnType("INTEGER")
                b.Property(Of Integer)("MinimumThreshold").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.HasKey("Id")
                b.ToTable("Inv_StockAlertConfigs")
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.Product", Sub(b)
                b.HasOne("MerchSys.Inventory.Entities.ProductCategory", "Category").
                    WithMany("Products").
                    HasForeignKey("CategoryId").
                    OnDelete(DeleteBehavior.Restrict).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.ShrinkageRecord", Sub(b)
                b.HasOne("MerchSys.Inventory.Entities.StockBatch", "StockBatch").
                    WithMany().
                    HasForeignKey("StockBatchId").
                    OnDelete(DeleteBehavior.SetNull)
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.StockAlertConfig", Sub(b)
                b.HasOne("MerchSys.Inventory.Entities.Product", "Product").
                    WithMany().
                    HasForeignKey("ProductId").
                    OnDelete(DeleteBehavior.Cascade).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Inventory.Entities.StockBatch", Sub(b)
                b.HasOne("MerchSys.Inventory.Entities.Product", "Product").
                    WithMany("StockBatches").
                    HasForeignKey("ProductId").
                    OnDelete(DeleteBehavior.Restrict).
                    IsRequired()
            End Sub)
        End Sub

    End Class

End Namespace
