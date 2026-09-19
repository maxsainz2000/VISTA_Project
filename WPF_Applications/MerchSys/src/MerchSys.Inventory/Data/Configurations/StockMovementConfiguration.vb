Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class StockMovementConfiguration
        Implements IEntityTypeConfiguration(Of StockMovement)

        Public Sub Configure(builder As EntityTypeBuilder(Of StockMovement)) Implements IEntityTypeConfiguration(Of StockMovement).Configure
            builder.ToTable("Inv_StockMovements")

            builder.HasKey(Function(m) m.Id)

            builder.Property(Function(m) m.ProductId).
                IsRequired()

            builder.Property(Function(m) m.MovementType).
                IsRequired().
                HasConversion(Of String)().
                HasMaxLength(20)

            builder.Property(Function(m) m.Quantity).
                IsRequired()

            builder.Property(Function(m) m.OccurredAt).
                IsRequired()

            builder.Property(Function(m) m.CreatedBy).HasMaxLength(256)
            builder.Property(Function(m) m.ModifiedBy).HasMaxLength(256)

            builder.HasIndex(Function(m) New With {m.ProductId, m.OccurredAt})

            builder.HasOne(Function(m) m.Product).
                WithMany().
                HasForeignKey(Function(m) m.ProductId).
                OnDelete(DeleteBehavior.Restrict).
                IsRequired()
        End Sub

    End Class

End Namespace
