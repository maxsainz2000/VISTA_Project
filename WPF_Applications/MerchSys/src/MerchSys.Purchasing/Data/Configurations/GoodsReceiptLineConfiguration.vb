Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class GoodsReceiptLineConfiguration
        Implements IEntityTypeConfiguration(Of GoodsReceiptLine)

        Public Sub Configure(builder As EntityTypeBuilder(Of GoodsReceiptLine)) Implements IEntityTypeConfiguration(Of GoodsReceiptLine).Configure
            builder.ToTable("Pur_GoodsReceiptLines")

            builder.HasKey(Function(l) l.Id)

            builder.Property(Function(l) l.GoodsReceiptId).IsRequired()
            builder.Property(Function(l) l.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(l) l.UnitCost).HasPrecision(18, 4)
        End Sub

    End Class

End Namespace
