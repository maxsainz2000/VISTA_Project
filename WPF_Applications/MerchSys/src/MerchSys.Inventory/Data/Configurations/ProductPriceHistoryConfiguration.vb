Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class ProductPriceHistoryConfiguration
        Implements IEntityTypeConfiguration(Of ProductPriceHistory)

        Public Sub Configure(builder As EntityTypeBuilder(Of ProductPriceHistory)) Implements IEntityTypeConfiguration(Of ProductPriceHistory).Configure
            builder.ToTable("Inv_ProductPriceHistory")

            builder.HasKey(Function(p) p.Id)

            builder.Property(Function(p) p.ProductId).
                IsRequired()

            builder.Property(Function(p) p.OldPrice).
                IsRequired().
                HasPrecision(18, 2)

            builder.Property(Function(p) p.NewPrice).
                IsRequired().
                HasPrecision(18, 2)

            builder.Property(Function(p) p.ChangedAt).
                IsRequired()

            builder.Property(Function(p) p.ChangedBy).
                IsRequired().
                HasMaxLength(256)

            builder.Property(Function(p) p.Reason).
                HasMaxLength(500)

            builder.HasIndex(Function(p) New With {p.ProductId, p.ChangedAt})

            builder.HasOne(Function(p) p.Product).
                WithMany().
                HasForeignKey(Function(p) p.ProductId).
                OnDelete(DeleteBehavior.Restrict).
                IsRequired()
        End Sub

    End Class

End Namespace
