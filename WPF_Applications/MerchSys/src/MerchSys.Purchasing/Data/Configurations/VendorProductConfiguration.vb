Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class VendorProductConfiguration
        Implements IEntityTypeConfiguration(Of VendorProduct)

        Public Sub Configure(builder As EntityTypeBuilder(Of VendorProduct)) Implements IEntityTypeConfiguration(Of VendorProduct).Configure
            builder.ToTable("Pur_VendorProducts")

            builder.HasKey(Function(vp) vp.Id)

            builder.Property(Function(vp) vp.VendorId).IsRequired()
            builder.Property(Function(vp) vp.ProductId).IsRequired()
            builder.Property(Function(vp) vp.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(vp) vp.LastUnitCost).HasPrecision(18, 4)
            builder.Property(Function(vp) vp.Notes).IsRequired(False)

            ' FK relation to Vendor with restrict delete behavior to prevent deleting vendors with active products
            builder.HasOne(Function(vp) vp.Vendor).
                    WithMany().
                    HasForeignKey(Function(vp) vp.VendorId).
                    OnDelete(DeleteBehavior.Restrict)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()

            ' Unique composite index on VendorId and ProductId where IsDeleted = 0
            builder.HasIndex(Function(vp) New With {vp.VendorId, vp.ProductId}).
                    IsUnique().
                    HasFilter("IsDeleted = 0")
        End Sub

    End Class

End Namespace
