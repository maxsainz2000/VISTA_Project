Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class ReorderConfigConfiguration
        Implements IEntityTypeConfiguration(Of ReorderConfig)

        Public Sub Configure(builder As EntityTypeBuilder(Of ReorderConfig)) Implements IEntityTypeConfiguration(Of ReorderConfig).Configure
            builder.ToTable("Pur_ReorderConfigs")

            builder.HasKey(Function(c) c.Id)

            builder.Property(Function(c) c.ProductId).IsRequired()
            builder.Property(Function(c) c.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(c) c.SeasonalMultiplier).HasPrecision(5, 2)

            builder.HasIndex(Function(c) c.ProductId).IsUnique()
            builder.HasIndex(Function(c) c.IsActive)

            builder.HasOne(Function(c) c.PreferredVendor).
                    WithMany().
                    HasForeignKey(Function(c) c.PreferredVendorId).
                    OnDelete(DeleteBehavior.SetNull).
                    IsRequired(False)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
