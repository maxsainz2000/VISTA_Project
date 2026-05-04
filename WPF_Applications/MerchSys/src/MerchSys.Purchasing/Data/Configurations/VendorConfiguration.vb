Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class VendorConfiguration
        Implements IEntityTypeConfiguration(Of Vendor)

        Public Sub Configure(builder As EntityTypeBuilder(Of Vendor)) Implements IEntityTypeConfiguration(Of Vendor).Configure
            builder.ToTable("Pur_Vendors")

            builder.HasKey(Function(v) v.Id)

            builder.Property(Function(v) v.Name).IsRequired().HasMaxLength(200)
            builder.Property(Function(v) v.ContactPerson).IsRequired().HasMaxLength(100)
            builder.Property(Function(v) v.Phone).IsRequired().HasMaxLength(20)
            builder.Property(Function(v) v.Email).HasMaxLength(100)
            builder.Property(Function(v) v.Address).IsRequired().HasMaxLength(500)

            builder.HasIndex(Function(v) v.Name).IsUnique()

            builder.HasMany(Function(v) v.PurchaseOrders).
                    WithOne(Function(po) po.Vendor).
                    HasForeignKey(Function(po) po.VendorId).
                    OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
