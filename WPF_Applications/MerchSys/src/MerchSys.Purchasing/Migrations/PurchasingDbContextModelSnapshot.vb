Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports MerchSys.Purchasing.Data

Namespace Migrations

    <DbContext(GetType(PurchasingDbContext))>
    Partial Class PurchasingDbContextModelSnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder.HasAnnotation("ProductVersion", "10.0.7")

            modelBuilder.Entity("MerchSys.Purchasing.Entities.Vendor", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of String)("Address").IsRequired().HasMaxLength(500).HasColumnType("TEXT")
                b.Property(Of String)("ContactPerson").IsRequired().HasMaxLength(100).HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("DefaultLeadTimeDays").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Email").HasMaxLength(100).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Phone").IsRequired().HasMaxLength(20).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("Name").IsUnique()
                b.ToTable("Pur_Vendors")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.PurchaseOrder", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ExpectedDeliveryDate").HasColumnType("TEXT")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime)("OrderDate").HasColumnType("TEXT")
                b.Property(Of String)("OrderNumber").IsRequired().HasMaxLength(20).HasColumnType("TEXT")
                b.Property(Of Integer)("Status").HasColumnType("INTEGER")
                b.Property(Of Decimal)("TotalAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Integer)("VendorId").HasColumnType("INTEGER")
                b.HasKey("Id")
                b.HasIndex("OrderNumber").IsUnique()
                b.HasIndex("VendorId")
                b.ToTable("Pur_PurchaseOrders")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.PurchaseOrderLine", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Decimal)("LineTotal").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").IsRequired().HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of Integer)("PurchaseOrderId").HasColumnType("INTEGER")
                b.Property(Of Integer)("QuantityOrdered").HasColumnType("INTEGER")
                b.Property(Of Decimal)("UnitCost").HasPrecision(18, 4).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("PurchaseOrderId")
                b.ToTable("Pur_PurchaseOrderLines")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.GoodsReceipt", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("PurchaseOrderId").HasColumnType("INTEGER")
                b.Property(Of DateTime)("ReceivedDate").HasColumnType("TEXT")
                b.Property(Of String)("ReceivedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("ReceiptNumber").IsRequired().HasMaxLength(20).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("ReceiptNumber").IsUnique()
                b.HasIndex("PurchaseOrderId")
                b.ToTable("Pur_GoodsReceipts")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.GoodsReceiptLine", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("DiscrepancyNotes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ExpiryDate").HasColumnType("TEXT")
                b.Property(Of Integer)("GoodsReceiptId").HasColumnType("INTEGER")
                b.Property(Of Boolean)("HasDiscrepancy").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").IsRequired().HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of Integer)("QuantityOrdered").HasColumnType("INTEGER")
                b.Property(Of Integer)("QuantityReceived").HasColumnType("INTEGER")
                b.Property(Of Decimal)("UnitCost").HasPrecision(18, 4).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("GoodsReceiptId")
                b.ToTable("Pur_GoodsReceiptLines")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.AccountsPayableEntry", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of Decimal)("AmountPaid").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Decimal)("Balance").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime)("DueDate").HasColumnType("TEXT")
                b.Property(Of DateTime)("InvoiceDate").HasColumnType("TEXT")
                b.Property(Of String)("InvoiceNumber").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsPaid").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("PurchaseOrderId").HasColumnType("INTEGER")
                b.Property(Of Decimal)("TotalAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Integer)("VendorId").HasColumnType("INTEGER")
                b.HasKey("Id")
                b.HasIndex("VendorId", "IsPaid")
                b.HasIndex("PurchaseOrderId").IsUnique()
                b.ToTable("Pur_AccountsPayable")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.ReorderConfig", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("DefaultOrderQuantity").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsActive").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsSeasonalItem").HasColumnType("INTEGER")
                b.Property(Of Integer)("LeadTimeDays").HasColumnType("INTEGER")
                b.Property(Of Integer)("MinimumThreshold").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer?)("PreferredVendorId").HasColumnType("INTEGER")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").IsRequired().HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of Integer)("SafetyStock").HasColumnType("INTEGER")
                b.Property(Of Decimal)("SeasonalMultiplier").HasPrecision(5, 2).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("ProductId").IsUnique()
                b.HasIndex("IsActive")
                b.ToTable("Pur_ReorderConfigs")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.ReorderSuggestion", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer?)("ConvertedToPOId").HasColumnType("INTEGER")
                b.Property(Of Integer)("CurrentStock").HasColumnType("INTEGER")
                b.Property(Of Integer)("EstimatedLeadTimeDays").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsSeasonalAdjusted").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer?)("PreferredVendorId").HasColumnType("INTEGER")
                b.Property(Of String)("PreferredVendorName").HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").IsRequired().HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of Integer)("ReorderPoint").HasColumnType("INTEGER")
                b.Property(Of String)("Status").IsRequired().HasMaxLength(20).HasColumnType("TEXT")
                b.Property(Of Integer)("SuggestedQuantity").HasColumnType("INTEGER")
                b.HasKey("Id")
                b.HasIndex("Status")
                b.HasIndex("ProductId", "Status")
                b.ToTable("Pur_ReorderSuggestions")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.PriceChangeAlert", Sub(b)
                b.Property(Of Integer)("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER")
                b.Property(Of DateTime?)("AcknowledgedAt").HasColumnType("TEXT")
                b.Property(Of Decimal)("ChangePercent").HasPrecision(18, 4).HasColumnType("TEXT")
                b.Property(Of String)("ChangeDirection").IsRequired().HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("GoodsReceiptId").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsAcknowledged").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Decimal)("NewUnitCost").HasPrecision(18, 4).HasColumnType("TEXT")
                b.Property(Of Decimal)("PreviousUnitCost").HasPrecision(18, 4).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").IsRequired().HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("VendorId").HasColumnType("INTEGER")
                b.Property(Of String)("VendorName").IsRequired().HasMaxLength(256).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("IsAcknowledged")
                b.HasIndex("ProductId")
                b.ToTable("Pur_PriceChangeAlerts")
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.PurchaseOrder", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.Vendor", "Vendor").
                    WithMany("PurchaseOrders").
                    HasForeignKey("VendorId").
                    OnDelete(DeleteBehavior.Restrict).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.PurchaseOrderLine", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.PurchaseOrder", "PurchaseOrder").
                    WithMany("Lines").
                    HasForeignKey("PurchaseOrderId").
                    OnDelete(DeleteBehavior.Cascade).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.GoodsReceipt", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.PurchaseOrder", "PurchaseOrder").
                    WithMany("GoodsReceipts").
                    HasForeignKey("PurchaseOrderId").
                    OnDelete(DeleteBehavior.Cascade).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.GoodsReceiptLine", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.GoodsReceipt", "GoodsReceipt").
                    WithMany("Lines").
                    HasForeignKey("GoodsReceiptId").
                    OnDelete(DeleteBehavior.Cascade).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.AccountsPayableEntry", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.PurchaseOrder", "PurchaseOrder").
                    WithOne().
                    HasForeignKey("MerchSys.Purchasing.Entities.AccountsPayableEntry", "PurchaseOrderId").
                    OnDelete(DeleteBehavior.Restrict).
                    IsRequired()
                b.HasOne("MerchSys.Purchasing.Entities.Vendor", "Vendor").
                    WithMany().
                    HasForeignKey("VendorId").
                    OnDelete(DeleteBehavior.Restrict).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.Purchasing.Entities.ReorderConfig", Sub(b)
                b.HasOne("MerchSys.Purchasing.Entities.Vendor", "PreferredVendor").
                    WithMany().
                    HasForeignKey("PreferredVendorId").
                    OnDelete(DeleteBehavior.SetNull)
            End Sub)
        End Sub

    End Class

End Namespace
