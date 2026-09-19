Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class GoodsReceiptConfiguration
        Implements IEntityTypeConfiguration(Of GoodsReceipt)

        Public Sub Configure(builder As EntityTypeBuilder(Of GoodsReceipt)) Implements IEntityTypeConfiguration(Of GoodsReceipt).Configure
            builder.ToTable("Pur_GoodsReceipts")

            builder.HasKey(Function(gr) gr.Id)

            builder.Property(Function(gr) gr.ReceiptNumber).IsRequired().HasMaxLength(128)
            builder.HasIndex(Function(gr) gr.ReceiptNumber).IsUnique()

            builder.Property(Function(gr) gr.PurchaseOrderId).IsRequired()

            builder.HasMany(Function(gr) gr.Lines).
                    WithOne(Function(l) l.GoodsReceipt).
                    HasForeignKey(Function(l) l.GoodsReceiptId).
                    OnDelete(DeleteBehavior.Cascade)
        End Sub

    End Class

End Namespace
