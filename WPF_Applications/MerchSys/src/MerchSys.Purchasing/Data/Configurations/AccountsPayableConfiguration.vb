Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class AccountsPayableConfiguration
        Implements IEntityTypeConfiguration(Of AccountsPayableEntry)

        Public Sub Configure(builder As EntityTypeBuilder(Of AccountsPayableEntry)) Implements IEntityTypeConfiguration(Of AccountsPayableEntry).Configure
            builder.ToTable("Pur_AccountsPayable")

            builder.HasKey(Function(ap) ap.Id)

            builder.Property(Function(ap) ap.PurchaseOrderId).IsRequired()
            builder.Property(Function(ap) ap.VendorId).IsRequired()
            builder.Property(Function(ap) ap.TotalAmount).HasPrecision(18, 2)
            builder.Property(Function(ap) ap.AmountPaid).HasPrecision(18, 2)
            builder.Property(Function(ap) ap.Balance).HasPrecision(18, 2)

            builder.HasIndex(Function(ap) New With {ap.VendorId, ap.IsPaid})

            builder.HasOne(Function(ap) ap.PurchaseOrder).
                    WithOne().
                    HasForeignKey(Of AccountsPayableEntry)(Function(ap) ap.PurchaseOrderId).
                    OnDelete(DeleteBehavior.Restrict)

            builder.HasOne(Function(ap) ap.Vendor).
                    WithMany().
                    HasForeignKey(Function(ap) ap.VendorId).
                    OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
