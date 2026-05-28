Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class PurchaseOrderConfiguration
        Implements IEntityTypeConfiguration(Of PurchaseOrder)

        Public Sub Configure(builder As EntityTypeBuilder(Of PurchaseOrder)) Implements IEntityTypeConfiguration(Of PurchaseOrder).Configure
            builder.ToTable("Pur_PurchaseOrders")

            builder.HasKey(Function(po) po.Id)

            builder.Property(Function(po) po.OrderNumber).IsRequired().HasMaxLength(20)
            builder.HasIndex(Function(po) po.OrderNumber).IsUnique()

            builder.Property(Function(po) po.VendorId).IsRequired()
            builder.Property(Function(po) po.Status).IsRequired()
            builder.Property(Function(po) po.TotalAmount).HasPrecision(18, 2)

            builder.HasMany(Function(po) po.Lines).
                    WithOne(Function(l) l.PurchaseOrder).
                    HasForeignKey(Function(l) l.PurchaseOrderId).
                    OnDelete(DeleteBehavior.Cascade)

            builder.HasMany(Function(po) po.GoodsReceipts).
                    WithOne(Function(gr) gr.PurchaseOrder).
                    HasForeignKey(Function(gr) gr.PurchaseOrderId).
                    OnDelete(DeleteBehavior.Cascade)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
