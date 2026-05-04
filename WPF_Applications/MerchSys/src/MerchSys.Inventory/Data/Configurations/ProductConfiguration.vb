Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class ProductConfiguration
        Implements IEntityTypeConfiguration(Of Product)

        Public Sub Configure(builder As EntityTypeBuilder(Of Product)) Implements IEntityTypeConfiguration(Of Product).Configure
            builder.ToTable("Inv_Products")

            builder.HasKey(Function(p) p.Id)

            builder.Property(Function(p) p.Name).IsRequired().HasMaxLength(200)
            builder.Property(Function(p) p.Sku).IsRequired().HasMaxLength(50)
            builder.HasIndex(Function(p) p.Sku).IsUnique()

            builder.Property(Function(p) p.RetailPrice).HasPrecision(18, 2)
            builder.Property(Function(p) p.Unit).HasMaxLength(50)
            builder.Property(Function(p) p.Description).HasMaxLength(256)

            builder.HasMany(Function(p) p.StockBatches).
                    WithOne(Function(b) b.Product).
                    HasForeignKey(Function(b) b.ProductId).
                    OnDelete(DeleteBehavior.Restrict)

            builder.HasMany(Function(p) p.ShrinkageRecords).
                    WithOne(Function(s) s.Product).
                    HasForeignKey(Function(s) s.ProductId).
                    OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
