Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class ProductCategoryConfiguration
        Implements IEntityTypeConfiguration(Of ProductCategory)

        Public Sub Configure(builder As EntityTypeBuilder(Of ProductCategory)) Implements IEntityTypeConfiguration(Of ProductCategory).Configure
            builder.ToTable("Inv_ProductCategories")

            builder.HasKey(Function(c) c.Id)

            builder.Property(Function(c) c.Name).IsRequired().HasMaxLength(100)
            builder.HasIndex(Function(c) c.Name).IsUnique()

            builder.Property(Function(c) c.Description).HasMaxLength(256)

            builder.HasMany(Function(c) c.Products).
                    WithOne(Function(p) p.Category).
                    HasForeignKey(Function(p) p.CategoryId).
                    OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
